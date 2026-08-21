using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using ManagedCode.Communication.Commands.Execution;

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

    /// <summary>Counter of failed physical command attempts.</summary>
    public const string CommandAttemptFailureCounterName = "communication.command.attempt.failures";

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

    /// <summary>Counter of commands rejected by an open or isolated circuit.</summary>
    public const string CommandCircuitRejectedCounterName = "communication.command.circuit.rejected";

    /// <summary>Counter of circuit-breaker state transitions.</summary>
    public const string CommandCircuitTransitionCounterName = "communication.command.circuit.transitions";

    /// <summary>Counter of idempotency coordination outcomes.</summary>
    public const string CommandIdempotencyEventCounterName = "communication.command.idempotency.events";

    /// <summary>Up/down counter of active command executions.</summary>
    public const string CommandActiveExecutionName = "communication.command.active";

    /// <summary>Up/down counter of commands currently waiting for rate-limit admission.</summary>
    public const string CommandRateLimitQueueActiveName = "communication.command.rate_limit.queue.active";

    /// <summary>Counter of execution-infrastructure, callback, cleanup, and coordination failures.</summary>
    public const string CommandInfrastructureFailureCounterName = "communication.command.infrastructure.failures";

    /// <summary>Histogram of total command execution duration.</summary>
    public const string CommandDurationName = "communication.command.duration";

    /// <summary>Histogram of individual command attempt duration.</summary>
    public const string CommandAttemptDurationName = "communication.command.attempt.duration";

    /// <summary>Histogram of rate-limiter queue duration.</summary>
    public const string CommandRateLimitQueueDurationName = "communication.command.rate_limit.queue.duration";

    /// <summary>Default phase used for command execution infrastructure failures.</summary>
    internal const string ExecutionPhase = "execution";

    internal const string CommandExecutionActivityName = "communication.command.execute";
    internal const string CommandAttemptEventName = "command.attempt";
    internal const string CommandAttemptFailureEventName = "command.attempt.failure";
    internal const string CommandInfrastructureFailureEventName = "command.infrastructure.failure";
    internal const string CommandRetryEventName = "command.retry";
    internal const string CommandRetriesExhaustedEventName = "command.retries.exhausted";
    internal const string CommandTimeoutEventName = "command.timeout";
    internal const string CommandRateLimitQueuedEventName = "command.rate_limit.queued";
    internal const string CommandRateLimitRejectedEventName = "command.rate_limit.rejected";
    internal const string CommandCircuitRejectedEventName = "command.circuit.rejected";
    internal const string CommandCircuitTransitionEventName = "command.circuit.transition";
    internal const string CommandIdempotencyEventName = "command.idempotency";

    internal const string ErrorTypeTag = "error.type";
    internal const string ProblemTypeTag = "problem.type";
    internal const string ProblemTitleTag = "problem.title";
    internal const string ProblemStatusTag = "problem.status";
    internal const string ProblemErrorCodeTag = "problem.error_code";
    internal const string InfrastructurePhaseTag = "infrastructure.phase";
    internal const string CommandTypeTag = "command.type";
    internal const string CommandIdTag = "command.id";
    internal const string CommandCorrelationIdTag = "command.correlation_id";
    internal const string CommandCausationIdTag = "command.causation_id";
    internal const string CommandTraceIdTag = "command.trace_id";
    internal const string CommandSpanIdTag = "command.span_id";
    internal const string CommandUserPresentTag = "command.user_present";
    internal const string CommandAttemptTag = "command.attempt";
    internal const string CommandAttemptsTag = "command.attempts";
    internal const string CommandSuccessTag = "command.success";
    internal const string CommandRetryDelayMillisecondsTag = "command.retry_delay_ms";
    internal const string CommandOperationTag = "command.operation";
    internal const string CircuitStateTag = "circuit.state";
    internal const string CircuitPreviousStateTag = "circuit.previous_state";
    internal const string CircuitPartitionTag = "circuit.partition";
    internal const string CircuitRetryAfterMillisecondsTag = "circuit.retry_after_ms";
    internal const string IdempotencyOutcomeTag = "idempotency.outcome";

    private const string FailureUnit = "{failure}";
    private const string ExceptionUnit = "{exception}";
    private const string AttemptUnit = "{attempt}";
    private const string RetryUnit = "{retry}";
    private const string ExecutionUnit = "{execution}";
    private const string CommandUnit = "{command}";
    private const string TransitionUnit = "{transition}";
    private const string EventUnit = "{event}";
    private const string MillisecondsUnit = "ms";
    private const string FailureCounterDescription = "Number of failed results produced by the application.";
    private const string ExceptionCounterDescription = "Number of exceptions converted into a Problem.";
    private const string CommandIdFormat = "D";
    private const string RecordedTraceFlags = "01";
    private const string UnrecordedTraceFlags = "00";
    private const string TraceParentFormat = "00-{0}-{1}-{2}";
    private const string UnknownErrorType = "unknown";
    private const string UnknownAssemblyVersion = "0.0.0";

    private static readonly Meter MeterInstance = new(SourceName, ThisAssemblyVersion);

    private static readonly Counter<long> FailureCounter = MeterInstance.CreateCounter<long>(
        FailureCounterName,
        unit: FailureUnit,
        description: FailureCounterDescription);

    private static readonly Counter<long> ExceptionCounter = MeterInstance.CreateCounter<long>(
        ExceptionCounterName,
        unit: ExceptionUnit,
        description: ExceptionCounterDescription);

    private static readonly Counter<long> CommandAttemptCounter = MeterInstance.CreateCounter<long>(
        CommandAttemptCounterName,
        unit: AttemptUnit);

    private static readonly Counter<long> CommandAttemptFailureCounter = MeterInstance.CreateCounter<long>(
        CommandAttemptFailureCounterName,
        unit: AttemptUnit);

    private static readonly Counter<long> CommandRetryCounter = MeterInstance.CreateCounter<long>(
        CommandRetryCounterName,
        unit: RetryUnit);

    private static readonly Counter<long> CommandRetriesExhaustedCounter = MeterInstance.CreateCounter<long>(
        CommandRetriesExhaustedCounterName,
        unit: ExecutionUnit);

    private static readonly Counter<long> CommandTimeoutCounter = MeterInstance.CreateCounter<long>(
        CommandTimeoutCounterName,
        unit: ExecutionUnit);

    private static readonly Counter<long> CommandRateLimitQueuedCounter = MeterInstance.CreateCounter<long>(
        CommandRateLimitQueuedCounterName,
        unit: CommandUnit);

    private static readonly Counter<long> CommandRateLimitRejectedCounter = MeterInstance.CreateCounter<long>(
        CommandRateLimitRejectedCounterName,
        unit: CommandUnit);

    private static readonly Counter<long> CommandCircuitRejectedCounter = MeterInstance.CreateCounter<long>(
        CommandCircuitRejectedCounterName,
        unit: CommandUnit);

    private static readonly Counter<long> CommandCircuitTransitionCounter = MeterInstance.CreateCounter<long>(
        CommandCircuitTransitionCounterName,
        unit: TransitionUnit);

    private static readonly Counter<long> CommandIdempotencyEventCounter = MeterInstance.CreateCounter<long>(
        CommandIdempotencyEventCounterName,
        unit: EventUnit);

    private static readonly UpDownCounter<long> CommandActiveExecution = MeterInstance.CreateUpDownCounter<long>(
        CommandActiveExecutionName,
        unit: CommandUnit);

    private static readonly UpDownCounter<long> CommandRateLimitQueueActive = MeterInstance.CreateUpDownCounter<long>(
        CommandRateLimitQueueActiveName,
        unit: CommandUnit);

    private static readonly Counter<long> CommandInfrastructureFailureCounter = MeterInstance.CreateCounter<long>(
        CommandInfrastructureFailureCounterName,
        unit: FailureUnit);

    private static readonly Histogram<double> CommandDuration = MeterInstance.CreateHistogram<double>(
        CommandDurationName,
        unit: MillisecondsUnit);

    private static readonly Histogram<double> CommandAttemptDuration = MeterInstance.CreateHistogram<double>(
        CommandAttemptDurationName,
        unit: MillisecondsUnit);

    private static readonly Histogram<double> CommandRateLimitQueueDuration = MeterInstance.CreateHistogram<double>(
        CommandRateLimitQueueDurationName,
        unit: MillisecondsUnit);

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
        target.SetTag(ErrorTypeTag, errorType);

        if (problem is not null)
        {
            target.SetTag(ProblemTypeTag, problem.Type);
            target.SetTag(ProblemTitleTag, problem.Title);
            target.SetTag(ProblemStatusTag, problem.StatusCode);

            if (!string.IsNullOrEmpty(problem.ErrorCode))
            {
                target.SetTag(ProblemErrorCodeTag, problem.ErrorCode);
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
    ///     Records an exception on the current span and exception counter without incrementing the final-result
    ///     failure counter. Command execution uses this for transient attempt failures so the final outcome is counted
    ///     exactly once by <see cref="RecordCommandCompleted" />.
    /// </summary>
    public static void RecordAttemptFailure(ICommand command, Problem problem, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(exception);

        var tags = BuildCommandTags(command, false);
        tags.Add(ErrorTypeTag, exception.GetType().Name);
        CommandAttemptFailureCounter.Add(1, tags);
        ExceptionCounter.Add(1, BuildTags(exception.GetType().FullName ?? exception.GetType().Name, problem.StatusCode));

        var activity = Activity.Current;
        if (activity is not null)
        {
            activity.AddException(exception);
            activity.AddEvent(new ActivityEvent(
                CommandAttemptFailureEventName,
                tags: new ActivityTagsCollection
                {
                    { ErrorTypeTag, exception.GetType().FullName },
                    { ProblemStatusTag, problem.StatusCode }
                }));
        }
    }

    /// <summary>Records an execution exception without incrementing the final-result failure counter.</summary>
    public static void RecordInfrastructureFailure(Problem problem, Exception exception, string phase)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrWhiteSpace(phase);
        ExceptionCounter.Add(1, BuildTags(exception.GetType().FullName ?? exception.GetType().Name, problem.StatusCode));
        var tags = new TagList
        {
            { InfrastructurePhaseTag, phase },
            { ErrorTypeTag, exception.GetType().Name }
        };
        CommandInfrastructureFailureCounter.Add(1, tags);
        Activity.Current?.AddException(exception);
        Activity.Current?.AddEvent(new ActivityEvent(
            CommandInfrastructureFailureEventName,
            tags: new ActivityTagsCollection { { InfrastructurePhaseTag, phase } }));
    }

    /// <summary>Records an infrastructure failure that was not caused by an exception.</summary>
    public static void RecordInfrastructureFailure(Problem problem, string phase)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentException.ThrowIfNullOrWhiteSpace(phase);
        var tags = new TagList
        {
            { InfrastructurePhaseTag, phase },
            { ErrorTypeTag, ResolveErrorType(problem, null) }
        };
        CommandInfrastructureFailureCounter.Add(1, tags);
        Activity.Current?.AddEvent(new ActivityEvent(
            CommandInfrastructureFailureEventName,
            tags: new ActivityTagsCollection { { InfrastructurePhaseTag, phase } }));
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
        var activity = TryCreateParentContext(command, out var parentContext)
            ? ActivitySource.StartActivity(
                CommandExecutionActivityName,
                ActivityKind.Internal,
                parentContext)
            : ActivitySource.StartActivity(CommandExecutionActivityName, ActivityKind.Internal);
        if (activity is null)
        {
            return null;
        }

        activity.SetTag(CommandTypeTag, command.CommandType);
        activity.SetTag(CommandIdTag, command.CommandId.ToString(CommandIdFormat));
        activity.SetTag(CommandCorrelationIdTag, command.CorrelationId);
        activity.SetTag(CommandCausationIdTag, command.CausationId);
        activity.SetTag(CommandTraceIdTag, command.TraceId);
        activity.SetTag(CommandSpanIdTag, command.SpanId);
        activity.SetTag(CommandUserPresentTag, !string.IsNullOrWhiteSpace(command.UserId));
        return activity;
    }

    private static bool TryCreateParentContext(ICommand command, out ActivityContext context)
    {
        if (string.IsNullOrWhiteSpace(command.TraceId) || string.IsNullOrWhiteSpace(command.SpanId))
        {
            context = default;
            return false;
        }

        var flags = command.Metadata?.TraceRecorded == true ? RecordedTraceFlags : UnrecordedTraceFlags;
        var traceParent = string.Format(TraceParentFormat, command.TraceId, command.SpanId, flags);
        return ActivityContext.TryParse(traceParent, command.Metadata?.TraceState, isRemote: true, out context);
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
            CommandAttemptEventName,
            tags: new ActivityTagsCollection
            {
                { CommandAttemptTag, attempt },
                { CommandSuccessTag, result.IsSuccess }
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
            CommandRetryEventName,
            tags: new ActivityTagsCollection
            {
                { CommandAttemptTag, attempt },
                { CommandRetryDelayMillisecondsTag, delay.TotalMilliseconds },
                { ProblemStatusTag, problem.StatusCode }
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
            CommandRetriesExhaustedEventName,
            tags: new ActivityTagsCollection
            {
                { CommandAttemptsTag, attempts },
                { ProblemStatusTag, problem.StatusCode }
            }));
    }

    /// <summary>Records an execution timeout.</summary>
    public static void RecordCommandTimeout(ICommand command, Problem problem, Activity? activity = null)
    {
        CommandTimeoutCounter.Add(1, BuildCommandTags(command, false));
        (activity ?? Activity.Current)?.AddEvent(new ActivityEvent(
            CommandTimeoutEventName,
            tags: new ActivityTagsCollection { { ProblemStatusTag, problem.StatusCode } }));
    }

    /// <summary>Records rate-limit queueing.</summary>
    public static void RecordRateLimitQueued(ICommand command, TimeSpan queueDuration)
    {
        var tags = BuildCommandTags(command, false);
        CommandRateLimitQueuedCounter.Add(1, tags);
        CommandRateLimitQueueDuration.Record(queueDuration.TotalMilliseconds, tags);
        Activity.Current?.AddEvent(new ActivityEvent(CommandRateLimitQueuedEventName));
    }

    /// <summary>Records rate-limit rejection.</summary>
    public static void RecordRateLimitRejected(ICommand command, Problem problem)
    {
        CommandRateLimitRejectedCounter.Add(1, BuildCommandTags(command, false));
        Activity.Current?.AddEvent(new ActivityEvent(
            CommandRateLimitRejectedEventName,
            tags: new ActivityTagsCollection { { ProblemStatusTag, problem.StatusCode } }));
    }

    /// <summary>Records a command rejected by an open, half-open, or isolated circuit.</summary>
    public static void RecordCircuitRejected(ICommand command, CommandCircuitBreakerLease lease)
    {
        var tags = BuildCommandTags(command, false);
        tags.Add(CircuitStateTag, lease.State.ToString());
        CommandCircuitRejectedCounter.Add(1, tags);
        Activity.Current?.AddEvent(new ActivityEvent(
            CommandCircuitRejectedEventName,
            tags: new ActivityTagsCollection
            {
                { CircuitPartitionTag, lease.PartitionKey },
                { CircuitStateTag, lease.State.ToString() },
                { CircuitRetryAfterMillisecondsTag, lease.RetryAfter.TotalMilliseconds }
            }));
    }

    /// <summary>Records a circuit-breaker state transition without using its partition as a metric tag.</summary>
    public static void RecordCircuitTransition(CommandCircuitBreakerEvent transition)
    {
        var tags = new TagList
        {
            { CircuitPreviousStateTag, transition.PreviousState.ToString() },
            { CircuitStateTag, transition.State.ToString() }
        };
        CommandCircuitTransitionCounter.Add(1, tags);
        Activity.Current?.AddEvent(new ActivityEvent(
            CommandCircuitTransitionEventName,
            tags: new ActivityTagsCollection
            {
                { CircuitPartitionTag, transition.PartitionKey },
                { CircuitPreviousStateTag, transition.PreviousState.ToString() },
                { CircuitStateTag, transition.State.ToString() }
            }));
    }

    /// <summary>Records one idempotency hit, miss, wait, conflict, indeterminate state, or store error.</summary>
    public static void RecordIdempotencyEvent(ICommand command, string outcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome);
        var tags = BuildOperationTags(command);
        tags.Add(IdempotencyOutcomeTag, outcome);
        CommandIdempotencyEventCounter.Add(1, tags);
        Activity.Current?.AddEvent(new ActivityEvent(
            CommandIdempotencyEventName,
            tags: new ActivityTagsCollection { { IdempotencyOutcomeTag, outcome } }));
    }

    /// <summary>Changes the active-execution count by one.</summary>
    public static void RecordActiveExecution(ICommand command, int delta) =>
        CommandActiveExecution.Add(delta, BuildOperationTags(command));

    /// <summary>Changes the active rate-limit queue count by one.</summary>
    public static void RecordActiveRateLimitQueue(ICommand command, int delta) =>
        CommandRateLimitQueueActive.Add(delta, BuildOperationTags(command));

    private static string ResolveErrorType(Problem? problem, Exception? exception)
    {
        if (exception is not null)
        {
            return exception.GetType().Name;
        }

        if (problem is null)
        {
            return UnknownErrorType;
        }

        return !string.IsNullOrEmpty(problem.ErrorCode)
            ? problem.ErrorCode
            : problem.Type;
    }

    private static TagList BuildTags(string errorType, int statusCode)
    {
        var tags = new TagList { { ErrorTypeTag, errorType } };

        if (statusCode != 0)
        {
            tags.Add(ProblemStatusTag, statusCode);
        }

        return tags;
    }

    private static TagList BuildCommandTags(ICommand command, bool success)
    {
        var tags = BuildOperationTags(command);
        tags.Add(CommandSuccessTag, success);
        return tags;
    }

    private static TagList BuildOperationTags(ICommand command) => new()
    {
        { CommandOperationTag, command.GetType().FullName ?? command.GetType().Name }
    };

    private static string ThisAssemblyVersion =>
        typeof(CommunicationTelemetry).Assembly.GetName().Version?.ToString() ?? UnknownAssemblyVersion;
}
