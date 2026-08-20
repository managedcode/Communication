using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ManagedCode.Communication.Telemetry;

/// <summary>
///     Traces and metrics for failures flowing through the library.
/// </summary>
/// <remarks>
///     <para>
///         Built on <see cref="System.Diagnostics.ActivitySource" /> and <see cref="System.Diagnostics.Metrics.Meter" />,
///         which ship with .NET. The library therefore takes no dependency on the OpenTelemetry SDK: an application
///         that uses OpenTelemetry subscribes to <see cref="SourceName" /> and the signals appear, and an application
///         that does not pays nothing — with no listener attached, recording is a couple of null checks.
///     </para>
///     <para>
///         Wire it up with:
///         <code>
///         builder.Services.AddOpenTelemetry()
///             .WithTracing(t =&gt; t.AddSource(CommunicationTelemetry.SourceName))
///             .WithMetrics(m =&gt; m.AddMeter(CommunicationTelemetry.SourceName));
///         </code>
///     </para>
/// </remarks>
public static class CommunicationTelemetry
{
    /// <summary>
    ///     Name of both the <see cref="ActivitySource" /> and the <see cref="Meter" />. Register this with your
    ///     tracer and meter providers.
    /// </summary>
    public const string SourceName = "ManagedCode.Communication";

    /// <summary>Counter of failed results, tagged by error type and status code.</summary>
    public const string FailureCounterName = "communication.result.failures";

    /// <summary>Counter of exceptions converted into a <see cref="Problem" />.</summary>
    public const string ExceptionCounterName = "communication.exceptions";

    /// <summary>Counter of command attempts.</summary>
    public const string CommandAttemptCounterName = "communication.command.attempts";

    /// <summary>Counter of command retries.</summary>
    public const string CommandRetryCounterName = "communication.command.retries";

    /// <summary>Counter of command executions that exhausted their retry budget.</summary>
    public const string CommandRetriesExhaustedCounterName = "communication.command.retries.exhausted";

    /// <summary>Counter of timed-out command executions.</summary>
    public const string CommandTimeoutCounterName = "communication.command.timeouts";

    /// <summary>Counter of commands queued by a rate limiter.</summary>
    public const string CommandRateLimitQueuedCounterName = "communication.command.rate_limit.queued";

    /// <summary>Counter of commands rejected by a rate limiter.</summary>
    public const string CommandRateLimitRejectedCounterName = "communication.command.rate_limit.rejected";

    /// <summary>Histogram of total command execution duration.</summary>
    public const string CommandDurationName = "communication.command.duration";

    /// <summary>Histogram of individual command attempt duration.</summary>
    public const string CommandAttemptDurationName = "communication.command.attempt.duration";

    /// <summary>Histogram of rate-limiter queue duration.</summary>
    public const string CommandRateLimitQueueDurationName = "communication.command.rate_limit.queue.duration";

    private static readonly Meter MeterInstance = new(SourceName, ThisAssemblyVersion);

    private static readonly Counter<long> FailureCounter = MeterInstance.CreateCounter<long>(
        FailureCounterName,
        unit: "{failure}",
        description: "Number of failed results produced by the application.");

    private static readonly Counter<long> ExceptionCounter = MeterInstance.CreateCounter<long>(
        ExceptionCounterName,
        unit: "{exception}",
        description: "Number of exceptions converted into a Problem.");

    private static readonly Counter<long> CommandAttemptCounter = MeterInstance.CreateCounter<long>(
        CommandAttemptCounterName,
        unit: "{attempt}");

    private static readonly Counter<long> CommandRetryCounter = MeterInstance.CreateCounter<long>(
        CommandRetryCounterName,
        unit: "{retry}");

    private static readonly Counter<long> CommandRetriesExhaustedCounter = MeterInstance.CreateCounter<long>(
        CommandRetriesExhaustedCounterName,
        unit: "{execution}");

    private static readonly Counter<long> CommandTimeoutCounter = MeterInstance.CreateCounter<long>(
        CommandTimeoutCounterName,
        unit: "{execution}");

    private static readonly Counter<long> CommandRateLimitQueuedCounter = MeterInstance.CreateCounter<long>(
        CommandRateLimitQueuedCounterName,
        unit: "{command}");

    private static readonly Counter<long> CommandRateLimitRejectedCounter = MeterInstance.CreateCounter<long>(
        CommandRateLimitRejectedCounterName,
        unit: "{command}");

    private static readonly Histogram<double> CommandDuration = MeterInstance.CreateHistogram<double>(
        CommandDurationName,
        unit: "ms");

