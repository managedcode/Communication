using System;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication.Logging;

/// <summary>
///     Source-generated logging for failures, plus the one-call helpers that log and record telemetry together.
/// </summary>
/// <remarks>
///     Kept separate from <see cref="LoggerCenter" /> so failure logging has an obvious home: when a
///     <see cref="Problem" /> is built from an exception it keeps only the type name and the message, and the
///     original exception — with its stack trace and inner exceptions — is discarded. The overloads here take that
///     exception so it reaches both the log and the trace.
/// </remarks>
public static partial class ProblemLoggerCenter
{
    [LoggerMessage(
        EventId = 8001,
        Level = LogLevel.Error,
        Message = "Operation failed: {ProblemTitle} ({StatusCode}) — {ProblemDetail}")]
    public static partial void LogProblem(
        ILogger logger,
        string? problemTitle,
        int statusCode,
        string? problemDetail);

    [LoggerMessage(
        EventId = 8002,
        Level = LogLevel.Error,
        Message = "Operation failed with an exception: {ProblemTitle} ({StatusCode}) — {ProblemDetail}")]
    public static partial void LogProblemWithException(
        ILogger logger,
        Exception exception,
        string? problemTitle,
        int statusCode,
        string? problemDetail);

    [LoggerMessage(
        EventId = 8003,
        Level = LogLevel.Warning,
        Message = "Validation failed for {FieldCount} field(s): {Fields}")]
    public static partial void LogValidationProblem(
        ILogger logger,
        int fieldCount,
        string fields);

    [LoggerMessage(
        EventId = 8004,
        Level = LogLevel.Error,
        Message = "Exception converted to a Problem with status {StatusCode} in {Operation}")]
    public static partial void LogExceptionConverted(
        ILogger logger,
        Exception exception,
        int statusCode,
        string operation);
}
