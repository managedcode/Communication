using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.Extensions.Telemetry;
using ManagedCode.Communication.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using Shouldly;

namespace ManagedCode.Communication.Tests.CQRS;

[NotInParallel]
public sealed class CqrsExceptionTelemetryTests
{
    private const string OperationName = "cqrs-native-operation";
    private const string FailureDetail = "cqrs telemetry exception detail";
    private const string ExceptionEventName = "exception";
    private const string ExceptionStackTraceTag = "exception.stacktrace";
    private const string ExceptionPropertyName = "Exception";
    private const string StackTracePropertyName = "StackTrace";
    private static readonly TimeSpan LifecycleTimeout = TimeSpan.FromSeconds(10);

    [Test]
    public async Task CreateAndNormalize_ExceptionFailuresReportTheOriginalStackOnceWithoutRetainingIt()
    {
        var logs = new List<CapturedLog>();
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == CommunicationTelemetry.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.OperationName == OperationName)
                {
                    activities.Add(activity);
                }
            }
        };
        ActivitySource.AddActivityListener(listener);

        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.AddProcessor(new SimpleLogRecordExportProcessor(new CaptureExporter<LogRecord>(record =>
            {
                if (record.CategoryName == typeof(Result).FullName)
                {
                    logs.Add(new CapturedLog(record.LogLevel, record.Exception));
                }
            })));
        });
        builder.AddCommunicationTelemetry();
        using var host = builder.Build();
        using (var startTimeout = new CancellationTokenSource(LifecycleTimeout))
        {
            await host.StartAsync(startTimeout.Token).ConfigureAwait(false);
        }

        using var producerActivity = CommunicationTelemetry.StartActivity(OperationName);
        producerActivity.ShouldNotBeNull();
        var producerException = new CqrsTelemetryException(FailureDetail);
        var producerChunks = await CollectAsync(CqrsStream.Create<ProgressUpdate, FinalResult>(
                _ => ThrowProducerExceptionAsync(producerException)))
            .ConfigureAwait(false);
        producerChunks.Count.ShouldBe(1);
        var producerFailure = AssertSingleFailure(producerChunks, sequence: 1);
        producerActivity.Stop();
        AssertTelemetry(logs, activities, producerException, nameof(ThrowProducerExceptionAsync));
        AssertNoExceptionSerialized(producerFailure, producerException);

        logs.Clear();
        activities.Clear();
        using var normalizerActivity = CommunicationTelemetry.StartActivity(OperationName);
        normalizerActivity.ShouldNotBeNull();
        var normalizerException = new CqrsTelemetryException(FailureDetail);
        var normalizedChunks = await CollectAsync(CqrsStreamNormalizer.NormalizeAsync(
                ThrowAfterStartedAsync(normalizerException),
                assignSequenceNumbers: true,
                ensureTerminalChunk: true))
            .ConfigureAwait(false);
        normalizedChunks.Count.ShouldBe(2);
        normalizedChunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        var normalizedFailure = AssertSingleFailure(normalizedChunks, sequence: 2);
        normalizerActivity.Stop();
        AssertTelemetry(logs, activities, normalizerException, nameof(ThrowAfterStartedAsync));
        AssertNoExceptionSerialized(normalizedFailure, normalizerException);

        using var stopTimeout = new CancellationTokenSource(LifecycleTimeout);
        await host.StopAsync(stopTimeout.Token).ConfigureAwait(false);
    }

    private static Chunk AssertSingleFailure(IReadOnlyList<Chunk> chunks, long sequence)
    {
        chunks.Count(chunk => chunk.Kind == CqrsStreamChunkKind.Failed).ShouldBe(1);
        var failed = chunks.Single(chunk => chunk.Kind == CqrsStreamChunkKind.Failed);
        failed.Problem.ShouldNotBeNull();
        failed.Problem!.Title.ShouldBe(nameof(CqrsTelemetryException));
        failed.Problem.Detail.ShouldBe(FailureDetail);
        failed.Problem.StatusCode.ShouldBe(500);
        failed.Sequence.ShouldBe(sequence);
        return failed;
    }

    private static void AssertTelemetry(
        IReadOnlyList<CapturedLog> logs,
        IReadOnlyList<Activity> activities,
        Exception failure,
        string throwSite)
    {
        var matchingLogs = logs.Where(entry => ReferenceEquals(entry.Exception, failure)).ToArray();
        matchingLogs.Length.ShouldBe(1);
        matchingLogs[0].Level.ShouldBe(LogLevel.Error);

        var matchingActivities = activities
            .Where(activity => activity.Events.Any(item => item.Name == CommunicationTelemetry.CreatedFailureEventName))
            .ToArray();
        matchingActivities.Length.ShouldBe(1);
        matchingActivities[0].Status.ShouldBe(ActivityStatusCode.Unset);
        matchingActivities[0].Events.Single(item => item.Name == CommunicationTelemetry.CreatedFailureEventName).Tags
            .Single(tag => tag.Key == CommunicationTelemetry.ProblemErrorCodeTag).Value.ShouldBe(failure.GetType().FullName);
        var exceptionEvent = matchingActivities[0].Events.Single(item => item.Name == ExceptionEventName);
        var stackTrace = exceptionEvent.Tags.Single(tag => tag.Key == ExceptionStackTraceTag).Value?.ToString();
        stackTrace.ShouldNotBeNullOrWhiteSpace();
        stackTrace!.ShouldContain(throwSite);
    }

    private static void AssertNoExceptionSerialized(Chunk chunk, Exception failure)
    {
        var json = JsonSerializer.Serialize(chunk);
        failure.StackTrace.ShouldNotBeNullOrWhiteSpace();
        json.ShouldNotContain(failure.StackTrace!);
        using var document = JsonDocument.Parse(json);
        AssertNoExceptionProperties(document.RootElement);
    }

    private static void AssertNoExceptionProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                property.Name.Equals(ExceptionPropertyName, StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
                property.Name.Equals(StackTracePropertyName, StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
                AssertNoExceptionProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                AssertNoExceptionProperties(item);
            }
        }
    }

    private static async Task<IReadOnlyList<Chunk>> CollectAsync(IAsyncEnumerable<Chunk> stream)
    {
        using var enumerationTimeout = new CancellationTokenSource(LifecycleTimeout);
        var chunks = new List<Chunk>();
        await foreach (var chunk in stream.WithCancellation(enumerationTimeout.Token).ConfigureAwait(false))
        {
            chunks.Add(chunk);
        }

        return chunks;
    }

    private static async ValueTask<Result<FinalResult>> ThrowProducerExceptionAsync(Exception exception)
    {
        await Task.Yield();
        throw exception;
    }

    private static async IAsyncEnumerable<Chunk> ThrowAfterStartedAsync(Exception exception)
    {
        yield return CqrsTestStreams.Started();
        await Task.Yield();
        throw exception;
    }

    private sealed record CapturedLog(LogLevel Level, Exception? Exception);

    private sealed class CqrsTelemetryException(string message) : Exception(message)
    {
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
