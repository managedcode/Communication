using System;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication.Logging;

/// <summary>
/// High-performance logging center using Source Generators for zero-allocation logging
/// </summary>
public static partial class LoggerCenter
{
    // Collection Result Logging
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Error,
        Message = "Error {Message} in {FileName} at line {LineNumber} in {Caller}")]
    public static partial void LogCollectionResultError(
        ILogger logger, Exception exception, string message, string fileName, int lineNumber, string caller);

    // Command Store Logging
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Cleaned up {Count} expired commands older than {MaxAge}")]
    public static partial void LogCommandCleanupExpired(
        ILogger logger, int count, TimeSpan maxAge);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Information,
        Message = "Cleaned up {Count} commands with status {Status} older than {MaxAge}")]
    public static partial void LogCommandCleanupByStatus(
        ILogger logger, int count, object status, TimeSpan maxAge);

    // Validation Filter Logging
    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Warning,
        Message = "Model validation failed for {ActionName}")]
    public static partial void LogValidationFailed(
        ILogger logger, string actionName);

    // Hub Exception Logging
    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Error,
        Message = "Unhandled exception in hub method {HubType}.{HubMethod}")]
    public static partial void LogHubException(
        ILogger logger, Exception exception, string hubType, string hubMethod);

    // Exception Filter Logging
    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Error,
        Message = "Unhandled exception in {ControllerName}.{ActionName}")]
    public static partial void LogControllerException(
        ILogger logger, Exception exception, string controllerName, string actionName);

    [LoggerMessage(
        EventId = 5002,
        Level = LogLevel.Information,
        Message = "Exception handled by {FilterType} for {ControllerName}.{ActionName}")]
    public static partial void LogExceptionHandled(
        ILogger logger, string filterType, string controllerName, string actionName);

    [LoggerMessage(
        EventId = 5003,
        Level = LogLevel.Error,
        Message = "Error occurred while handling exception in {FilterType}")]
    public static partial void LogFilterError(
        ILogger logger, Exception exception, string filterType);

    // Background Service Logging
    [LoggerMessage(
        EventId = 6001,
        Level = LogLevel.Information,
        Message = "Command cleanup service started with interval {Interval}")]
    public static partial void LogCleanupServiceStarted(
        ILogger logger, TimeSpan interval);

    [LoggerMessage(
        EventId = 6002,
        Level = LogLevel.Information,
        Message = "Cleaned up {Count} expired commands")]
    public static partial void LogCleanupCompleted(
        ILogger logger, int count);

    [LoggerMessage(
        EventId = 6003,
        Level = LogLevel.Information,
        Message = "Health metrics - Total: {TotalCommands}, Completed: {CompletedCommands}, Failed: {FailedCommands}, InProgress: {InProgressCommands}, FailureRate: {FailureRate:P2}, StuckRate: {StuckRate:P2}")]
    public static partial void LogHealthMetrics(
        ILogger logger, int totalCommands, int completedCommands, int failedCommands, 
        int inProgressCommands, double failureRate, double stuckRate);

    [LoggerMessage(
        EventId = 6004,
        Level = LogLevel.Error,
        Message = "Error during command cleanup")]
    public static partial void LogCleanupError(
        ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 6005,
        Level = LogLevel.Information,
        Message = "Command cleanup service stopped")]
    public static partial void LogCleanupServiceStopped(
        ILogger logger);
}