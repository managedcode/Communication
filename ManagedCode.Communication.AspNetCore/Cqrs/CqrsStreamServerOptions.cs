namespace ManagedCode.Communication.AspNetCore;

/// <summary>
///     Server-side behaviour applied when an <c>IAsyncEnumerable&lt;CqrsStreamChunk&lt;,&gt;&gt;</c> is converted into a
///     Server-Sent Events response.
/// </summary>
public sealed class CqrsStreamServerOptions
{
    /// <summary>
    ///     Shared defaults, used when the application has not registered its own options.
    /// </summary>
    /// <remarks>
    ///     A single shared instance. Do not mutate it — that would change behaviour process-wide. To change defaults,
    ///     call <c>AddCommunicationCqrs(options =&gt; …)</c>, or pass your own instance to
    ///     <c>WithCommunicationCqrsResults(options)</c> for a single endpoint.
    /// </remarks>
    public static CqrsStreamServerOptions Default { get; } = new();

    /// <summary>
    ///     Fill in <c>CqrsStreamChunk.Sequence</c> for chunks a handler emitted without one,
    ///     and derive the SSE <c>id:</c> field from it. Lets clients restore ordering and resume by
    ///     <c>Last-Event-ID</c> without every handler having to number its own chunks.
    /// </summary>
    public bool AssignSequenceNumbers { get; set; } = true;

    /// <summary>
    ///     When a handler's stream ends without a terminal chunk, append a terminal
    ///     <c>CqrsStreamChunkKind.Failed</c> chunk carrying <c>CqrsStreamProblems.IncompleteStream</c>
    ///     so consumers never have to guess whether the command finished.
    /// </summary>
    public bool EnsureTerminalChunk { get; set; } = true;
}