    private static readonly Histogram<double> CommandAttemptDuration = MeterInstance.CreateHistogram<double>(
        CommandAttemptDurationName,
        unit: "ms");

    private static readonly Histogram<double> CommandRateLimitQueueDuration = MeterInstance.CreateHistogram<double>(
        CommandRateLimitQueueDurationName,
        unit: "ms");

    /// <summary>
    ///     Activity source for spans the library starts. Also the source whose current activity
    ///     <c>RecordFailure</c> annotates.
    /// </summary>
    public static ActivitySource ActivitySource { get; } = new(SourceName, ThisAssemblyVersion);

    /// <summary>
    ///     The meter carrying <see cref="FailureCounterName" /> and <see cref="ExceptionCounterName" />.
    /// </summary>
    public static Meter Meter => MeterInstance;

    /// <summary>
    ///     Records a failure: increments the failure counter and, when a span is in progress, marks it as errored
    ///     and tags it with the problem.
    /// </summary>
    /// <param name="problem">The failure to record.</param>
    /// <param name="exception">
    ///     The exception the problem came from, when there was one. Supplying it attaches the real type, message and
    ///     stack trace to the span — a <see cref="Problem" /> alone keeps only the type name and the message, so
    ///     without this the original stack trace never reaches your traces.
    /// </param>
    /// <param name="activity">Span to annotate; defaults to <see cref="Activity.Current" />.</param>
    public static void RecordFailure(Problem? problem, Exception? exception = null, Activity? activity = null)
    {
        if (problem is null && exception is null)
        {
            return;
        }

        var errorType = ResolveErrorType(problem, exception);
        var statusCode = problem?.StatusCode ?? 0;

        FailureCounter.Add(1, BuildTags(errorType, statusCode));

        if (exception is not null)
        {
            ExceptionCounter.Add(1, BuildTags(exception.GetType().FullName ?? errorType, statusCode));
        }

        var target = activity ?? Activity.Current;
        if (target is null)
        {
            return;
        }

        target.SetStatus(ActivityStatusCode.Error, problem?.Detail ?? exception?.Message);
        target.SetTag("error.type", errorType);

        if (problem is not null)
        {
            target.SetTag("problem.type", problem.Type);
            target.SetTag("problem.title", problem.Title);
            target.SetTag("problem.status", problem.StatusCode);

            if (!string.IsNullOrEmpty(problem.ErrorCode))
            {
                target.SetTag("problem.error_code", problem.ErrorCode);
            }
        }

        if (exception is not null)
        {
            // Carries type, message and stack trace as an exception event, which is what makes a trace
            // actionable; the Problem on its own has already discarded the stack.
            target.AddException(exception);
        }
    }

    /// <summary>
    ///     Records a failed <see cref="Result" />. Successful results are ignored.
    /// </summary>
    public static void RecordFailure(in Result result, Exception? exception = null, Activity? activity = null)
    {
        if (result.IsFailed)
        {
            RecordFailure(result.Problem, exception, activity);
        }
    }

    /// <summary>
    ///     Records a failed <see cref="Result{T}" />. Successful results are ignored.
    /// </summary>
    public static void RecordFailure<T>(in Result<T> result, Exception? exception = null, Activity? activity = null)
    {
        if (result.IsFailed)
        {
            RecordFailure(result.Problem, exception, activity);
        }
    }

