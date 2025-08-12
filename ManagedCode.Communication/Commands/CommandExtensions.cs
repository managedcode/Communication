using System;

namespace ManagedCode.Communication.Commands;

/// <summary>
/// Extension methods for Command classes to provide fluent API
/// </summary>
public static class CommandExtensions
{
    /// <summary>
    /// Sets the correlation ID for the command
    /// </summary>
    public static T WithCorrelationId<T>(this T command, string correlationId) where T : ICommand
    {
        command.CorrelationId = correlationId;
        return command;
    }

    /// <summary>
    /// Sets the causation ID for the command
    /// </summary>
    public static T WithCausationId<T>(this T command, string causationId) where T : ICommand
    {
        command.CausationId = causationId;
        return command;
    }

    /// <summary>
    /// Sets the trace ID for the command
    /// </summary>
    public static T WithTraceId<T>(this T command, string traceId) where T : ICommand
    {
        command.TraceId = traceId;
        return command;
    }

    /// <summary>
    /// Sets the span ID for the command
    /// </summary>
    public static T WithSpanId<T>(this T command, string spanId) where T : ICommand
    {
        command.SpanId = spanId;
        return command;
    }

    /// <summary>
    /// Sets the user ID for the command
    /// </summary>
    public static T WithUserId<T>(this T command, string userId) where T : ICommand
    {
        command.UserId = userId;
        return command;
    }

    /// <summary>
    /// Sets the session ID for the command
    /// </summary>
    public static T WithSessionId<T>(this T command, string sessionId) where T : ICommand
    {
        command.SessionId = sessionId;
        return command;
    }

    /// <summary>
    /// Sets metadata for the command using a configuration action
    /// </summary>
    public static T WithMetadata<T>(this T command, Action<CommandMetadata> configureMetadata) where T : ICommand
    {
        command.Metadata ??= new CommandMetadata();
        configureMetadata(command.Metadata);
        return command;
    }

    /// <summary>
    /// Sets metadata for the command
    /// </summary>
    public static T WithMetadata<T>(this T command, CommandMetadata metadata) where T : ICommand
    {
        command.Metadata = metadata;
        return command;
    }
}