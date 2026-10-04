using System.Net;

namespace ManagedCode.Communication.CQRS;

/// <summary>
///     Well-known <see cref="Problem" /> titles produced by the CQRS streaming transport itself, as opposed to
///     failures reported by a command handler. Consumers can branch on these without string-matching messages.
/// </summary>
public static class CqrsStreamProblems
{
    /// <summary>The stream ended without a terminal chunk.</summary>
    public const string IncompleteStream = "cqrs_stream_incomplete";

    /// <summary>A received frame could not be decoded into a chunk.</summary>
    public const string MalformedChunk = "cqrs_stream_malformed_chunk";

    /// <summary>A physical SSE frame exceeded its configured byte limit.</summary>
    public const string FrameLimitExceeded = "cqrs_stream_frame_limit_exceeded";

    /// <summary>A non-success HTTP body exceeded its configured byte limit.</summary>
    public const string FailureBodyLimitExceeded = "cqrs_stream_failure_body_limit_exceeded";

    /// <summary>A successful CQRS response exceeded its configured aggregate byte limit.</summary>
    public const string TotalLimitExceeded = "cqrs_stream_total_limit_exceeded";

    internal const string IncompleteDetail = "The command stream ended without emitting a terminal chunk.";
    internal const string MalformedDetail = "The server sent an invalid CQRS stream frame.";
    internal const string FrameLimitDetail = "The CQRS response frame exceeded its configured byte limit.";
    internal const string FailureBodyLimitDetail = "The HTTP failure response exceeded its configured byte limit.";
    internal const string TotalLimitDetail = "The CQRS response stream exceeded its configured byte limit.";
    internal const string NonSuccessDetail = "The HTTP request returned a non-success status code.";

    internal static Problem Incomplete()
    {
        return Problem.Create(IncompleteStream, IncompleteDetail, (int)HttpStatusCode.InternalServerError);
    }

    internal static Problem Malformed()
    {
        return Problem.Create(MalformedChunk, MalformedDetail, (int)HttpStatusCode.InternalServerError);
    }

    internal static Problem LimitExceeded(CqrsResponseLimitKind kind)
    {
        var (title, detail) = kind switch
        {
            CqrsResponseLimitKind.Frame => (FrameLimitExceeded, FrameLimitDetail),
            CqrsResponseLimitKind.Total => (TotalLimitExceeded, TotalLimitDetail),
            _ => (FailureBodyLimitExceeded, FailureBodyLimitDetail)
        };

        return Problem.Create(title, detail, (int)HttpStatusCode.InternalServerError);
    }

    internal static Problem FailureBodyLimitExceededFor(HttpStatusCode statusCode)
    {
        return Problem.Create(FailureBodyLimitExceeded, FailureBodyLimitDetail, (int)statusCode);
    }

    internal static Problem NonSuccess(HttpStatusCode statusCode)
    {
        return Problem.Create(statusCode.ToString(), NonSuccessDetail, (int)statusCode);
    }
}