    /// <summary>
    ///     Starts a span from <see cref="ActivitySource" />. Returns <c>null</c> when nothing is listening, which
    ///     callers can ignore — <c>using</c> on a null activity is a no-op.
    /// </summary>
    public static Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        return ActivitySource.StartActivity(name, kind);
    }

    /// <summary>Starts a command execution span and attaches command correlation metadata.</summary>
    public static Activity? StartCommandExecution(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var activity = ActivitySource.StartActivity("communication.command.execute", ActivityKind.Internal);
        if (activity is null)
        {
            return null;
        }

        activity.SetTag("command.type", command.CommandType);
        activity.SetTag("command.id", command.CommandId.ToString("D"));
        activity.SetTag("command.correlation_id", command.CorrelationId);
        activity.SetTag("command.causation_id", command.CausationId);
        activity.SetTag("command.trace_id", command.TraceId);
        activity.SetTag("command.span_id", command.SpanId);
        activity.SetTag("enduser.id", command.UserId);
        return activity;
    }

    /// <summary>Records one command handler or rate-limit attempt.</summary>
    public static void RecordCommandAttempt(
        ICommand command,
        int attempt,
        IResult result,
        TimeSpan duration)
    {
        var tags = BuildCommandTags(command, result.IsSuccess);
        CommandAttemptCounter.Add(1, tags);
        CommandAttemptDuration.Record(duration.TotalMilliseconds, tags);
        Activity.Current?.AddEvent(new ActivityEvent(
            "command.attempt",
            tags: new ActivityTagsCollection
            {
                { "command.attempt", attempt },
                { "command.success", result.IsSuccess }
            }));
    }

    /// <summary>Records the final result and duration of a command execution.</summary>
    public static void RecordCommandCompleted(
        ICommand command,
        IResult result,
        TimeSpan duration)
    {
        var tags = BuildCommandTags(command, result.IsSuccess);
        CommandDuration.Record(duration.TotalMilliseconds, tags);

        if (result.IsFailed)
        {
            RecordFailure(result.Problem, activity: Activity.Current);
        }
        else
        {
            Activity.Current?.SetStatus(ActivityStatusCode.Ok);
        }
    }

    /// <summary>Records a scheduled retry.</summary>
    public static void RecordCommandRetry(
        ICommand command,
        int attempt,
        TimeSpan delay,
        Problem problem,
        Activity? activity = null)
    {
        CommandRetryCounter.Add(1, BuildCommandTags(command, false));
        (activity ?? Activity.Current)?.AddEvent(new ActivityEvent(
            "command.retry",
            tags: new ActivityTagsCollection
            {
                { "command.attempt", attempt },
                { "command.retry_delay_ms", delay.TotalMilliseconds },
                { "problem.status", problem.StatusCode }
            }));
    }

    /// <summary>Records retry-budget exhaustion.</summary>
    public static void RecordRetriesExhausted(
        ICommand command,
        int attempts,
        Problem problem,
        Activity? activity = null)
    {
        CommandRetriesExhaustedCounter.Add(1, BuildCommandTags(command, false));
        (activity ?? Activity.Current)?.AddEvent(new ActivityEvent(
            "command.retries.exhausted",
            tags: new ActivityTagsCollection
            {
                { "command.attempts", attempts },
                { "problem.status", problem.StatusCode }
            }));
    }

    /// <summary>Records an execution timeout.</summary>
    public static void RecordCommandTimeout(ICommand command, Problem problem, Activity? activity = null)
    {
        CommandTimeoutCounter.Add(1, BuildCommandTags(command, false));
        (activity ?? Activity.Current)?.AddEvent(new ActivityEvent(
            "command.timeout",
            tags: new ActivityTagsCollection { { "problem.status", problem.StatusCode } }));
    }

    /// <summary>Records rate-limit queueing.</summary>
    public static void RecordRateLimitQueued(ICommand command, TimeSpan queueDuration)
    {
        var tags = BuildCommandTags(command, false);
        CommandRateLimitQueuedCounter.Add(1, tags);
        CommandRateLimitQueueDuration.Record(queueDuration.TotalMilliseconds, tags);
        Activity.Current?.AddEvent(new ActivityEvent("command.rate_limit.queued"));
    }

    /// <summary>Records rate-limit rejection.</summary>
    public static void RecordRateLimitRejected(ICommand command, Problem problem)
    {
        CommandRateLimitRejectedCounter.Add(1, BuildCommandTags(command, false));
        Activity.Current?.AddEvent(new ActivityEvent(
            "command.rate_limit.rejected",
            tags: new ActivityTagsCollection { { "problem.status", problem.StatusCode } }));
    }

    private static string ResolveErrorType(Problem? problem, Exception? exception)
    {
        if (exception is not null)
        {
            return exception.GetType().Name;
        }

        if (problem is null)
        {
            return "unknown";
        }

        return !string.IsNullOrEmpty(problem.ErrorCode)
            ? problem.ErrorCode
            : problem.Title ?? problem.Type;
    }

    private static TagList BuildTags(string errorType, int statusCode)
    {
        var tags = new TagList { { "error.type", errorType } };

        if (statusCode != 0)
        {
            tags.Add("problem.status", statusCode);
        }

        return tags;
    }

    private static TagList BuildCommandTags(ICommand command, bool success)
    {
        return new TagList
        {
            { "command.type", command.CommandType },
            { "command.success", success }
        };
    }

    private static string ThisAssemblyVersion =>
        typeof(CommunicationTelemetry).Assembly.GetName().Version?.ToString() ?? "0.0.0";
}
