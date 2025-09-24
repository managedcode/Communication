using System;
using ManagedCode.Communication.Commands;
using Orleans;

namespace ManagedCode.Communication.Surrogates;

[Immutable]
[GenerateSerializer]
public struct CommandTSurrogate<T>
{
    [Id(0)] public Guid CommandId;
    [Id(1)] public string CommandType;
    [Id(2)] public T? Value;
    [Id(3)] public DateTime Timestamp;
    [Id(4)] public string? CorrelationId;
    [Id(5)] public string? CausationId;
    [Id(6)] public string? TraceId;
    [Id(7)] public string? SpanId;
    [Id(8)] public string? UserId;
    [Id(9)] public string? SessionId;
    [Id(10)] public CommandMetadata? Metadata;

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
