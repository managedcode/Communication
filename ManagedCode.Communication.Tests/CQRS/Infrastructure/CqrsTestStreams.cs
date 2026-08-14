using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;

namespace ManagedCode.Communication.Tests.CQRS;

/// <summary>
///     The canonical command streams every CQRS test builds on. Kept in one place so a behaviour change is asserted
///     against one definition rather than a dozen near-identical copies.
/// </summary>
public static class CqrsTestStreams
{
    /// <summary>Delay used by "long running" streams. Deliberately tiny: no test asserts on wall-clock timing.</summary>
    public static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(5);

    /// <summary>Number of progress chunks emitted by <see cref="LongRunningAsync" /> by default.</summary>
    public const int DefaultTickCount = 10;

    public static Chunk Started(string state = "started", long? sequence = null)
    {
        return Chunk.Started(Result<ProgressUpdate>.Succeed(new ProgressUpdate(state)), sequence: sequence);
    }

    public static Chunk Progress(string state, long? sequence = null)
    {
        return Chunk.Progress(Result<ProgressUpdate>.Succeed(new ProgressUpdate(state)), sequence: sequence);
    }

    public static Chunk Completed(string status = "done", long? sequence = null)
    {
        return Chunk.Completed(Result<FinalResult>.Succeed(new FinalResult(status)), sequence: sequence);
    }

    /// <summary>Started → Progress → Completed, with explicit sequence numbers.</summary>
    public static async IAsyncEnumerable<Chunk> CompletedAsync()
    {
        yield return Started(sequence: 1);
        await Task.Yield();
        yield return Progress("processing", sequence: 2);
        await Task.Yield();
        yield return Completed(sequence: 3);
    }

    /// <summary>Same shape as <see cref="CompletedAsync" /> but echoes the command payload.</summary>
    public static async IAsyncEnumerable<Chunk> CompletedAsync(SubmitCommand command)
    {
        yield return Started(sequence: 1);
        await Task.Yield();
        yield return Progress($"processing {command.Payload}", sequence: 2);
        await Task.Yield();
        yield return Completed($"done {command.Payload}", sequence: 3);
    }

    /// <summary>Started → Progress → Completed with no sequence numbers, so the transport must assign them.</summary>
    public static async IAsyncEnumerable<Chunk> CompletedWithoutSequencesAsync()
    {
        yield return Started();
        await Task.Yield();
        yield return Progress("processing");
        await Task.Yield();
        yield return Completed();
    }

    /// <summary>A handler that reports a business failure — a handled error, not an exception.</summary>
    public static async IAsyncEnumerable<Chunk> FailedByHandlerAsync()
    {
        yield return Started(sequence: 1);
        await Task.Yield();
        yield return Chunk.Failed(
            Problem.Create("payment_declined", "The payment provider declined the charge.", 402),
            "operation failed",
            sequence: 2);
    }

    /// <summary>An unhandled exception thrown after some progress was already streamed.</summary>
    public static async IAsyncEnumerable<Chunk> ThrowsAfterProgressAsync()
    {
        yield return Started(sequence: 1);
        await Task.Yield();
        yield return Progress("processing", sequence: 2);
        await Task.Yield();

        throw new InvalidOperationException("Command failed unexpectedly");
    }

    /// <summary>An unhandled exception thrown before any chunk is produced.</summary>
    public static async IAsyncEnumerable<Chunk> ThrowsImmediatelyAsync()
    {
        await Task.Yield();

        throw new InvalidOperationException("Immediate stream failure");
#pragma warning disable CS0162 // Required to keep this an iterator.
        yield break;
#pragma warning restore CS0162
    }

    /// <summary>Emits a null chunk between valid ones; the transport must drop it.</summary>
    public static async IAsyncEnumerable<Chunk> WithNullChunkAsync()
    {
        yield return Started(sequence: 1);
        await Task.Yield();
        yield return null!;
        await Task.Yield();
        yield return Completed(sequence: 2);
    }

    /// <summary>A handler that forgets to emit a terminal chunk.</summary>
    public static async IAsyncEnumerable<Chunk> WithoutTerminalChunkAsync()
    {
        yield return Started(sequence: 1);
        await Task.Yield();
        yield return Progress("processing", sequence: 2);
    }

    /// <summary>A stream that produces nothing at all.</summary>
    public static async IAsyncEnumerable<Chunk> EmptyAsync()
    {
        await Task.Yield();
        yield break;
    }

    /// <summary>Started → <paramref name="tickCount" /> progress chunks → Completed.</summary>
    public static async IAsyncEnumerable<Chunk> LongRunningAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default,
        int tickCount = DefaultTickCount,
        TimeSpan? delay = null)
    {
        yield return Started(sequence: 0);

        var effectiveDelay = delay ?? Tick;

        for (var i = 1; i <= tickCount; i++)
        {
            await Task.Delay(effectiveDelay, cancellationToken).ConfigureAwait(false);
            yield return Progress($"tick {i}", i);
        }

        yield return Completed(sequence: tickCount + 1);
    }

    /// <summary>
    ///     A long-running stream that reports, through <paramref name="cancellationObserved" />, whether the server saw
    ///     cancellation once enumeration stopped.
    /// </summary>
    public static async IAsyncEnumerable<Chunk> ReportingCancellationAsync(
        TaskCompletionSource<bool> cancellationObserved,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        try
        {
            yield return Started(sequence: 0);

            for (var i = 1; i <= 1000; i++)
            {
                await Task.Delay(Tick, cancellationToken).ConfigureAwait(false);
                yield return Progress($"tick {i}", i);
            }

            yield return Completed(sequence: 1001);
        }
        finally
        {
            cancellationObserved.TrySetResult(cancellationToken.IsCancellationRequested);
        }
    }

    /// <summary>A stream of something that is not a CQRS chunk; the transport must leave it alone.</summary>
    public static async IAsyncEnumerable<int> NonChunkAsync()
    {
        yield return 1;
        await Task.Yield();
        yield return 2;
    }
}
