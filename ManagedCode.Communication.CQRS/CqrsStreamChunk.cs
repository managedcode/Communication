using System;
using ManagedCode.Communication;

namespace ManagedCode.Communication.CQRS;

/// <summary>
///     Contract state for a streamed CQRS command chunk.
/// </summary>
public enum CqrsStreamChunkKind
{
    /// <summary>
    ///     First message in the stream, usually command start metadata.
    /// </summary>
    Started,

    /// <summary>
    ///     Intermediate progress update while command work is still in-flight.
    /// </summary>
    Progress,

    /// <summary>
    ///     Command completed successfully and final object can be read from <see cref="Final" />.
    /// </summary>
    Completed,

    /// <summary>
    ///     Command failed and failure details should be available in <see cref="Final" />.
    /// </summary>
    Failed
}

/// <summary>
///     A chunk of a CQRS stream. It emits progress updates and a terminal command outcome.
/// </summary>
public sealed class CqrsStreamChunk<TProgress, TResult>
{
    private const string DefaultStartedEvent = "cqrs-started";
    private const string DefaultProgressEvent = "cqrs-progress";
    private const string DefaultCompletedEvent = "cqrs-completed";
    private const string DefaultFailedEvent = "cqrs-failed";

    /// <summary>
    ///     Creates a started chunk.
    /// </summary>
    public static CqrsStreamChunk<TProgress, TResult> Started(
        Result<TProgress>? progress = null,
        string? message = null,
        string? eventType = null,
        string? eventId = null,
        long? sequence = null,
        DateTime? timestampUtc = null)
    {
        return new CqrsStreamChunk<TProgress, TResult>(
            CqrsStreamChunkKind.Started,
            progress,
            null,
            message,
            ResolveEventType(CqrsStreamChunkKind.Started, eventType),
            eventId,
            sequence,
            timestampUtc);
    }

    /// <summary>
    ///     Creates a progress chunk.
    /// </summary>
    public static CqrsStreamChunk<TProgress, TResult> Progress(
        Result<TProgress> progress,
        string? message = null,
        string? eventType = null,
        string? eventId = null,
        long? sequence = null,
        DateTime? timestampUtc = null)
    {
        return new CqrsStreamChunk<TProgress, TResult>(
            CqrsStreamChunkKind.Progress,
            progress,
            null,
            message,
            ResolveEventType(CqrsStreamChunkKind.Progress, eventType),
            eventId,
            sequence,
            timestampUtc);
    }

    /// <summary>
    ///     Creates a completed terminal chunk.
    /// </summary>
    public static CqrsStreamChunk<TProgress, TResult> Completed(
        Result<TResult> final,
        string? message = null,
        string? eventType = null,
        string? eventId = null,
        long? sequence = null,
        DateTime? timestampUtc = null)
    {
        if (!final.IsSuccess)
        {
            throw new ArgumentException("Completed stream chunk requires a successful result.", nameof(final));
        }

        return new CqrsStreamChunk<TProgress, TResult>(
            CqrsStreamChunkKind.Completed,
            null,
            final,
            message,
            ResolveEventType(CqrsStreamChunkKind.Completed, eventType),
            eventId,
            sequence,
            timestampUtc);
    }

    /// <summary>
    ///     Creates a failed terminal chunk.
    /// </summary>
    public static CqrsStreamChunk<TProgress, TResult> Failed(
        Result<TResult> final,
        string? message = null,
        string? eventType = null,
        string? eventId = null,
        long? sequence = null,
        DateTime? timestampUtc = null)
    {
        if (final.IsSuccess)
        {
            throw new ArgumentException("Failed stream chunk requires a failed result.", nameof(final));
        }

        return new CqrsStreamChunk<TProgress, TResult>(
            CqrsStreamChunkKind.Failed,
            null,
            final,
            message,
            ResolveEventType(CqrsStreamChunkKind.Failed, eventType),
            eventId,
            sequence,
            timestampUtc);
    }

    /// <summary>
    ///     Creates a failed terminal chunk from a <see cref="Problem" />.
    /// </summary>
    public static CqrsStreamChunk<TProgress, TResult> Failed(
        Problem problem,
        string? message = null,
        string? eventType = null,
        string? eventId = null,
        long? sequence = null,
        DateTime? timestampUtc = null)
    {
        return Failed(Result<TResult>.Fail(problem), message, eventType, eventId, sequence, timestampUtc);
    }

    public CqrsStreamChunk()
    {
    }

    public CqrsStreamChunk(
        CqrsStreamChunkKind kind,
        Result<TProgress>? progressResult,
        Result<TResult>? final,
        string? message,
        string? eventType,
        string? eventId,
        long? sequence,
        DateTime? timestampUtc)
    {
        Kind = kind;
        ProgressResult = progressResult;
        Final = final;
        Message = message;
        EventType = eventType ?? ResolveEventType(kind, null);
        EventId = eventId;
        Sequence = sequence;
        TimestampUtc = timestampUtc ?? DateTime.UtcNow;
    }

    /// <summary>
    ///     Chunk kind in the stream lifecycle.
    /// </summary>
    public CqrsStreamChunkKind Kind { get; init; }

    /// <summary>
    ///     Progress payload. Set for <see cref="CqrsStreamChunkKind.Started" /> and
    ///     <see cref="CqrsStreamChunkKind.Progress" /> chunks.
    /// </summary>
    public Result<TProgress>? ProgressResult { get; init; }

    /// <summary>
    ///     Final terminal payload. Set for <see cref="CqrsStreamChunkKind.Completed" /> and
    ///     <see cref="CqrsStreamChunkKind.Failed" /> chunks.
    /// </summary>
    public Result<TResult>? Final { get; init; }

    /// <summary>
    ///     Human-readable chunk message.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    ///     SSE event type for this chunk.
    /// </summary>
    public string EventType
    {
        get => string.IsNullOrWhiteSpace(_eventType) ? ResolveEventType(Kind, null) : _eventType;
        init => _eventType = value;
    }

    /// <summary>
    ///     Optional SSE event id.
    /// </summary>
    public string? EventId { get; init; }

    /// <summary>
    ///     Optional sequence number to restore strict stream ordering.
    /// </summary>
    public long? Sequence { get; init; }

    /// <summary>
    ///     UTC timestamp for diagnostics and ordering.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    private string? _eventType;

    /// <summary>
    ///     Indicates this chunk carries an in-flight progress payload.
    /// </summary>
    public bool IsProgress => Kind is CqrsStreamChunkKind.Started or CqrsStreamChunkKind.Progress;

    /// <summary>
    ///     True when the stream reached terminal state.
    /// </summary>
    public bool IsTerminal => Kind is CqrsStreamChunkKind.Completed or CqrsStreamChunkKind.Failed;

    private static string ResolveEventType(CqrsStreamChunkKind kind, string? eventType)
    {
        if (!string.IsNullOrWhiteSpace(eventType))
        {
            return eventType!;
        }

        return kind switch
        {
            CqrsStreamChunkKind.Started => DefaultStartedEvent,
            CqrsStreamChunkKind.Progress => DefaultProgressEvent,
            CqrsStreamChunkKind.Completed => DefaultCompletedEvent,
            CqrsStreamChunkKind.Failed => DefaultFailedEvent,
            _ => "cqrs"
        };
    }
}
