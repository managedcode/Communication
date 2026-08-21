using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Commands.Execution;
using ManagedCode.Communication.Telemetry;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ManagedCode.Communication.Tests.Telemetry;

/// <summary>
///     Traces and metrics emitted for failures.
/// </summary>
/// <remarks>
///     Uses <see cref="ActivityListener" /> and <see cref="MeterListener" /> directly, the same way the
///     OpenTelemetry SDK subscribes, so these tests prove what a real collector would receive without taking a
///     dependency on the SDK.
/// </remarks>
[NotInParallel]
public sealed class CommunicationTelemetryTests : IDisposable
{
    private readonly ActivityListener _activityListener;
    private readonly ConcurrentQueue<Activity> _activities = new();

    public CommunicationTelemetryTests()
    {
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == CommunicationTelemetry.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => _activities.Enqueue(activity)
        };

        ActivitySource.AddActivityListener(_activityListener);
    }

    public void Dispose()
    {
        _activityListener.Dispose();
    }

    [Test]
    public void RecordFailure_MarksTheSpanAsErrored()
    {
        using (var activity = CommunicationTelemetry.StartActivity("test-op"))
        {
            activity.ShouldNotBeNull();
            CommunicationTelemetry.RecordFailure(Problem.Create("boom", "it broke", 409));
        }

        var recorded = _activities.Single(a => a.DisplayName == "test-op");
        recorded.Status.ShouldBe(ActivityStatusCode.Error);
        recorded.StatusDescription.ShouldBe("it broke");
        recorded.GetTagItem("problem.status").ShouldBe(409);
        recorded.GetTagItem("problem.title").ShouldBe("boom");
    }

    [Test]
    public void RecordFailure_AttachesTheRealExceptionWithItsStackTrace()
    {
        Exception captured;
        try
        {
            throw new InvalidOperationException("the real cause");
        }
        catch (Exception exception)
        {
            captured = exception;
        }

        using (var activity = CommunicationTelemetry.StartActivity("test-op"))
        {
            activity.ShouldNotBeNull();
            CommunicationTelemetry.RecordFailure(Problem.Create(captured), captured);
        }

        var recorded = _activities.Single(a => a.DisplayName == "test-op");
        var exceptionEvent = recorded.Events.SingleOrDefault(e => e.Name == "exception");

        // A Problem keeps only the type name and the message; without passing the exception the stack trace
        // never reaches the trace, which is the part that makes an incident diagnosable.
        exceptionEvent.Name.ShouldBe("exception");
        var tags = exceptionEvent.Tags.ToDictionary(tag => tag.Key, tag => tag.Value?.ToString());
        tags["exception.type"].ShouldBe(typeof(InvalidOperationException).FullName);
        tags["exception.message"].ShouldBe("the real cause");
        tags["exception.stacktrace"].ShouldNotBeNullOrWhiteSpace();
        tags["exception.stacktrace"]!.ShouldContain(nameof(RecordFailure_AttachesTheRealExceptionWithItsStackTrace));
    }

    [Test]
    public void RecordFailure_WithoutAnActivityDoesNothingHarmful()
    {
        var testActivity = Activity.Current;
        try
        {
            Activity.Current = null;
            Should.NotThrow(() => CommunicationTelemetry.RecordFailure(Problem.Create("boom", "d", 500)));
        }
        finally
        {
            Activity.Current = testActivity;
        }
    }

    [Test]
    public void RecordFailure_IgnoresSuccessfulResults()
    {
        using (CommunicationTelemetry.StartActivity("test-op"))
        {
            CommunicationTelemetry.RecordFailure(Result.Succeed());
            CommunicationTelemetry.RecordFailure(Result<int>.Succeed(1));
        }

        _activities.Single(a => a.DisplayName == "test-op").Status.ShouldBe(ActivityStatusCode.Unset);
    }

    [Test]
    public void FailureCounterIsIncrementedForEachFailure()
    {
        var measurements = new List<long>();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == CommunicationTelemetry.SourceName &&
                    instrument.Name == CommunicationTelemetry.FailureCounterName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };
        meterListener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            if (HasTag(tags, "error.type", "a") || HasTag(tags, "error.type", "b"))
            {
                measurements.Add(value);
            }
        });
        meterListener.Start();

        CommunicationTelemetry.RecordFailure(Problem.Create(
            CommandExecutionTestConstants.TelemetryTitleA,
            CommandExecutionTestConstants.TelemetryDetail,
            500,
            CommandExecutionTestConstants.TelemetryTypeA));
        CommunicationTelemetry.RecordFailure(Problem.Create(
            CommandExecutionTestConstants.TelemetryTitleB,
            CommandExecutionTestConstants.TelemetryDetail,
            404,
            CommandExecutionTestConstants.TelemetryTypeB));

        meterListener.RecordObservableInstruments();
        measurements.Count.ShouldBe(2);
        measurements.ShouldAllBe(value => value == 1);
    }

    [Test]
    public void Report_LeavesTheResultUnchangedSoItCanSitInAChain()
    {
        var failed = Result<int>.Fail(Problem.Create("boom", "d", 500));

        var returned = failed.Report(NullLogger.Instance);

        returned.IsFailed.ShouldBeTrue();
        returned.Problem!.Title.ShouldBe("boom");
    }

    [Test]
    public void Track_TurnsAThrownExceptionIntoAReportedFailure()
    {
        var result = CommunicationDiagnostics.Track<int>(
            "risky",
            () => throw new InvalidOperationException("exploded"),
            NullLogger.Instance);

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(nameof(InvalidOperationException));
        result.Problem!.Detail.ShouldBe("exploded");

        var recorded = _activities.Single(a => a.DisplayName == "risky");
        recorded.Status.ShouldBe(ActivityStatusCode.Error);
        recorded.Events.ShouldContain(e => e.Name == "exception");
    }

    [Test]
    public async Task TrackAsync_ReportsAFailedResultWithoutSwallowingIt()
    {
        var result = await CommunicationDiagnostics.TrackAsync(
            "risky-async",
            () => Task.FromResult(Result<int>.Fail(Problem.Create("nope", "denied", 403))),
            NullLogger.Instance);

        result.IsFailed.ShouldBeTrue();
        result.Problem!.StatusCode.ShouldBe(403);

        _activities.Single(a => a.DisplayName == "risky-async").Status.ShouldBe(ActivityStatusCode.Error);
    }

    [Test]
    public void Track_LeavesASuccessfulResultAlone()
    {
        var result = CommunicationDiagnostics.Track("fine", () => Result<int>.Succeed(42), NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
        _activities.Single(a => a.DisplayName == "fine").Status.ShouldBe(ActivityStatusCode.Unset);
    }

    [Test]
    public async Task CommandExecution_EmitsCorrelationTagsAndRetryEvents()
    {
        var command = Command.Create("payment.capture");
        command.CorrelationId = "correlation-a";
        command.TraceId = "upstream-trace";
        var options = new CommandExecutionOptions();
        options.Timeout.Enabled = false;
        options.Idempotency.Enabled = false;
        options.Retry.Enabled = true;
        options.Retry.MaxRetries = 1;
        options.Retry.Delay = TimeSpan.Zero;
        options.Retry.UseJitter = false;
        var attempt = 0;

        var result = await CommandExecutor.ExecuteResultAsync(
            command,
            (_, _) => Task.FromResult(++attempt == 1
                ? Result<int>.Fail(Problem.Create(HttpStatusCode.ServiceUnavailable))
                : Result<int>.Succeed(1)),
            new CommandExecutionRuntime(options));

        result.IsSuccess.ShouldBeTrue();
        var commandId = command.CommandId.ToString("D");
        var activity = _activities.Single(a =>
            a.DisplayName == "communication.command.execute"
            && Equals(a.GetTagItem("command.id"), commandId));
        activity.GetTagItem("command.type").ShouldBe("payment.capture");
        activity.GetTagItem("command.correlation_id").ShouldBe("correlation-a");
        activity.GetTagItem("command.trace_id").ShouldBe("upstream-trace");
        activity.Events.Count(e => e.Name == "command.attempt").ShouldBe(2);
        activity.Events.Count(e => e.Name == "command.retry").ShouldBe(1);
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Test]
    public async Task CommandExecution_WithSerializedW3CParent_ContinuesRemoteTrace()
    {
        var command = Command.Create(CommandExecutionTestConstants.TraceContinue);
        command.TraceId = CommandExecutionTestConstants.TraceId;
        command.SpanId = CommandExecutionTestConstants.SpanId;
        command.Metadata = new CommandMetadata
        {
            TraceRecorded = true,
            TraceState = CommandExecutionTestConstants.TraceState
        };
        var options = new CommandExecutionOptions();
        options.Timeout.Enabled = false;
        options.Idempotency.Enabled = false;
        var previous = Activity.Current;
        Activity.Current = null;
        try
        {
            await CommandExecutor.ExecuteResultAsync(
                command,
                static (_, _) => Task.FromResult(Result.Succeed()),
                new CommandExecutionRuntime(options));
        }
        finally
        {
            Activity.Current = previous;
        }

        var activity = _activities.Single(item =>
            item.DisplayName == CommunicationTelemetry.CommandExecutionActivityName
            && Equals(
                item.GetTagItem(CommunicationTelemetry.CommandIdTag),
                command.CommandId.ToString(CommandExecutionTestConstants.CommandIdFormat)));
        activity.TraceId.ToHexString().ShouldBe(command.TraceId);
        activity.ParentSpanId.ToHexString().ShouldBe(command.SpanId);
        activity.ParentId.ShouldNotBeNull();
    }

    [Test]
    public async Task CommandExecution_ThrownAttempt_CountsFinalFailureExactlyOnce()
    {
        var measurements = new Dictionary<string, long>(StringComparer.Ordinal);
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, current) =>
            {
                if (instrument.Meter.Name == CommunicationTelemetry.SourceName
                    && instrument.Name is CommunicationTelemetry.FailureCounterName
                        or CommunicationTelemetry.CommandAttemptFailureCounterName
                        or CommunicationTelemetry.ExceptionCounterName)
                {
                    current.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
            measurements[instrument.Name] = measurements.GetValueOrDefault(instrument.Name) + value);
        listener.Start();
        var options = new CommandExecutionOptions();
        options.Timeout.Enabled = false;
        options.Idempotency.Enabled = false;
        options.Retry.Enabled = false;

        var result = await CommandExecutor.ExecuteAsync(
            Command.Create(CommandExecutionTestConstants.MetricException),
            static (_, _) => Task.FromException<int>(new IOException(CommandExecutionTestConstants.NetworkFailure)),
            new CommandExecutionRuntime(options));

        result.IsFailed.ShouldBeTrue();
        measurements[CommunicationTelemetry.FailureCounterName].ShouldBe(1);
        measurements[CommunicationTelemetry.CommandAttemptFailureCounterName].ShouldBe(1);
        measurements[CommunicationTelemetry.ExceptionCounterName].ShouldBe(1);
    }

    [Test]
    public async Task CommandExecution_IncrementsAttemptAndRetryMetrics()
    {
        var measurements = new Dictionary<string, long>(StringComparer.Ordinal);
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == CommunicationTelemetry.SourceName &&
                    instrument.Name is CommunicationTelemetry.CommandAttemptCounterName
                        or CommunicationTelemetry.CommandRetryCounterName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            if (HasTag(tags, CommunicationTelemetry.CommandOperationTag, typeof(Command).FullName!))
            {
                measurements[instrument.Name] = measurements.GetValueOrDefault(instrument.Name) + value;
            }
        });
        meterListener.Start();

        var options = new CommandExecutionOptions();
        options.Timeout.Enabled = false;
        options.Idempotency.Enabled = false;
        options.Retry.Enabled = true;
        options.Retry.MaxRetries = 1;
        options.Retry.Delay = TimeSpan.Zero;
        options.Retry.UseJitter = false;
        var attempt = 0;

        await CommandExecutor.ExecuteResultAsync(
            Command.Create("metric.command"),
            (_, _) => Task.FromResult(++attempt == 1
                ? Result.Fail(Problem.Create(HttpStatusCode.ServiceUnavailable))
                : Result.Succeed()),
            new CommandExecutionRuntime(options));

        measurements[CommunicationTelemetry.CommandAttemptCounterName].ShouldBe(2);
        measurements[CommunicationTelemetry.CommandRetryCounterName].ShouldBe(1);
    }

    private static bool HasTag(
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        string key,
        object expectedValue)
    {
        foreach (var tag in tags)
        {
            if (tag.Key == key && Equals(tag.Value, expectedValue))
            {
                return true;
            }
        }

        return false;
    }
}
