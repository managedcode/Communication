using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication.CQRS;

/// <summary>
///     A single chunk of a CQRS command stream. A well-formed stream emits zero or more progress chunks
///     (<see cref="CqrsStreamChunkKind.Started" /> / <see cref="CqrsStreamChunkKind.Progress" />) followed by exactly one
///     terminal chunk (<see cref="CqrsStreamChunkKind.Completed" /> / <see cref="CqrsStreamChunkKind.Failed" />).
/// </summary>
/// <typeparam name="TProgress">Progress payload type.</typeparam>
/// <typeparam name="TResult">Final (terminal) payload type.</typeparam>
public sealed record CqrsStreamChunk<TProgress, TResult>
{
    /// <summary>SSE <c>event:</c> name used for <see cref="CqrsStreamChunkKind.Started" /> chunks.</summary>
    public const string StartedEventType = "cqrs-started";

    /// <summary>SSE <c>event:</c> name used for <see cref="CqrsStreamChunkKind.Progress" /> chunks.</summary>
    public const string ProgressEventType = "cqrs-progress";

    /// <summary>SSE <c>event:</c> name used for <see cref="CqrsStreamChunkKind.Completed" /> chunks.</summary>
    public const string CompletedEventType = "cqrs-completed";

    /// <summary>SSE <c>event:</c> name used for <see cref="CqrsStreamChunkKind.Failed" /> chunks.</summary>
    public const string FailedEventType = "cqrs-failed";

    private readonly string? _eventType;
    private readonly DateTime _timestampUtc = DateTime.UtcNow;

    /// <summary>
    ///     Creates an empty chunk. Present for serializers; prefer the named factory methods.
    /// </summary>
    public CqrsStreamChunk()
    {
    }

