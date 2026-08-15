using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Communication.CQRS;

/// <summary>
///     Push-style writer handed to a <c>CqrsStream.Create</c> handler. Sequence numbers are
///     assigned automatically and the terminal chunk is emitted by the stream itself, so a handler only reports progress.
/// </summary>
/// <typeparam name="TProgress">Progress payload type.</typeparam>
/// <typeparam name="TResult">Final (terminal) payload type.</typeparam>
public interface ICqrsStreamWriter<TProgress, TResult>
{
    /// <summary>
    ///     Token signalled when the consumer disconnects or cancels. Honour it in long-running work.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    ///     Emits the <see cref="CqrsStreamChunkKind.Started" /> chunk.
    /// </summary>
    ValueTask StartedAsync(Result<TProgress>? progress = null, string? message = null);

    /// <summary>
    ///     Emits the <see cref="CqrsStreamChunkKind.Started" /> chunk from a bare payload.
    /// </summary>
    ValueTask StartedAsync(TProgress progress, string? message = null);

    /// <summary>
    ///     Emits a <see cref="CqrsStreamChunkKind.Progress" /> chunk.
    /// </summary>
    ValueTask ProgressAsync(Result<TProgress> progress, string? message = null);

    /// <summary>
    ///     Emits a <see cref="CqrsStreamChunkKind.Progress" /> chunk from a bare payload.
    /// </summary>
    ValueTask ProgressAsync(TProgress progress, string? message = null);

    /// <summary>
    ///     Emits a pre-built chunk. Its <see cref="CqrsStreamChunk{TProgress,TResult}.Sequence" /> is assigned when unset.
    /// </summary>
    /// <remarks>
    ///     Terminal chunks written here are passed through, but the value returned by the handler is what decides the
    ///     stream outcome — prefer returning a <see cref="Result{TResult}" /> over writing a terminal chunk by hand.
    /// </remarks>
    ValueTask WriteAsync(CqrsStreamChunk<TProgress, TResult> chunk);
}
