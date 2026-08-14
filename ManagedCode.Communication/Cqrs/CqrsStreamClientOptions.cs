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
    /// <summary>
    ///     Shared defaults: web-style JSON, malformed frames become a terminal failure, and the stream is guaranteed
    ///     to end on a terminal chunk.
    /// </summary>
    public static CqrsStreamClientOptions Default { get; } = new();

    /// <summary>
    ///     JSON options used to decode chunk payloads. Defaults to <see cref="JsonSerializerDefaults.Web" />.
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; init; }

    /// <summary>
    ///     How to react to a frame that cannot be decoded.
    /// </summary>
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

    internal JsonSerializerOptions ResolveJsonOptions()
    {
        return JsonSerializerOptions ?? CqrsStreamSerialization.Default;
    }
}
