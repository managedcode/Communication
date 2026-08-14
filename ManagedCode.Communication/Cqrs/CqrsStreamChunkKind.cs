using System.Text.Json.Serialization;

namespace ManagedCode.Communication.CQRS;

/// <summary>
///     Lifecycle state of a streamed CQRS command chunk.
/// </summary>
/// <remarks>
///     Serialized as a string (<c>"Started"</c>, <c>"Progress"</c>, …) so that adding members never shifts the
///     wire values of existing ones. Numeric payloads produced by older clients are still accepted on read.
///     The explicit numeric values are part of the contract and must never be reassigned.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<CqrsStreamChunkKind>))]
public enum CqrsStreamChunkKind
{
    /// <summary>
    ///     First message in the stream, usually command start metadata.
    /// </summary>
    Started = 0,

    /// <summary>
    ///     Intermediate progress update while command work is still in-flight.
    /// </summary>
    Progress = 1,

    /// <summary>
    ///     Command completed successfully; the final payload is available in
    ///     <see cref="CqrsStreamChunk{TProgress,TResult}.Final" />.
    /// </summary>
    Completed = 2,

    /// <summary>
    ///     Command failed; failure details are available in
    ///     <see cref="CqrsStreamChunk{TProgress,TResult}.Final" />.
    /// </summary>
    Failed = 3
}
