using System;
using ManagedCode.Communication.Commands;
using Orleans;

namespace ManagedCode.Communication.Orleans.Surrogates;

/// <summary>
///     Orleans serialization surrogate for <c>CommandT</c>.
/// </summary>
[Immutable]
[GenerateSerializer]
public struct CommandTSurrogate<T>
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
    ///     The payload carried by the original value.
    /// </summary>
    [Id(2)] public T? Value;
    /// <summary>
    ///     When the command was created (UTC).
    /// </summary>
    [Id(3)] public DateTime Timestamp;
    /// <summary>
    ///     Correlation identifier.
    /// </summary>
    [Id(4)] public string? CorrelationId;
    /// <summary>
    ///     Identifier of the causing command.
    /// </summary>
    [Id(5)] public string? CausationId;
    /// <summary>
    ///     Distributed-tracing trace identifier.
    /// </summary>
    [Id(6)] public string? TraceId;
    /// <summary>
    ///     Distributed-tracing span identifier.
    /// </summary>
    [Id(7)] public string? SpanId;
    /// <summary>
    ///     Acting user.
    /// </summary>
    [Id(8)] public string? UserId;
    /// <summary>
    ///     Session the command belongs to.
    /// </summary>
    [Id(9)] public string? SessionId;
    /// <summary>
    ///     Execution policy and free-form metadata.
    /// </summary>
    [Id(10)] public CommandMetadata? Metadata;

    /// <summary>
    ///     Creates the surrogate from its parts.
    /// </summary>
    public CommandTSurrogate(
        Guid commandId, 
        string commandType, 
        T? value,
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
        Value = value;
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
