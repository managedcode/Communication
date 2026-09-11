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
    private const string ValidationField = "email";

    [Test]
    public async Task HostRegistration_ShouldExportAutomaticLogsTracesAndMetrics()
    {
        var spans = new List<Activity>();
        var logs = new List<(LogLevel Level, Exception? Exception, ActivityTraceId TraceId, ActivitySpanId SpanId, string? Message)>();
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
                    logs.Add((record.LogLevel, record.Exception, record.TraceId, record.SpanId, record.FormattedMessage));
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
        var failure = spans.Single(span => span.OperationName == CommunicationTelemetry.CreatedFailureActivityName);
        logs[0].SpanId.ShouldBe(failure.SpanId);
        failure.Status.ShouldBe(ActivityStatusCode.Error);
        failure.ParentSpanId.ShouldBe(parent.SpanId);
        failure.Events.Single(e => e.Name == ExceptionEvent).Tags
            .Single(tag => tag.Key == ExceptionStackTrace).Value.ShouldNotBeNull();
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result));
        json.RootElement.GetProperty(CommunicationJsonNames.Problem)
            .TryGetProperty(OriginalExceptionProperty, out _).ShouldBeFalse();
        JsonSerializer.Serialize(result).ShouldNotContain(original.StackTrace!);

        Result.Fail(FailureTitle, FailureDetail).IsFailed.ShouldBeTrue();
        logs.Count.ShouldBe(2);
        logs[1].Level.ShouldBe(LogLevel.Warning);
        logs[1].Exception.ShouldBeNull();
        logs[1].Message.ShouldNotBeNull();
        logs[1].Message!.ShouldContain(FailureTitle);
        logs[1].Message!.ShouldContain(FailureDetail);

        host.Services.GetRequiredService<MeterProvider>().ForceFlush().ShouldBeTrue();
        createdFailures.ShouldBe(2);
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
        spans.Count.ShouldBe(4);
        spans.ShouldAllBe(span => span.Status == ActivityStatusCode.Error);
        spans.Count(span => Equals(span.GetTagItem(ParentActivityTag), FailureTitle)).ShouldBe(3);
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

        Result<int>.From((Func<int>)ThrowFailure).IsFailed.ShouldBeTrue();
        (await Result<int>.From((Func<Task<int>>)ThrowTaskFailure)).IsFailed.ShouldBeTrue();
        (await Result<int>.From((Func<ValueTask<int>>)ThrowValueTaskFailure)).IsFailed.ShouldBeTrue();

        spans.Count.ShouldBe(3);
        foreach (var span in spans)
        {
            span.Status.ShouldBe(ActivityStatusCode.Error);
            var stack = span.Events.Single(e => e.Name == ExceptionEvent).Tags
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
