using System;
using ManagedCode.Communication.Commands;
using Orleans;

namespace ManagedCode.Communication.Orleans.Surrogates;

/// <summary>
///     Orleans serialization surrogate for <c>Command</c>.
/// </summary>
[Immutable]
[GenerateSerializer]
public struct CommandSurrogate
{
    /// <summary>
    ///     Identity of the command.
    /// </summary>
    [Id(0)] public Guid CommandId;
    /// <summary>
    ///     Logical command name.
    /// </summary>
    [Id(1)] public string CommandType;
    /// <summary>
    ///     When the command was created (UTC).
    /// </summary>
    [Id(2)] public DateTime Timestamp;
    /// <summary>
    ///     Correlation identifier.
    /// </summary>
    [Id(3)] public string? CorrelationId;
    /// <summary>
    ///     Identifier of the causing command.
    /// </summary>
    [Id(4)] public string? CausationId;
    /// <summary>
    ///     Distributed-tracing trace identifier.
    /// </summary>
    [Id(5)] public string? TraceId;
    /// <summary>
    ///     Distributed-tracing span identifier.
    /// </summary>
    [Id(6)] public string? SpanId;
    /// <summary>
    ///     Acting user.
    /// </summary>
    [Id(7)] public string? UserId;
    /// <summary>
    ///     Session the command belongs to.
    /// </summary>
    [Id(8)] public string? SessionId;
    /// <summary>
    ///     Execution policy and free-form metadata.
    /// </summary>
    [Id(9)] public CommandMetadata? Metadata;

    /// <summary>
    ///     Creates the surrogate from its parts.
    /// </summary>
    public CommandSurrogate(
        Guid commandId, 
        string commandType,
        DateTime timestamp,
        string? correlationId,
        string? causationId,
        string? traceId,
        string? spanId,
        string? userId,
        string? sessionId,
        CommandMetadata? metadata)
    {
        CommandId = commandId;
        CommandType = commandType;
        Timestamp = timestamp;
        CorrelationId = correlationId;
        CausationId = causationId;
        TraceId = traceId;
        SpanId = spanId;
        UserId = userId;
        SessionId = sessionId;
        Metadata = metadata;
    }
}
