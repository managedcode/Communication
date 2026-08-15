using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Communication.CQRS;

/// <summary>
///     Consumes a CQRS stream, so a caller who only wants the answer does not have to write the loop.
/// </summary>
/// <remarks>
///     Draining a stream by hand means an <c>await foreach</c>, a list, a branch per chunk kind and a decision
///     about what a stream that simply stops means. These do it once: progress is handed to a callback as it
///     arrives and the terminal chunk becomes a <see cref="Result{TResult}" />.
///     <para>
///         Every method returns <see cref="Task{TResult}" /> rather than <c>ValueTask</c> so the async railway
///         operators chain straight onto the call — see the examples on <c>ToResultAsync</c>.
///     </para>
///     <para>
///         Nothing here blocks and nothing buffers unless you ask for it: <c>ToResultAsync</c> keeps only the
///         terminal chunk, while <c>ToOutcomeAsync</c> and <c>ToChunkListAsync</c> retain what their name says.
///     </para>
/// </remarks>
public static class CqrsStreamExtensions
{
    /// <summary>
    ///     Drains the stream and returns its terminal result, discarding progress.
    /// </summary>
    /// <typeparam name="TProgress">Progress payload type.</typeparam>
    /// <typeparam name="TResult">Terminal payload type.</typeparam>
    /// <param name="stream">The stream to drain.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    ///     The terminal result. A stream that ends without a terminal chunk fails with
    ///     <see cref="CqrsStreamProblems.IncompleteStream" /> rather than returning a success that never happened.
    /// </returns>
    /// <example>
    ///     <code>
    ///     var report = await grain.StreamAsync().ToResultAsync();
    ///     if (report.IsFailed) return report.Problem;
    ///     </code>
    /// </example>
    public static Task<Result<TResult>> ToResultAsync<TProgress, TResult>(
        this IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> stream,
        CancellationToken cancellationToken = default)
    {
        return ToResultAsync<TProgress, TResult>(stream, null, null, cancellationToken);
    }

    /// <summary>
    ///     Drains the stream, reporting each progress payload to <paramref name="onProgress" />, and returns its
    ///     terminal result.
    /// </summary>
    /// <typeparam name="TProgress">Progress payload type.</typeparam>
    /// <typeparam name="TResult">Terminal payload type.</typeparam>
    /// <param name="stream">The stream to drain.</param>
    /// <param name="onProgress">
    ///     Called once per progress payload, in order, before the next chunk is read. Progress chunks that carry
    ///     no payload are skipped rather than reported as <c>null</c>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The terminal result.</returns>
    /// <example>
    ///     <code>
    ///     var report = await client
    ///         .GetForCqrsStreamAsync&lt;ImportProgress, ImportReport&gt;("/import")
    ///         .ToResultAsync(progress =&gt; Console.WriteLine($"{progress.Percent}%"));
    ///     </code>
    /// </example>
    public static Task<Result<TResult>> ToResultAsync<TProgress, TResult>(
        this IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> stream,
        Action<TProgress> onProgress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(onProgress);

        return ToResultAsync<TProgress, TResult>(stream, onProgress, null, cancellationToken);
    }

    /// <summary>
    ///     Drains the stream, awaiting <paramref name="onProgress" /> for each progress payload, and returns its
    ///     terminal result.
    /// </summary>
    /// <typeparam name="TProgress">Progress payload type.</typeparam>
    /// <typeparam name="TResult">Terminal payload type.</typeparam>
    /// <param name="stream">The stream to drain.</param>
    /// <param name="onProgress">
    ///     Awaited once per progress payload, in order. The stream is not read again until it completes, so a slow
    ///     handler applies back-pressure rather than queueing up.
    /// </param>
    /// <param name="cancellationToken">Cancellation token, also passed to the callback.</param>
    /// <returns>The terminal result.</returns>
    /// <remarks>
    ///     The callback takes the cancellation token as a second parameter, which is also what keeps it from being
    ///     confused with the <see cref="Action{TProgress}" /> overload: an async lambda would otherwise bind to
    ///     that one and have its task silently dropped.
    /// </remarks>
    public static Task<Result<TResult>> ToResultAsync<TProgress, TResult>(
        this IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> stream,
        Func<TProgress, CancellationToken, Task> onProgress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(onProgress);

        return ToResultAsync(stream, null, onProgress, cancellationToken);
    }

