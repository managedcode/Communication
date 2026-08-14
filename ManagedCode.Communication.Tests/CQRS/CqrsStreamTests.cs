using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.CQRS;

/// <summary>
///     The push-style authoring API. Its whole reason to exist is that a handler cannot get sequencing or the
///     terminal chunk wrong, so those guarantees are what these tests pin down.
/// </summary>
public class CqrsStreamTests
{
    [Fact]
    public async Task Create_EmitsProgressThenTheReturnedResultAsCompleted()
    {
        var chunks = await CollectAsync(CqrsStream.Create<ProgressUpdate, FinalResult>(async writer =>
        {
            await writer.StartedAsync(new ProgressUpdate("started"));
            await writer.ProgressAsync(new ProgressUpdate("half"));
            return Result<FinalResult>.Succeed(new FinalResult("done"));
        }));

        chunks.Select(chunk => chunk.Kind).ShouldBe([
            CqrsStreamChunkKind.Started,
            CqrsStreamChunkKind.Progress,
            CqrsStreamChunkKind.Completed
        ]);

        chunks[^1].TryGetResult(out var result).ShouldBeTrue();
        result.Status.ShouldBe("done");
    }

    [Fact]
    public async Task Create_NumbersEveryChunkAutomatically()
    {
        var chunks = await CollectAsync(CqrsStream.Create<ProgressUpdate, FinalResult>(async writer =>
        {
            await writer.StartedAsync(new ProgressUpdate("started"));
            await writer.ProgressAsync(new ProgressUpdate("a"));
            await writer.ProgressAsync(new ProgressUpdate("b"));
            return Result<FinalResult>.Succeed(new FinalResult("done"));
        }));

        chunks.Select(chunk => chunk.Sequence).ShouldBe([1L, 2L, 3L, 4L]);
    }

    [Fact]
    public async Task Create_TurnsAFailedResultIntoATerminalFailedChunk()
    {
        var chunks = await CollectAsync(CqrsStream.Create<ProgressUpdate, FinalResult>(async writer =>
        {
            await writer.StartedAsync(new ProgressUpdate("started"));
            return Result<FinalResult>.Fail(Problem.Create("rejected", "Quota exceeded", 429));
        }));

        chunks.Count.ShouldBe(2);
        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[^1].Problem!.Title.ShouldBe("rejected");
        chunks[^1].Problem!.StatusCode.ShouldBe(429);
    }

    [Fact]
    public async Task Create_TurnsAThrownExceptionIntoATerminalFailedChunk()
    {
        var chunks = await CollectAsync(CqrsStream.Create<ProgressUpdate, FinalResult>(async writer =>
        {
            await writer.StartedAsync(new ProgressUpdate("started"));
            throw new InvalidOperationException("handler exploded");
        }));

        chunks.Count.ShouldBe(2);
        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[^1].Problem!.Title.ShouldBe(nameof(InvalidOperationException));
        chunks[^1].Problem!.Detail.ShouldBe("handler exploded");
    }

    [Fact]
    public async Task Create_AlwaysEndsOnATerminalChunkEvenWithoutAnyProgress()
    {
        var chunks = await CollectAsync(CqrsStream.Create<ProgressUpdate, FinalResult>(
            _ => ValueTask.FromResult(Result<FinalResult>.Succeed(new FinalResult("instant")))));

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
        chunks[0].Sequence.ShouldBe(1);
    }

    [Fact]
    public async Task Create_BarePayloadOverload_CompletesWithThatPayload()
    {
        var chunks = await CollectAsync(CqrsStream.Create<ProgressUpdate, FinalResult>(async writer =>
        {
            await writer.ProgressAsync(new ProgressUpdate("working"), "in flight");
            return new FinalResult("done");
        }));

        chunks.Count.ShouldBe(2);
        chunks[0].Message.ShouldBe("in flight");
        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
        chunks[^1].TryGetResult(out var result).ShouldBeTrue();
        result.Status.ShouldBe("done");
    }

    [Fact]
    public async Task Create_BarePayloadOverload_StillReportsThrownExceptions()
    {
        var chunks = await CollectAsync(CqrsStream.Create<ProgressUpdate, FinalResult>(
            ValueTask<FinalResult> (_) => throw new TimeoutException("too slow")));

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[0].Problem!.Title.ShouldBe(nameof(TimeoutException));
    }

