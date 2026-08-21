using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ManagedCode.Communication.Logging;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication.Telemetry;

/// <summary>
///     One call that both logs a failure and records it on the current trace.
/// </summary>
/// <remarks>
///     Reporting a failure normally means doing two things that are easy to get out of step: writing a log entry
///     and marking the span. These helpers do both, and take the originating <see cref="Exception" /> so the stack
///     trace survives — a <see cref="Problem" /> built from an exception keeps only its type name and message.
/// </remarks>
public static class CommunicationDiagnostics
{
    /// <summary>
    ///     Logs a transient command-attempt exception and keeps the original stack trace without incrementing the
    ///     final-result failure counter.
    /// </summary>
    public static void ReportAttemptFailure(
        ILogger? logger,
        ICommand command,
        Problem problem,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(exception);
        CommunicationTelemetry.RecordAttemptFailure(command, problem, exception);
        if (logger is not null)
        {
            ProblemLoggerCenter.LogProblemWithException(
                logger,
                exception,
                problem.Title,
                problem.StatusCode,
                problem.Detail);
        }
    }

    /// <summary>Logs an infrastructure exception while leaving final execution failure counting to its caller.</summary>
    public static void ReportInfrastructureFailure(
        ILogger? logger,
        Problem problem,
        Exception exception,
        string phase = CommunicationTelemetry.ExecutionPhase)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(exception);
        CommunicationTelemetry.RecordInfrastructureFailure(problem, exception, phase);
        if (logger is not null)
        {
            ProblemLoggerCenter.LogProblemWithException(
                logger,
                exception,
                problem.Title,
                problem.StatusCode,
                problem.Detail);
        }
    }

    /// <summary>Logs a non-exception infrastructure failure outside the final-result failure counter.</summary>
    public static void ReportInfrastructureFailure(ILogger? logger, Problem problem, string phase)
    {
        ArgumentNullException.ThrowIfNull(problem);
        CommunicationTelemetry.RecordInfrastructureFailure(problem, phase);
        if (logger is not null)
        {
            ProblemLoggerCenter.LogProblem(logger, problem.Title, problem.StatusCode, problem.Detail);
        }
    }

    /// <summary>
    ///     Logs the problem and records it on the current activity.
    /// </summary>
    /// <param name="logger">Where to log. Pass <c>null</c> to record telemetry only.</param>
    /// <param name="problem">The failure.</param>
    /// <param name="exception">The exception behind the failure, when there was one.</param>
    public static void ReportFailure(ILogger? logger, Problem? problem, Exception? exception = null)
    {
        if (problem is null && exception is null)
        {
            return;
        }

        CommunicationTelemetry.RecordFailure(problem, exception);

        if (logger is null)
        {
            return;
        }

        if (problem is not null && TryDescribeValidation(problem, out var fieldCount, out var fields))
        {
            ProblemLoggerCenter.LogValidationProblem(logger, fieldCount, fields);
            return;
        }

        if (exception is not null)
        {
            ProblemLoggerCenter.LogProblemWithException(
                logger, exception, problem?.Title, problem?.StatusCode ?? 0, problem?.Detail);
            return;
        }

        ProblemLoggerCenter.LogProblem(logger, problem!.Title, problem.StatusCode, problem.Detail);
    }

    /// <summary>
    ///     Reports the failure carried by a <see cref="Result" /> and returns it unchanged, so it can sit inside a
    ///     chain without breaking it. A successful result is left alone.
    /// </summary>
    public static Result Report(this Result result, ILogger? logger = null, Exception? exception = null)
    {
        if (result.IsFailed)
        {
            ReportFailure(logger, result.Problem, exception);
        }

        return result;
    }

    /// <inheritdoc cref="Report(Result,ILogger,Exception)" />
    public static Result<T> Report<T>(this Result<T> result, ILogger? logger = null, Exception? exception = null)
    {
        if (result.IsFailed)
        {
            ReportFailure(logger, result.Problem, exception);
        }

        return result;
    }

    /// <summary>
    ///     Runs an operation inside a span named <paramref name="operationName" />, reporting any failure it
    ///     returns or throws.
    /// </summary>
    /// <remarks>
    ///     A thrown exception is converted into a failed <see cref="Result{T}" /> so the caller stays on the Result
    ///     path, while the exception itself — stack trace included — still reaches the log and the trace.
    /// </remarks>
    public static Result<T> Track<T>(string operationName, Func<Result<T>> operation, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentNullException.ThrowIfNull(operation);

        using var activity = CommunicationTelemetry.StartActivity(operationName);

        try
        {
            var result = operation();
            if (result.IsFailed)
            {
                ReportFailure(logger, result.Problem);
            }

            return result;
        }
        catch (Exception exception)
        {
            var problem = Problem.Create(exception);
            ReportFailure(logger, problem, exception);
            return Result<T>.Fail(problem);
        }
    }

    /// <inheritdoc cref="Track{T}(string,Func{Result{T}},ILogger)" />
    public static async Task<Result<T>> TrackAsync<T>(
        string operationName,
        Func<Task<Result<T>>> operation,
        ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentNullException.ThrowIfNull(operation);

        using var activity = CommunicationTelemetry.StartActivity(operationName);

        try
        {
            var result = await operation().ConfigureAwait(false);
            if (result.IsFailed)
            {
                ReportFailure(logger, result.Problem);
            }

            return result;
        }
        catch (Exception exception)
        {
            var problem = Problem.Create(exception);
            ReportFailure(logger, problem, exception);
            return Result<T>.Fail(problem);
        }
    }

    private static bool TryDescribeValidation(Problem problem, out int fieldCount, out string fields)
    {
        var errors = problem.GetValidationErrors();
        if (errors is null || errors.Count == 0)
        {
            fieldCount = 0;
            fields = string.Empty;
            return false;
        }

        fieldCount = errors.Count;
        fields = string.Join(", ", errors.Keys);
        return true;
    }
}
