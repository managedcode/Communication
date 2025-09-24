using System;
using ManagedCode.Communication.Commands;
using Orleans;

namespace ManagedCode.Communication.Surrogates;

[Immutable]
[GenerateSerializer]
public struct CommandSurrogate
{
    [Id(0)] public Guid CommandId;
    [Id(1)] public string CommandType;
    [Id(2)] public DateTime Timestamp;
    [Id(3)] public string? CorrelationId;
    [Id(4)] public string? CausationId;
    [Id(5)] public string? TraceId;
    [Id(6)] public string? SpanId;
    [Id(7)] public string? UserId;
    [Id(8)] public string? SessionId;
    [Id(9)] public CommandMetadata? Metadata;

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