    [Fact]
    public async Task Create_WriteAsync_PassesThroughCustomChunksAndKeepsExplicitSequences()
    {
        var chunks = await CollectAsync(CqrsStream.Create<ProgressUpdate, FinalResult>(async writer =>
        {
            await writer.WriteAsync(Chunk.Started(eventType: "custom-start", sequence: 50));
            await writer.ProgressAsync(new ProgressUpdate("after"));
            return Result<FinalResult>.Succeed(new FinalResult("done"));
        }));

        chunks[0].EventType.ShouldBe("custom-start");
        chunks[0].Sequence.ShouldBe(50);
        chunks[1].Sequence.ShouldBe(1);
    }

    [Fact]
    public async Task Create_WriteAsync_RejectsNullChunks()
    {
        var chunks = await CollectAsync(CqrsStream.Create<ProgressUpdate, FinalResult>(async writer =>
        {
            await writer.WriteAsync(null!);
            return Result<FinalResult>.Succeed(new FinalResult("unreachable"));
        }));

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[0].Problem!.Title.ShouldBe(nameof(ArgumentNullException));
    }

    [Fact]
    public async Task Create_ExposesTheConsumerCancellationTokenToTheHandler()
    {
        using var cancellation = new CancellationTokenSource();
        var observed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var stream = CqrsStream.Create<ProgressUpdate, FinalResult>(async writer =>
        {
            try
            {
                await writer.StartedAsync(new ProgressUpdate("started"));

                while (true)
                {
                    await Task.Delay(CqrsTestStreams.Tick, writer.CancellationToken);
                    await writer.ProgressAsync(new ProgressUpdate("tick"));
                }
            }
            finally
            {
                observed.TrySetResult(writer.CancellationToken.IsCancellationRequested);
            }
        }, cancellation.Token);

        await using (var enumerator = stream.GetAsyncEnumerator())
        {
            (await enumerator.MoveNextAsync()).ShouldBeTrue();
            (await enumerator.MoveNextAsync()).ShouldBeTrue();
        }

        (await observed.Task.WaitAsync(TimeSpan.FromSeconds(10))).ShouldBeTrue();
    }

    [Fact]
    public async Task Create_StopsTheHandlerWhenTheConsumerDisposesEarly()
    {
        var handlerFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var stream = CqrsStream.Create<ProgressUpdate, FinalResult>(async writer =>
        {
            try
            {
                for (var i = 0; i < 100_000; i++)
                {
                    await writer.ProgressAsync(new ProgressUpdate($"tick {i}"));
                }

                return Result<FinalResult>.Succeed(new FinalResult("done"));
            }
            finally
            {
                handlerFinished.TrySetResult();
            }
        });

        await using (var enumerator = stream.GetAsyncEnumerator())
        {
            (await enumerator.MoveNextAsync()).ShouldBeTrue();
        }

        // Disposing the enumerator must unblock and unwind the producer rather than leaking it.
        await handlerFinished.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task Create_PropagatesCancellationToTheConsumer()
    {
        using var cancellation = new CancellationTokenSource();

        var stream = CqrsStream.Create<ProgressUpdate, FinalResult>(async writer =>
        {
            while (true)
            {
                await writer.ProgressAsync(new ProgressUpdate("tick"));
            }
        }, cancellation.Token);

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in stream)
            {
                await cancellation.CancelAsync();
            }
        });
    }

    [Fact]
    public void Create_RejectsANullHandler()
    {
        Should.Throw<ArgumentNullException>(() =>
            CqrsStream.Create<ProgressUpdate, FinalResult>(
                (Func<ICqrsStreamWriter<ProgressUpdate, FinalResult>, ValueTask<Result<FinalResult>>>)null!));

        Should.Throw<ArgumentNullException>(() =>
            CqrsStream.Create<ProgressUpdate, FinalResult>(
                (Func<ICqrsStreamWriter<ProgressUpdate, FinalResult>, ValueTask<FinalResult>>)null!));
    }

    private static async Task<IReadOnlyList<Chunk>> CollectAsync(IAsyncEnumerable<Chunk> stream)
    {
        var chunks = new List<Chunk>();

        await foreach (var chunk in stream)
        {
            chunks.Add(chunk);
        }

        return chunks;
    }
}
