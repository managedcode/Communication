using System;
using System.Text.Json;

namespace ManagedCode.Communication.CQRS;

/// <summary>
///     What the client should do with an SSE frame it cannot decode into a chunk.
/// </summary>
public enum CqrsMalformedChunkBehavior
{
    /// <summary>
    ///     Emit a terminal <see cref="CqrsStreamChunkKind.Failed" /> chunk describing the decoding failure and end the
    ///     stream. Keeps the "a stream always ends on a terminal chunk" contract. This is the default.
    /// </summary>
    EmitFailedChunk = 0,

    /// <summary>
    ///     Ignore the frame and keep reading. Useful when a proxy injects frames the contract does not know about.
    /// </summary>
    Skip = 1,

    /// <summary>
    ///     Let the <see cref="JsonException" /> surface out of the enumeration.
    /// </summary>
    Throw = 2
}

/// <summary>
///     Client-side behaviour for reading a CQRS command stream.
/// </summary>
public sealed record CqrsStreamClientOptions
{
    /// <summary>Default maximum UTF8 bytes in one physical SSE frame.</summary>
    public const int DefaultMaximumFrameBytes = 16 * 1024 * 1024;

    /// <summary>Largest supported maximum UTF8 bytes in one physical SSE frame.</summary>
    public const int MaximumFrameBytesLimit = 64 * 1024 * 1024;

    /// <summary>Default maximum UTF8 bytes in a non-success HTTP body.</summary>
    public const int DefaultMaximumFailureBodyBytes = 64 * 1024;

    /// <summary>Largest supported maximum UTF8 bytes in a non-success HTTP body.</summary>
    public const int MaximumFailureBodyBytesLimit = 1024 * 1024;

    /// <summary>
    ///     Shared defaults: web-style JSON, finite frame and failure-body limits, malformed frames become a terminal
    ///     failure, and the stream is guaranteed to end on a terminal chunk.
    /// </summary>
    public static CqrsStreamClientOptions Default { get; } = new();

    /// <summary>Maximum UTF8 wire bytes in one physical SSE frame, including its ending delimiter.</summary>
    public int MaximumFrameBytes { get; init; } = DefaultMaximumFrameBytes;

    /// <summary>Maximum UTF8 response-body bytes inspected for a non-success HTTP response.</summary>
    public int MaximumFailureBodyBytes { get; init; } = DefaultMaximumFailureBodyBytes;

    /// <summary>
    ///     Optional maximum number of UTF8 bytes in the successful HTTP response body. It counts frames, heartbeat
    ///     comments and delimiters and remains unset by default for long-running streams.
    /// </summary>
    public long? MaximumStreamBytes { get; init; }

    /// <summary>
    ///     JSON options used to decode chunk payloads. Defaults to <see cref="JsonSerializerDefaults.Web" />.
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; init; }

    /// <summary>How to react to a bounded frame that cannot be decoded.</summary>
    public CqrsMalformedChunkBehavior MalformedChunkBehavior { get; init; } = CqrsMalformedChunkBehavior.EmitFailedChunk;

    /// <summary>
    ///     When the server stream ends without a terminal chunk, append a terminal
    ///     <see cref="CqrsStreamChunkKind.Failed" /> chunk carrying <see cref="CqrsStreamProblems.IncompleteStream" />.
    /// </summary>
    public bool EnsureTerminalChunk { get; init; } = true;

    /// <summary>
    ///     Fill in <see cref="CqrsStreamChunk{TProgress,TResult}.Sequence" /> for chunks that arrive without one.
    /// </summary>
    public bool AssignSequenceNumbers { get; init; } = true;

    internal void Validate()
    {
        if (MaximumFrameBytes is <= 0 or > MaximumFrameBytesLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(MaximumFrameBytes));
        }

        if (MaximumFailureBodyBytes is <= 0 or > MaximumFailureBodyBytesLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(MaximumFailureBodyBytes));
        }

        if (MaximumStreamBytes is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaximumStreamBytes));
        }

        if (!Enum.IsDefined(MalformedChunkBehavior))
        {
            throw new ArgumentOutOfRangeException(nameof(MalformedChunkBehavior));
        }
    }

    internal JsonSerializerOptions ResolveJsonOptions()
    {
        return JsonSerializerOptions ?? CqrsStreamSerialization.Default;
    }
}
