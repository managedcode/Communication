using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;
using Shouldly;

namespace ManagedCode.Communication.Tests.CQRS;

/// <summary>
///     The stream hygiene shared by the server and client transports. Tested directly because both sides depend on
///     the exact same guarantees: no nulls, monotonic sequences, faults become terminal chunks, streams always end
///     on a terminal chunk.
/// </summary>
public class CqrsStreamNormalizerTests
{
    [Test]
    public async Task AssignsSequenceNumbersWhenHandlerOmitsThem()
    {
        var chunks = await NormalizeAsync(CqrsTestStreams.CompletedWithoutSequencesAsync());

        chunks.Select(chunk => chunk.Sequence).ShouldBe([1L, 2L, 3L]);
    }

    [Test]
    public async Task PreservesSequenceNumbersTheHandlerSupplied()
    {
        var chunks = await NormalizeAsync(CqrsTestStreams.CompletedAsync());

        chunks.Select(chunk => chunk.Sequence).ShouldBe([1L, 2L, 3L]);
    }

    [Test]
    public async Task ContinuesNumberingAfterAnExplicitSequence()
    {
        var chunks = await NormalizeAsync(Stream(
            CqrsTestStreams.Started(sequence: 100),
            CqrsTestStreams.Progress("a"),
            CqrsTestStreams.Progress("b"),
            CqrsTestStreams.Completed()));

        chunks.Select(chunk => chunk.Sequence).ShouldBe([100L, 101L, 102L, 103L]);
    }

    [Test]
    public async Task LeavesSequencesAloneWhenAssignmentIsDisabled()
    {
        var chunks = await NormalizeAsync(CqrsTestStreams.CompletedWithoutSequencesAsync(), assignSequenceNumbers: false);

        chunks.ShouldAllBe(chunk => chunk.Sequence == null);
    }

    [Test]
    public async Task DropsNullChunks()
    {
        var chunks = await NormalizeAsync(CqrsTestStreams.WithNullChunkAsync());

        chunks.Count.ShouldBe(2);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunks[1].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
    }

    [Test]
    public async Task TurnsAnEnumerationFaultIntoATerminalFailedChunk()
    {
        var chunks = await NormalizeAsync(CqrsTestStreams.ThrowsAfterProgressAsync());

        chunks.Count.ShouldBe(3);
        chunks[2].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[2].Problem!.Title.ShouldBe(nameof(InvalidOperationException));
        chunks[2].Problem!.Detail.ShouldBe("Command failed unexpectedly");
        chunks[2].Sequence.ShouldBe(3);
    }

    [Test]
    public async Task TurnsAnImmediateFaultIntoTheOnlyChunk()
    {
        var chunks = await NormalizeAsync(CqrsTestStreams.ThrowsImmediatelyAsync());

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[0].Problem!.Detail.ShouldBe("Immediate stream failure");
        chunks[0].Sequence.ShouldBe(1);
    }

    [Test]
    public async Task AppendsATerminalChunkWhenTheHandlerForgetsOne()
    {
        var chunks = await NormalizeAsync(CqrsTestStreams.WithoutTerminalChunkAsync());

        chunks.Count.ShouldBe(3);
        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[^1].Problem!.Title.ShouldBe(CqrsStreamProblems.IncompleteStream);
        chunks[^1].Sequence.ShouldBe(3);
    }

    [Test]
    public async Task AppendsATerminalChunkForAnEmptyStream()
    {
        var chunks = await NormalizeAsync(CqrsTestStreams.EmptyAsync());

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[0].Problem!.Title.ShouldBe(CqrsStreamProblems.IncompleteStream);
    }

    [Test]
    public async Task TreatsATerminalChunkFollowedByMoreChunksAsIncomplete()
    {
        var chunks = await NormalizeAsync(Stream(
            CqrsTestStreams.Started(),
            CqrsTestStreams.Completed(),
            CqrsTestStreams.Progress("late")));

        chunks.Count.ShouldBe(4);
        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[^1].Problem!.Title.ShouldBe(CqrsStreamProblems.IncompleteStream);
    }

    [Test]
    public async Task LeavesAnIncompleteStreamAloneWhenTheGuaranteeIsDisabled()
    {
        var chunks = await NormalizeAsync(CqrsTestStreams.WithoutTerminalChunkAsync(), ensureTerminalChunk: false);

        chunks.Count.ShouldBe(2);
        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Progress);
    }

    [Test]
    public async Task DoesNotAppendATerminalChunkWhenTheHandlerAlreadyFailed()
    {
        var chunks = await NormalizeAsync(CqrsTestStreams.FailedByHandlerAsync());

        chunks.Count.ShouldBe(2);
        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[^1].Problem!.Title.ShouldBe("payment_declined");
    }

    [Test]
    public async Task PropagatesCancellationRatherThanReportingItAsAFailure()
    {
        using var cancellation = new CancellationTokenSource();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var chunk in Normalize(CqrsTestStreams.LongRunningAsync(tickCount: 1000), cancellationToken: cancellation.Token))
            {
                if (chunk.Kind == CqrsStreamChunkKind.Progress)
                {
                    await cancellation.CancelAsync();
                }
            }
        });
    }

    [Test]
    public async Task DisposesTheSourceEnumeratorWhenTheConsumerStopsEarly()
    {
        var disposed = false;

        await using (var enumerator = Normalize(TrackingStream()).GetAsyncEnumerator())
        {
            (await enumerator.MoveNextAsync()).ShouldBeTrue();
        }

        disposed.ShouldBeTrue();

        async IAsyncEnumerable<Chunk> TrackingStream()
        {
            try
            {
                yield return CqrsTestStreams.Started();
                await Task.Yield();
                yield return CqrsTestStreams.Completed();
            }
            finally
            {
                disposed = true;
            }
        }
    }

    private static IAsyncEnumerable<Chunk> Normalize(
        IAsyncEnumerable<Chunk> source,
        bool assignSequenceNumbers = true,
        bool ensureTerminalChunk = true,
        CancellationToken cancellationToken = default)
    {
        return CqrsStreamNormalizer.NormalizeAsync(source, assignSequenceNumbers, ensureTerminalChunk, cancellationToken);
    }

    private static async Task<IReadOnlyList<Chunk>> NormalizeAsync(
        IAsyncEnumerable<Chunk> source,
        bool assignSequenceNumbers = true,
        bool ensureTerminalChunk = true)
    {
        var chunks = new List<Chunk>();

        await foreach (var chunk in Normalize(source, assignSequenceNumbers, ensureTerminalChunk))
        {
            chunks.Add(chunk);
        }

        return chunks;
    }

    private static async IAsyncEnumerable<Chunk> Stream(params Chunk[] chunks)
    {
        foreach (var chunk in chunks)
        {
            await Task.Yield();
            yield return chunk;
        }
    }
}
