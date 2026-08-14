using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace ManagedCode.Communication.CQRS;

/// <summary>
///     Shared stream hygiene applied identically on the producing (server) and consuming (client) side:
///     drop null chunks, assign monotonic sequence numbers, turn enumeration faults into a terminal
///     <see cref="CqrsStreamChunkKind.Failed" /> chunk, and guarantee the stream ends on a terminal chunk.
/// </summary>
internal static class CqrsStreamNormalizer
{
    public static async IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> NormalizeAsync<TProgress, TResult>(
        IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> source,
        bool assignSequenceNumbers,
        bool ensureTerminalChunk,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var enumerator = source.GetAsyncEnumerator(cancellationToken);
        var sequence = 0L;
        var sawTerminal = false;
        CqrsStreamChunk<TProgress, TResult>? faultChunk = null;

        try
        {
            while (true)
            {
                bool moved;

                try
                {
                    moved = await enumerator.MoveNextAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (CqrsStreamPassthroughException passthrough)
                {
                    ExceptionDispatchInfo.Capture(passthrough.Inner).Throw();
                    throw; // Unreachable; keeps the compiler happy about `moved`.
                }
                catch (Exception exception)
                {
                    faultChunk = CqrsStreamChunk<TProgress, TResult>.FromException(
                        exception,
                        "The command stream faulted before reaching a terminal chunk.",
                        sequence: sequence + 1);
                    break;
                }

                if (!moved)
                {
                    break;
                }

                var current = enumerator.Current;
                if (current is null)
                {
                    continue;
                }

                sequence = NextSequence(sequence, current.Sequence);
                sawTerminal = current.IsTerminal;

                yield return assignSequenceNumbers && !current.Sequence.HasValue
                    ? current with { Sequence = sequence }
                    : current;
            }
        }
        finally
        {
            await enumerator.DisposeAsync().ConfigureAwait(false);
        }

        if (faultChunk is not null)
        {
            yield return faultChunk;
            yield break;
        }

        if (ensureTerminalChunk && !sawTerminal)
        {
            yield return CqrsStreamChunk<TProgress, TResult>.Failed(
                CqrsStreamProblems.Incomplete(),
                "The command stream ended without emitting a terminal chunk.",
                sequence: sequence + 1);
        }
    }

    private static long NextSequence(long current, long? declared)
    {
        return declared.HasValue && declared.Value > current ? declared.Value : current + 1;
    }
}
