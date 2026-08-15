using System;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication.Logging;

/// <summary>
/// High-performance logging center using Source Generators for zero-allocation logging
/// </summary>
public static partial class LoggerCenter
{
    // Collection Result Logging
    /// <summary>
    ///     Logs an error raised while producing a collection result, with the call site.
    /// </summary>
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Error,
        Message = "Error {Message} in {FileName} at line {LineNumber} in {Caller}")]
    public static partial void LogCollectionResultError(
        ILogger logger, Exception exception, string message, string fileName, int lineNumber, string caller);

    // Command Store Logging
    /// <summary>
    ///     Logs how many expired commands a cleanup pass removed.
    /// </summary>
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Cleaned up {Count} expired commands older than {MaxAge}")]
    public static partial void LogCommandCleanupExpired(
        ILogger logger, int count, TimeSpan maxAge);

    /// <summary>
    ///     Logs how many commands in a given status a cleanup pass removed.
    /// </summary>
    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Information,
        Message = "Cleaned up {Count} commands with status {Status} older than {MaxAge}")]
    public static partial void LogCommandCleanupByStatus(
        ILogger logger, int count, object status, TimeSpan maxAge);

    // Validation Filter Logging
    /// <summary>
    ///     Logs a validation failure.
    /// </summary>
    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Warning,
        Message = "Model validation failed for {ActionName}")]
    public static partial void LogValidationFailed(
        ILogger logger, string actionName);

    // Hub Exception Logging
    /// <summary>
    ///     Logs an exception thrown by a SignalR hub method.
    /// </summary>
    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Error,
        Message = "Unhandled exception in hub method {HubType}.{HubMethod}")]
    public static partial void LogHubException(
        ILogger logger, Exception exception, string hubType, string hubMethod);

    // Exception Filter Logging
    /// <summary>
    ///     Logs an exception thrown by an MVC action.
    /// </summary>
    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Error,
        Message = "Unhandled exception in {ControllerName}.{ActionName}")]
    public static partial void LogControllerException(
        ILogger logger, Exception exception, string controllerName, string actionName);

    /// <summary>
    ///     Logs that a filter converted an exception into a failed result.
    /// </summary>
    [LoggerMessage(
        EventId = 5002,
        Level = LogLevel.Information,
        Message = "Exception handled by {FilterType} for {ControllerName}.{ActionName}")]
    public static partial void LogExceptionHandled(
        ILogger logger, string filterType, string controllerName, string actionName);

    /// <summary>
    ///     Logs a fault inside a filter itself.
    /// </summary>
    [LoggerMessage(
        EventId = 5003,
        Level = LogLevel.Error,
        Message = "Error occurred while handling exception in {FilterType}")]
    public static partial void LogFilterError(
        ILogger logger, Exception exception, string filterType);

    // Background Service Logging
    /// <summary>
    ///     Logs that the command cleanup background service started.
    /// </summary>
    [LoggerMessage(
        EventId = 6001,
        Level = LogLevel.Information,
        Message = "Command cleanup service started with interval {Interval}")]
    public static partial void LogCleanupServiceStarted(
        ILogger logger, TimeSpan interval);

    /// <summary>
    ///     Logs the outcome of a cleanup pass.
    /// </summary>
    [LoggerMessage(
        EventId = 6002,
        Level = LogLevel.Information,
        Message = "Cleaned up {Count} expired commands")]
    public static partial void LogCleanupCompleted(
        ILogger logger, int count);

    /// <summary>
    ///     Logs command counts by status.
    /// </summary>
    [LoggerMessage(
        EventId = 6003,
        Level = LogLevel.Information,
        Message = "Health metrics - Total: {TotalCommands}, Completed: {CompletedCommands}, Failed: {FailedCommands}, InProgress: {InProgressCommands}, FailureRate: {FailureRate:P2}, StuckRate: {StuckRate:P2}")]
    public static partial void LogHealthMetrics(
        ILogger logger, int totalCommands, int completedCommands, int failedCommands, 
        int inProgressCommands, double failureRate, double stuckRate);

    /// <summary>
    ///     Logs a fault during cleanup.
    /// </summary>
    [LoggerMessage(
        EventId = 6004,
        Level = LogLevel.Error,
        Message = "Error during command cleanup")]
    public static partial void LogCleanupError(
        ILogger logger, Exception exception);

    /// <summary>
    ///     Logs that the command cleanup background service stopped.
    /// </summary>
    [LoggerMessage(
        EventId = 6005,
        Level = LogLevel.Information,
        Message = "Command cleanup service stopped")]
    public static partial void LogCleanupServiceStopped(
        ILogger logger);

    // Orleans Grain Call Filter Logging
    /// <summary>
    ///     Logs that an Orleans grain call exception was converted into a failed result.
    /// </summary>
    [LoggerMessage(
        EventId = 7001,
        Level = LogLevel.Error,
        Message = "Unhandled exception in Orleans grain call {InterfaceName}.{MethodName} for target {TargetId}; converted to failed Communication result with status {StatusCode}")]
    public static partial void LogOrleansGrainCallExceptionConverted(
        ILogger logger,
        Exception exception,
        string interfaceName,
        string methodName,
        string targetId,
        int statusCode);
}
