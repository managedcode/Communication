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
    /// <summary>
    ///     Creates the surrogate from its parts.
    /// </summary>
    public CqrsStreamChunkSurrogate(
        CqrsStreamChunkKind kind,
        Result<TProgress>? progressResult,
        Result<TResult>? final,
        string? message,
        string eventType,
        string? eventId,
        long? sequence)
    {
        Kind = kind;
        ProgressResult = progressResult;
        Final = final;
        Message = message;
        EventType = eventType;
        EventId = eventId;
        Sequence = sequence;
    }

    /// <summary>
    ///     Chunk lifecycle state.
    /// </summary>
    [Id(0)] public CqrsStreamChunkKind Kind;

    /// <summary>
    ///     Progress payload.
    /// </summary>
    [Id(1)] public Result<TProgress>? ProgressResult;

    /// <summary>
    ///     Terminal payload.
    /// </summary>
    [Id(2)] public Result<TResult>? Final;

    /// <summary>
    ///     Human-readable chunk message.
    /// </summary>
    [Id(3)] public string? Message;

    /// <summary>
    ///     SSE event name.
    /// </summary>
    [Id(4)] public string EventType;

    /// <summary>
    ///     SSE event id.
    /// </summary>
    [Id(5)] public string? EventId;

    /// <summary>
    ///     Position of the chunk in the stream.
    /// </summary>
    [Id(6)] public long? Sequence;
}
