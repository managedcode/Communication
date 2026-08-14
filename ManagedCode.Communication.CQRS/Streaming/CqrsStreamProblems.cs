using System.Net;

namespace ManagedCode.Communication.CQRS;

/// <summary>
///     Well-known <see cref="Problem" /> titles produced by the CQRS streaming transport itself, as opposed to
///     failures reported by a command handler. Consumers can branch on these without string-matching messages.
/// </summary>
public static class CqrsStreamProblems
{
    /// <summary>
    ///     The stream ended without a terminal chunk — the handler neither completed nor failed.
    /// </summary>
    public const string IncompleteStream = "cqrs_stream_incomplete";

    /// <summary>
    ///     A received frame could not be decoded into a chunk.
    /// </summary>
    public const string MalformedChunk = "cqrs_stream_malformed_chunk";

    internal static Problem Incomplete()
    {
        return Problem.Create(
            IncompleteStream,
            "The command stream ended without emitting a terminal chunk.",
            (int)HttpStatusCode.InternalServerError);
    }

    internal static Problem Malformed(string detail)
    {
        return Problem.Create(
            MalformedChunk,
            detail,
            (int)HttpStatusCode.InternalServerError);
    }
}
