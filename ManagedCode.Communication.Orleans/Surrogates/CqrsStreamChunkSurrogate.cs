using System;
using ManagedCode.Communication.CQRS;
using Orleans;

namespace ManagedCode.Communication.Orleans.Surrogates;

/// <summary>
///     Orleans serialization surrogate for <see cref="CqrsStreamChunk{TProgress,TResult}" />.
/// </summary>
/// <remarks>
///     Without this, Orleans refuses to start any silo whose grain interfaces mention a chunk: the type has no
///     generated serializer, and that is a configuration error rather than a runtime one.
/// </remarks>
[Immutable]
[GenerateSerializer]
public struct CqrsStreamChunkSurrogate<TProgress, TResult>
{
    public CqrsStreamChunkSurrogate(
        CqrsStreamChunkKind kind,
        Result<TProgress>? progressResult,
        Result<TResult>? final,
        string? message,
        string eventType,
        string? eventId,
        long? sequence,
        DateTime timestampUtc)
    {
        Kind = kind;
        ProgressResult = progressResult;
        Final = final;
        Message = message;
        EventType = eventType;
        EventId = eventId;
        Sequence = sequence;
        TimestampUtc = timestampUtc;
    }

    [Id(0)] public CqrsStreamChunkKind Kind;

    [Id(1)] public Result<TProgress>? ProgressResult;

    [Id(2)] public Result<TResult>? Final;

    [Id(3)] public string? Message;

    [Id(4)] public string EventType;

    [Id(5)] public string? EventId;

    [Id(6)] public long? Sequence;

    [Id(7)] public DateTime TimestampUtc;
}