    /// <summary>
    ///     Creates a fully specified chunk. Prefer <see cref="Started" />, <see cref="Progress(Result{TProgress},string,string,string,long?,DateTime?)" />,
    ///     <see cref="Completed(Result{TResult},string,string,string,long?,DateTime?)" /> or <see cref="Failed(Result{TResult},string?,string?,string?,long?,DateTime?)" />.
    /// </summary>
    public CqrsStreamChunk(
        CqrsStreamChunkKind kind,
        Result<TProgress>? progressResult = null,
        Result<TResult>? final = null,
        string? message = null,
        string? eventType = null,
        string? eventId = null,
        long? sequence = null,
        DateTime? timestampUtc = null)
    {
        Kind = kind;
        ProgressResult = progressResult;
        Final = final;
        Message = message;
        EventType = eventType!;
        EventId = eventId;
        Sequence = sequence;

        if (timestampUtc.HasValue)
        {
            TimestampUtc = timestampUtc.Value;
        }
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
    ///     Terminal payload. Set for <see cref="CqrsStreamChunkKind.Completed" /> and
    ///     <see cref="CqrsStreamChunkKind.Failed" /> chunks.
    /// </summary>
    public Result<TResult>? Final { get; init; }

    /// <summary>
    ///     Human-readable chunk message.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    ///     SSE <c>event:</c> name for this chunk. Falls back to the <see cref="Kind" /> default when unset or blank.
    /// </summary>
    public string EventType
    {
        get => string.IsNullOrWhiteSpace(_eventType) ? ResolveEventType(Kind) : _eventType;
        init => _eventType = value;
    }

    /// <summary>
    ///     SSE <c>id:</c> value. When unset, the transport derives one from <see cref="Sequence" />.
    /// </summary>
    public string? EventId { get; init; }

    /// <summary>
    ///     Monotonic position of this chunk within the stream. When a handler leaves it unset, the server transport
    ///     assigns one, so consumers can always rely on it to restore ordering.
    /// </summary>
    public long? Sequence { get; init; }

    /// <summary>
    ///     UTC timestamp for diagnostics and ordering. Always normalized to <see cref="DateTimeKind.Utc" />.
    /// </summary>
    public DateTime TimestampUtc
    {
        get => _timestampUtc;
        init => _timestampUtc = NormalizeToUtc(value);
    }

    /// <summary>
    ///     True when this chunk carries an in-flight progress payload.
    /// </summary>
    [JsonIgnore]
    public bool IsProgress => Kind is CqrsStreamChunkKind.Started or CqrsStreamChunkKind.Progress;

    /// <summary>
    ///     True when the stream reached a terminal state (either outcome).
    /// </summary>
    [JsonIgnore]
    public bool IsTerminal => Kind is CqrsStreamChunkKind.Completed or CqrsStreamChunkKind.Failed;

    /// <summary>
    ///     True when this is the terminal success chunk.
    /// </summary>
    [JsonIgnore]
    public bool IsCompleted => Kind is CqrsStreamChunkKind.Completed;

    /// <summary>
    ///     True when this is the terminal failure chunk.
    /// </summary>
    [JsonIgnore]
    public bool IsFailed => Kind is CqrsStreamChunkKind.Failed;

    /// <summary>
    ///     The failure carried by this chunk, from either payload, or <c>null</c> when the chunk is not a failure.
    /// </summary>
    [JsonIgnore]
    public Problem? Problem => Final is { IsFailed: true } final
        ? final.Problem
        : ProgressResult is { IsFailed: true } progress
            ? progress.Problem
            : null;

    /// <summary>
    ///     Gets the progress payload when this chunk carries a successful one.
    /// </summary>
    public bool TryGetProgress([MaybeNullWhen(false)] out TProgress progress)
    {
        if (ProgressResult is { IsSuccess: true, Value: not null } result)
        {
            progress = result.Value;
            return true;
        }

        progress = default;
        return false;
    }

    /// <summary>
    ///     Gets the terminal payload when this chunk is a successful completion.
    /// </summary>
    public bool TryGetResult([MaybeNullWhen(false)] out TResult result)
    {
        if (Final is { IsSuccess: true, Value: not null } final)
        {
            result = final.Value;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    ///     Gets the terminal failure when this chunk is a failure.
    /// </summary>
    public bool TryGetProblem([NotNullWhen(true)] out Problem? problem)
    {
        problem = Problem;
        return problem is not null;
    }

    /// <summary>
    ///     Creates a <see cref="CqrsStreamChunkKind.Started" /> chunk.
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
            eventType,
            eventId,
            sequence,
            timestampUtc);
    }

    /// <summary>
    ///     Creates a <see cref="CqrsStreamChunkKind.Progress" /> chunk.
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
            eventType,
            eventId,
            sequence,
            timestampUtc);
    }

    /// <summary>
    ///     Creates a <see cref="CqrsStreamChunkKind.Progress" /> chunk from a bare payload.
    /// </summary>
    public static CqrsStreamChunk<TProgress, TResult> Progress(
        TProgress progress,
        string? message = null,
        long? sequence = null)
    {
        return Progress(Result<TProgress>.Succeed(progress), message, sequence: sequence);
    }

    /// <summary>
    ///     Creates a terminal <see cref="CqrsStreamChunkKind.Completed" /> chunk.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="final" /> is a failed result.</exception>
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
            eventType,
            eventId,
            sequence,
            timestampUtc);
    }

    /// <summary>
    ///     Creates a terminal <see cref="CqrsStreamChunkKind.Completed" /> chunk from a bare payload.
    /// </summary>
    public static CqrsStreamChunk<TProgress, TResult> Completed(
        TResult final,
        string? message = null,
        long? sequence = null)
    {
        return Completed(Result<TResult>.Succeed(final), message, sequence: sequence);
    }

    /// <summary>
    ///     Creates a terminal <see cref="CqrsStreamChunkKind.Failed" /> chunk.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="final" /> is a successful result.</exception>
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
            eventType,
            eventId,
            sequence,
            timestampUtc);
    }

    /// <summary>
    ///     Creates a terminal <see cref="CqrsStreamChunkKind.Failed" /> chunk from a <see cref="Problem" />.
    /// </summary>
    public static CqrsStreamChunk<TProgress, TResult> Failed(
        Problem problem,
        string? message = null,
        string? eventType = null,
        string? eventId = null,
        long? sequence = null,
        DateTime? timestampUtc = null)
    {
        ArgumentNullException.ThrowIfNull(problem);

        return Failed(Result<TResult>.Fail(problem), message, eventType, eventId, sequence, timestampUtc);
    }

    /// <summary>
    ///     Creates a terminal <see cref="CqrsStreamChunkKind.Failed" /> chunk from an exception.
    /// </summary>
    /// <remarks>
    ///     Named rather than another <c>Failed</c> overload on purpose: <see cref="Result{TResult}" /> defines implicit
    ///     conversions from both <see cref="Exception" /> and <see cref="Problem" />, so an <c>Failed(Exception)</c>
    ///     overload would be ambiguous with <see cref="Failed(Result{TResult},string?,string?,string?,long?,DateTime?)" />
    ///     at every call site.
    /// </remarks>
    public static CqrsStreamChunk<TProgress, TResult> FromException(
        Exception exception,
        string? message = null,
        long? sequence = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return Failed(Problem.Create(exception), message, sequence: sequence);
    }

    /// <summary>
    ///     Default SSE <c>event:</c> name for a chunk kind.
    /// </summary>
    public static string ResolveEventType(CqrsStreamChunkKind kind)
    {
        return kind switch
        {
            CqrsStreamChunkKind.Started => StartedEventType,
            CqrsStreamChunkKind.Progress => ProgressEventType,
            CqrsStreamChunkKind.Completed => CompletedEventType,
            CqrsStreamChunkKind.Failed => FailedEventType,
            _ => "cqrs"
        };
    }

    private static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