    /// <summary>
    ///     Drains the stream and keeps everything: the terminal result, the progress payloads and the chunks.
    /// </summary>
    /// <typeparam name="TProgress">Progress payload type.</typeparam>
    /// <typeparam name="TResult">Terminal payload type.</typeparam>
    /// <param name="stream">The stream to drain.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The outcome of the whole stream.</returns>
    /// <example>
    ///     <code>
    ///     var outcome = await grain.StreamAsync().ToOutcomeAsync();
    ///
    ///     outcome.Chunks.Count.ShouldBe(3);
    ///     outcome.Progress.ShouldNotBeEmpty();
    ///     outcome.Value!.Status.ShouldBe("done");
    ///     </code>
    /// </example>
    public static async Task<CqrsStreamOutcome<TProgress, TResult>> ToOutcomeAsync<TProgress, TResult>(
        this IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var chunks = new List<CqrsStreamChunk<TProgress, TResult>>();
        var progress = new List<TProgress>();
        Result<TResult>? terminal = null;

        await foreach (var chunk in stream.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            chunks.Add(chunk);

            if (chunk.TryGetProgress(out var payload))
            {
                progress.Add(payload);
            }

            if (chunk.IsTerminal)
            {
                terminal = Terminal(chunk);
                break;
            }
        }

        return new CqrsStreamOutcome<TProgress, TResult>
        {
            Result = terminal ?? Result<TResult>.Fail(CqrsStreamProblems.Incomplete()),
            Progress = progress,
            Chunks = chunks
        };
    }

    /// <summary>
    ///     Collects the stream into a list of chunks, leaving interpretation to the caller.
    /// </summary>
    /// <typeparam name="TProgress">Progress payload type.</typeparam>
    /// <typeparam name="TResult">Terminal payload type.</typeparam>
    /// <param name="stream">The stream to drain.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Every chunk, in order.</returns>
    public static async Task<List<CqrsStreamChunk<TProgress, TResult>>> ToChunkListAsync<TProgress, TResult>(
        this IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var chunks = new List<CqrsStreamChunk<TProgress, TResult>>();

        await foreach (var chunk in stream.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            chunks.Add(chunk);
        }

        return chunks;
    }

    /// <summary>
    ///     Materializes the terminal result out of chunks that have already been collected.
    /// </summary>
    /// <remarks>
    ///     Named <c>ToStreamResult</c> rather than <c>ToResult</c> because the railway already has a
    ///     <c>ToResult&lt;T&gt;</c> that wraps any value at all. On a concrete <c>List&lt;&gt;</c> that one is
    ///     the exact match and binds first, so a <c>ToResult</c> here would silently never be called.
    /// </remarks>
    /// <typeparam name="TProgress">Progress payload type.</typeparam>
    /// <typeparam name="TResult">Terminal payload type.</typeparam>
    /// <param name="chunks">The chunks, in the order they arrived.</param>
    /// <returns>
    ///     The result carried by the first terminal chunk, or a failure carrying
    ///     <see cref="CqrsStreamProblems.IncompleteStream" /> when there is none.
    /// </returns>
    public static Result<TResult> ToStreamResult<TProgress, TResult>(
        this IEnumerable<CqrsStreamChunk<TProgress, TResult>> chunks)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        foreach (var chunk in chunks)
        {
            if (chunk.IsTerminal)
            {
                return Terminal(chunk);
            }
        }

        return Result<TResult>.Fail(CqrsStreamProblems.Incomplete());
    }

    private static async Task<Result<TResult>> ToResultAsync<TProgress, TResult>(
        IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> stream,
        Action<TProgress>? onProgress,
        Func<TProgress, CancellationToken, Task>? onProgressAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        await foreach (var chunk in stream.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if ((onProgress is not null || onProgressAsync is not null) && chunk.TryGetProgress(out var payload))
            {
                onProgress?.Invoke(payload);

                if (onProgressAsync is not null)
                {
                    await onProgressAsync(payload, cancellationToken).ConfigureAwait(false);
                }
            }

            if (chunk.IsTerminal)
            {
                return Terminal(chunk);
            }
        }

        return Result<TResult>.Fail(CqrsStreamProblems.Incomplete());
    }

    /// <summary>
    ///     Reads the result out of a terminal chunk, covering the case of one that claims to be terminal but
    ///     carries no payload at all.
    /// </summary>
    private static Result<TResult> Terminal<TProgress, TResult>(CqrsStreamChunk<TProgress, TResult> chunk)
    {
        if (chunk.Final is { } final)
        {
            return final;
        }

        return chunk.Problem is { } problem
            ? Result<TResult>.Fail(problem)
            : Result<TResult>.Fail(CqrsStreamProblems.Incomplete());
    }
}
