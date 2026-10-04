using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Extensions.Telemetry;
using ManagedCode.Communication.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Shouldly;

namespace ManagedCode.Communication.Tests.Telemetry;

[NotInParallel]
public sealed class AutomaticFailureTelemetryTests
{
    private const string FailureTitle = "automatic-failure-test";
    private const string FailureDetail = "automatic failure detail";
    private const string ParentOperation = "automatic-failure-parent";
    private const string ExceptionEvent = "exception";
    private const string ExceptionStackTrace = "exception.stacktrace";
    private const string OriginalExceptionProperty = "OriginalException";
    private const string ParentActivityTag = "problem.title";
    private const string ErrorCodeProperty = "ErrorCode";
    private const string StatusCodeProperty = "StatusCode";
    private const string MachineCode = "invalid_grant";
    private const string ValidationField = "email";

    [Test]
    public async Task HostRegistration_ShouldExportAutomaticLogsTracesAndMetrics()
    {
        var spans = new List<Activity>();
        var logs = new List<(LogLevel Level, Exception? Exception, ActivityTraceId TraceId, ActivitySpanId SpanId, string? Message, IReadOnlyList<KeyValuePair<string, object?>> Attributes)>();
        long createdFailures = 0;
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.AddProcessor(
            new SimpleLogRecordExportProcessor(new CaptureExporter<LogRecord>(record =>
            {
                if (record.CategoryName == typeof(Result).FullName)
                {
                    logs.Add((record.LogLevel, record.Exception, record.TraceId, record.SpanId, record.FormattedMessage, record.Attributes?.ToArray() ?? []));
                }
            })));
        });
        builder.AddCommunicationTelemetry().ShouldBeSameAs(builder);
        builder.AddCommunicationTelemetry(); // Repeated ServiceDefaults registration is harmless.
        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddProcessor(new SimpleActivityExportProcessor(
                new CaptureExporter<Activity>(spans.Add))))
            .WithMetrics(metrics => metrics.AddReader(new PeriodicExportingMetricReader(
                new CaptureExporter<Metric>(metric =>
                {
                    if (metric.Name == CommunicationTelemetry.CreatedFailureCounterName)
                    {
                        foreach (ref readonly var point in metric.GetMetricPoints())
                        {
                            createdFailures += point.GetSumLong();
                        }
                    }
                }), exportIntervalMilliseconds: int.MaxValue)));
        using var host = builder.Build();
        await host.StartAsync();
        using var parent = CommunicationTelemetry.StartActivity(ParentOperation);
        parent.ShouldNotBeNull();
        Exception original;
        try
        {
            throw new InvalidOperationException(FailureDetail);
        }
        catch (InvalidOperationException exception)
        {
            original = exception;
        }

        var result = Result<int>.Fail(original);
        Result.Fail(result.Problem!).IsFailed.ShouldBeTrue();
        CollectionResult<int>.Fail(result.Problem!).IsFailed.ShouldBeTrue();
        Result.Succeed().IsSuccess.ShouldBeTrue();
        Result<int>.Succeed(1).IsSuccess.ShouldBeTrue();
        logs.Count.ShouldBe(1);
        logs[0].Level.ShouldBe(LogLevel.Error);
        logs[0].Exception.ShouldBeSameAs(original);
        logs[0].TraceId.ShouldBe(parent.TraceId);
        parent.Status.ShouldBe(ActivityStatusCode.Unset);
        spans.ShouldBeEmpty();
        logs[0].SpanId.ShouldBe(parent.SpanId);
        logs[0].Attributes.Single(tag => tag.Key == ErrorCodeProperty).Value.ShouldBe(result.Problem!.ErrorCode);
        logs[0].Attributes.Single(tag => tag.Key == StatusCodeProperty).Value.ShouldBe(result.Problem!.StatusCode);
        parent.Events.Single(e => e.Name == CommunicationTelemetry.CreatedFailureEventName).Tags
            .Single(tag => tag.Key == CommunicationTelemetry.ProblemErrorCodeTag).Value.ShouldBe(result.Problem!.ErrorCode);
        parent.Events.Single(e => e.Name == ExceptionEvent).Tags
            .Single(tag => tag.Key == ExceptionStackTrace).Value.ShouldNotBeNull();
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result));
        json.RootElement.GetProperty(CommunicationJsonNames.Problem)
            .TryGetProperty(OriginalExceptionProperty, out _).ShouldBeFalse();
        JsonSerializer.Serialize(result).ShouldNotContain(original.StackTrace!);

        var codedProblem = Problem.Create(FailureTitle, FailureDetail, 400);
        codedProblem.ErrorCode = MachineCode;
        Result.Fail(codedProblem).IsFailed.ShouldBeTrue();
        logs.Count.ShouldBe(2);
        logs[1].Level.ShouldBe(LogLevel.Warning);
        logs[1].Exception.ShouldBeNull();
        logs[1].Message.ShouldNotBeNull();
        logs[1].Message!.ShouldContain(FailureTitle);
        logs[1].Message!.ShouldContain(FailureDetail);

        logs[1].Attributes.Single(tag => tag.Key == ErrorCodeProperty).Value.ShouldBe(MachineCode);
        logs[1].Attributes.Single(tag => tag.Key == StatusCodeProperty).Value.ShouldBe(400);

        var previous = Activity.Current;
        try
        {
            Activity.Current = null;
            Result.Fail(codedProblem).IsFailed.ShouldBeTrue(); // Same Problem is still deduplicated outside a trace.
            Result.Fail(FailureTitle, FailureDetail).IsFailed.ShouldBeTrue();
            Result<int>.Fail(original).IsFailed.ShouldBeTrue();
            logs.Count.ShouldBe(4);
            logs[2].Level.ShouldBe(LogLevel.Warning);
            logs[2].SpanId.ShouldBe(default);
            logs[3].Level.ShouldBe(LogLevel.Error);
            logs[3].Exception.ShouldBeSameAs(original);
            logs[3].SpanId.ShouldBe(default);
            spans.ShouldBeEmpty();
        }
        finally
        {
            Activity.Current = previous;
        }

        var validationProblem = Problem.Validation((ValidationField, FailureDetail));
        validationProblem.ErrorCode = MachineCode;
        Result.Fail(validationProblem).IsInvalid.ShouldBeTrue();
        logs.Count.ShouldBe(5);
        logs[4].Level.ShouldBe(LogLevel.Warning);
        logs[4].Attributes.Single(tag => tag.Key == ErrorCodeProperty).Value.ShouldBe(MachineCode);
        logs[4].Attributes.Single(tag => tag.Key == StatusCodeProperty).Value.ShouldBe(validationProblem.StatusCode);
        logs[4].SpanId.ShouldBe(parent.SpanId);

        host.Services.GetRequiredService<MeterProvider>().ForceFlush().ShouldBeTrue();
        createdFailures.ShouldBe(5);
        await host.StopAsync();
    }

    [Test]
    public void FailureFactories_ShouldTraceEveryShapeAndLeaveParentSuccessful()
    {
        var spans = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == CommunicationTelemetry.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = spans.Add
        };
        ActivitySource.AddActivityListener(listener);
        using var parent = CommunicationTelemetry.StartActivity(ParentOperation);
        Result.Fail(FailureTitle, FailureDetail).IsFailed.ShouldBeTrue();
        Result<int>.Fail(FailureTitle, FailureDetail).IsFailed.ShouldBeTrue();
        CollectionResult<int>.Fail(FailureTitle, FailureDetail).IsFailed.ShouldBeTrue();
        Result.FailValidation((ValidationField, FailureDetail)).IsInvalid.ShouldBeTrue();
        spans.ShouldBeEmpty();
        var events = parent!.Events.Where(item => item.Name == CommunicationTelemetry.CreatedFailureEventName).ToArray();
        events.Length.ShouldBe(4);
        events.Count(item => item.Tags.Any(tag => tag.Key == ParentActivityTag && Equals(tag.Value, FailureTitle))).ShouldBe(3);
        parent!.Status.ShouldBe(ActivityStatusCode.Unset);
    }

    [Test]
    public async Task From_WhenDelegatesThrow_ShouldImmediatelyRecordStackTraces()
    {
        var spans = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == CommunicationTelemetry.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = spans.Add
        };
        ActivitySource.AddActivityListener(listener);

        using var parent = CommunicationTelemetry.StartActivity(ParentOperation);
        parent.ShouldNotBeNull();
        Result<int>.From((Func<int>)ThrowFailure).IsFailed.ShouldBeTrue();
        (await Result<int>.From((Func<Task<int>>)ThrowTaskFailure)).IsFailed.ShouldBeTrue();
        (await Result<int>.From((Func<ValueTask<int>>)ThrowValueTaskFailure)).IsFailed.ShouldBeTrue();

        spans.ShouldBeEmpty();
        parent.Status.ShouldBe(ActivityStatusCode.Unset);
        var exceptions = parent.Events.Where(item => item.Name == ExceptionEvent).ToArray();
        exceptions.Length.ShouldBe(3);
        parent.Events.Count(item => item.Name == CommunicationTelemetry.CreatedFailureEventName).ShouldBe(3);
        foreach (var exceptionEvent in exceptions)
        {
            var stack = exceptionEvent.Tags
                .Single(tag => tag.Key == ExceptionStackTrace).Value?.ToString();
            stack.ShouldNotBeNullOrWhiteSpace();
            stack.ShouldContain(nameof(ThrowFailure));
        }
    }

    private static int ThrowFailure() => throw new InvalidOperationException(FailureDetail);

    private static async Task<int> ThrowTaskFailure()
    {
        await Task.Yield();
        return ThrowFailure();
    }

    private static async ValueTask<int> ThrowValueTaskFailure()
    {
        await Task.Yield();
        return ThrowFailure();
    }

    private sealed class CaptureExporter<T>(Action<T> capture) : BaseExporter<T> where T : class
    {
        public override ExportResult Export(in Batch<T> batch)
        {
            foreach (var item in batch)
            {
                capture(item);
            }

            return ExportResult.Success;
        }
    }
}
