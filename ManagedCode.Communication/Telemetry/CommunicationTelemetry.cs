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

    private static readonly Meter MeterInstance = new(SourceName, ThisAssemblyVersion);

    private static readonly Counter<long> FailureCounter = MeterInstance.CreateCounter<long>(
        FailureCounterName,
        unit: "{failure}",
        description: "Number of failed results produced by the application.");

    private static readonly Counter<long> ExceptionCounter = MeterInstance.CreateCounter<long>(
        ExceptionCounterName,
        unit: "{exception}",
        description: "Number of exceptions converted into a Problem.");

    /// <summary>
    ///     Activity source for spans the library starts. Also the source whose current activity
    ///     <see cref="RecordFailure" /> annotates.
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

    private static string ThisAssemblyVersion =>
        typeof(CommunicationTelemetry).Assembly.GetName().Version?.ToString() ?? "0.0.0";
}
