using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ManagedCode.Communication.CQRS;

/// <summary>
///     Helpers for authoring CQRS command streams.
/// </summary>
/// <remarks>
///     <see cref="Create{TProgress,TResult}" /> is the recommended way to write a handler: it assigns sequence numbers,
///     guarantees exactly one terminal chunk, and turns an unhandled exception into a
///     <see cref="CqrsStreamChunkKind.Failed" /> chunk — none of which a hand-written iterator does for free.
/// </remarks>
public static class CqrsStream
{
    /// <summary>
    ///     Builds a command stream from a push-style handler.
    /// </summary>
    /// <example>
    ///     <code>
    ///     app.MapGet("/import", (CancellationToken ct) =&gt; CqrsStream.Create&lt;ImportProgress, ImportReport&gt;(
    ///         async writer =&gt;
    ///         {
    ///             await writer.StartedAsync(new ImportProgress(0));
    ///             for (var i = 1; i &lt;= 10; i++)
    ///             {
    ///                 await DoWorkAsync(writer.CancellationToken);
    ///                 await writer.ProgressAsync(new ImportProgress(i * 10));
    ///             }
    ///
    ///             return Result&lt;ImportReport&gt;.Succeed(new ImportReport(10));
    ///         }, ct))
    ///        .WithCommunicationCqrsResults();
    ///     </code>
    /// </example>
    /// <param name="handler">
    ///     Command body. Report progress through the writer and return the terminal outcome. A returned failed
    ///     <see cref="Result{TResult}" /> becomes a <see cref="CqrsStreamChunkKind.Failed" /> chunk; a thrown exception
    ///     becomes one too.
    /// </param>
    /// <param name="cancellationToken">Cancelled when the consumer disconnects.</param>
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> Create<TProgress, TResult>(
        Func<ICqrsStreamWriter<TProgress, TResult>, ValueTask<Result<TResult>>> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return CreateCore(handler, cancellationToken);
    }

    /// <summary>
    ///     Builds a command stream from a push-style handler returning a bare payload. Any thrown exception becomes a
    ///     <see cref="CqrsStreamChunkKind.Failed" /> chunk.
    /// </summary>
    public static IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> Create<TProgress, TResult>(
        Func<ICqrsStreamWriter<TProgress, TResult>, ValueTask<TResult>> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);

        return CreateCore<TProgress, TResult>(
            async writer => Result<TResult>.Succeed(await handler(writer).ConfigureAwait(false)),
            cancellationToken);
    }

    private static async IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> CreateCore<TProgress, TResult>(
        Func<ICqrsStreamWriter<TProgress, TResult>, ValueTask<Result<TResult>>> handler,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var streamCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = streamCancellation.Token;

        var channel = Channel.CreateBounded<CqrsStreamChunk<TProgress, TResult>>(
            new BoundedChannelOptions(1)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait
            });

        var writer = new CqrsStreamWriter<TProgress, TResult>(channel.Writer, token);
        var producer = ProduceAsync(handler, writer, channel, token);

        try
        {
            while (true)
            {
                CqrsStreamChunk<TProgress, TResult> chunk;

                try
                {
                    if (!await channel.Reader.WaitToReadAsync(token).ConfigureAwait(false))
                    {
                        break;
                    }

                    if (!channel.Reader.TryRead(out chunk!))
                    {
                        continue;
                    }
                }
                catch (ChannelClosedException)
                {
                    break;
                }

                yield return chunk;
            }
        }
        finally
        {
            await streamCancellation.CancelAsync().ConfigureAwait(false);
            await producer.ConfigureAwait(false);
        }
    }

    private static async Task ProduceAsync<TProgress, TResult>(
        Func<ICqrsStreamWriter<TProgress, TResult>, ValueTask<Result<TResult>>> handler,
        CqrsStreamWriter<TProgress, TResult> writer,
        Channel<CqrsStreamChunk<TProgress, TResult>> channel,
        CancellationToken cancellationToken)
    {
        // Yield first so the producer never runs synchronously on the consumer's first MoveNextAsync.
        await Task.Yield();

        try
        {
            var final = await handler(writer).ConfigureAwait(false);

            var terminal = final.IsSuccess
                ? CqrsStreamChunk<TProgress, TResult>.Completed(final, sequence: writer.NextSequence())
                : CqrsStreamChunk<TProgress, TResult>.Failed(final, sequence: writer.NextSequence());

            await channel.Writer.WriteAsync(terminal, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Consumer went away (or cancelled): nothing left to report, just close the channel below.
        }
        catch (Exception exception)
        {
            try
            {
                await channel.Writer.WriteAsync(
                        CqrsStreamChunk<TProgress, TResult>.FromException(exception, sequence: writer.NextSequence()),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Consumer already gone; the failure has nowhere to go.
            }
        }
        finally
        {
            channel.Writer.TryComplete();
        }
    }

    private sealed class CqrsStreamWriter<TProgress, TResult>(
        ChannelWriter<CqrsStreamChunk<TProgress, TResult>> channelWriter,
        CancellationToken cancellationToken)
        : ICqrsStreamWriter<TProgress, TResult>
    {
        private long _sequence;

        public CancellationToken CancellationToken { get; } = cancellationToken;

        public ValueTask StartedAsync(Result<TProgress>? progress = null, string? message = null)
        {
            return WriteAsync(CqrsStreamChunk<TProgress, TResult>.Started(progress, message));
        }

        public ValueTask StartedAsync(TProgress progress, string? message = null)
        {
            return StartedAsync(Result<TProgress>.Succeed(progress), message);
        }

        public ValueTask ProgressAsync(Result<TProgress> progress, string? message = null)
        {
            return WriteAsync(CqrsStreamChunk<TProgress, TResult>.Progress(progress, message));
        }

        public ValueTask ProgressAsync(TProgress progress, string? message = null)
        {
            return ProgressAsync(Result<TProgress>.Succeed(progress), message);
        }

        public ValueTask WriteAsync(CqrsStreamChunk<TProgress, TResult> chunk)
        {
            ArgumentNullException.ThrowIfNull(chunk);

            var sequenced = chunk.Sequence.HasValue ? chunk : chunk with { Sequence = NextSequence() };
            return channelWriter.WriteAsync(sequenced, CancellationToken);
        }

        public long NextSequence()
        {
            return Interlocked.Increment(ref _sequence);
        }
    }
}
