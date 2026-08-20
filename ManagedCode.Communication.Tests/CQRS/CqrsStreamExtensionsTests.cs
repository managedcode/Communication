using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.Extensions;
using Shouldly;

namespace ManagedCode.Communication.Tests.CQRS;

/// <summary>
///     Covers the extensions that consume a CQRS stream on the caller's behalf: draining it to a result,
///     reporting progress through a callback, and materializing the whole outcome.
/// </summary>
public class CqrsStreamExtensionsTests
{
    private static async IAsyncEnumerable<Chunk> ThreeChunkStream()
    {
        yield return CqrsTestStreams.Started(sequence: 1);
        await Task.Yield();
        yield return CqrsTestStreams.Progress("half", 2);
        yield return CqrsTestStreams.Completed("done", 3);
    }

    private static async IAsyncEnumerable<Chunk> FailingStream()
    {
        yield return CqrsTestStreams.Started(sequence: 1);
        await Task.Yield();
        yield return Chunk.Failed(Problem.Create("boom", "detail", 409), "it broke");
    }

    private static async IAsyncEnumerable<Chunk> NeverTerminatesStream()
    {
        yield return CqrsTestStreams.Started(sequence: 1);
        await Task.Yield();
        yield return CqrsTestStreams.Progress("half", 2);
    }

    [Test]
    public async Task ToResultAsyncReturnsTheTerminalPayload()
    {
        var result = await ThreeChunkStream().ToResultAsync();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Status.ShouldBe("done");
    }

    [Test]
    public async Task ToResultAsyncSurfacesATerminalFailure()
    {
        var result = await FailingStream().ToResultAsync();

        result.IsFailed.ShouldBeTrue();
        result.Problem!.StatusCode.ShouldBe(409);
        result.Problem!.Title.ShouldBe("boom");
    }

    [Test]
    public async Task AStreamThatStopsWithoutATerminalChunkIsAFailure()
    {
        // The alternative would be reporting a success that the command never actually claimed.
        var result = await NeverTerminatesStream().ToResultAsync();

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(CqrsStreamProblems.IncompleteStream);
    }

    [Test]
    public async Task AnEmptyStreamIsAlsoAnIncompleteFailure()
    {
        var result = await EmptyAsync().ToResultAsync();

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(CqrsStreamProblems.IncompleteStream);

        static async IAsyncEnumerable<Chunk> EmptyAsync()
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    [Test]
    public async Task TheProgressCallbackSeesEveryPayloadInOrder()
    {
        var seen = new List<string>();

        var result = await ThreeChunkStream().ToResultAsync(progress => seen.Add(progress.State));

        result.Value!.Status.ShouldBe("done");
        seen.ShouldBe(["started", "half"]);
    }

    [Test]
    public async Task TheAsyncProgressCallbackIsAwaitedBeforeTheNextChunkIsRead()
    {
        var order = new List<string>();

        var result = await Instrumented().ToResultAsync(async (progress, token) =>
        {
            order.Add($"callback:{progress.State}");
            await Task.Delay(1, token);
            order.Add($"done:{progress.State}");
        });

        result.IsSuccess.ShouldBeTrue();

        // Each callback finishes before the stream is pulled again — a slow handler applies back-pressure
        // rather than letting chunks queue up behind it.
        order.ShouldBe([
            "yield:started", "callback:started", "done:started",
            "yield:half", "callback:half", "done:half",
            "yield:done"
        ]);

        async IAsyncEnumerable<Chunk> Instrumented()
        {
            order.Add("yield:started");
            yield return CqrsTestStreams.Started(sequence: 1);
            await Task.Yield();
            order.Add("yield:half");
            yield return CqrsTestStreams.Progress("half", 2);
            order.Add("yield:done");
            yield return CqrsTestStreams.Completed("done", 3);
        }
    }

    [Test]
    public async Task ProgressChunksCarryingNoPayloadAreNotReported()
    {
        var seen = 0;

        var result = await WithEmptyProgress().ToResultAsync(_ => seen++);

        result.IsSuccess.ShouldBeTrue();
        seen.ShouldBe(0);

        static async IAsyncEnumerable<Chunk> WithEmptyProgress()
        {
            await Task.CompletedTask;
            yield return Chunk.Progress(Result<ProgressUpdate>.Fail(Problem.Create("nope", "d", 500)));
            yield return CqrsTestStreams.Completed("done");
        }
    }

    [Test]
    public async Task ReadingStopsAtTheTerminalChunk()
    {
        var pulled = 0;

        var result = await Counting().ToResultAsync();

        result.IsSuccess.ShouldBeTrue();
        pulled.ShouldBe(2); // the terminal chunk, and nothing past it

        async IAsyncEnumerable<Chunk> Counting()
        {
            await Task.CompletedTask;
            pulled++;
            yield return CqrsTestStreams.Started();
            pulled++;
            yield return CqrsTestStreams.Completed("done");
            pulled++;
            yield return CqrsTestStreams.Progress("should never be pulled");
        }
    }

    [Test]
    public async Task AStreamThatThrowsBecomesAFailedResultInOneCall()
    {
        // No AsCqrsStream in front: draining applies the guarantees itself, so a raw transport that faults does
        // not throw out of the caller's await.
        var result = await Exploding().ToResultAsync();

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(nameof(InvalidOperationException));
        result.Problem!.Detail.ShouldBe("transport died");

        static async IAsyncEnumerable<Chunk> Exploding()
        {
            yield return CqrsTestStreams.Started();
            await Task.Yield();
            throw new InvalidOperationException("transport died");
        }
    }

    [Test]
    public async Task ProgressReportedBeforeAFaultStillReachesTheCallback()
    {
        var seen = new List<string>();

        var result = await Exploding().ToResultAsync(progress => seen.Add(progress.State));

        seen.ShouldBe(["started"]);
        result.IsFailed.ShouldBeTrue();

        static async IAsyncEnumerable<Chunk> Exploding()
        {
            yield return CqrsTestStreams.Started();
            await Task.Yield();
            throw new InvalidOperationException("transport died");
        }
    }

    [Test]
    public async Task ChunksAreNumberedEvenWhenTheSourceDidNotNumberThem()
    {
        var outcome = await Unnumbered().ToOutcomeAsync();

        outcome.Chunks.Select(chunk => chunk.Sequence).ShouldBe([1L, 2L]);

        static async IAsyncEnumerable<Chunk> Unnumbered()
        {
            await Task.CompletedTask;
            yield return CqrsTestStreams.Started();
            yield return CqrsTestStreams.Completed("done");
        }
    }

    [Test]
    public async Task ApplyingTheGuaranteesTwiceChangesNothing()
    {
        // AsCqrsStream is no longer needed in front of a drain, but putting it there must stay harmless.
        var once = await ThreeChunkStream().ToOutcomeAsync();
        var twice = await ThreeChunkStream().AsCqrsStream().ToOutcomeAsync();

        twice.Chunks.Count.ShouldBe(once.Chunks.Count);
        twice.Chunks.Select(c => c.Sequence).ShouldBe(once.Chunks.Select(c => c.Sequence));
        twice.Value!.Status.ShouldBe(once.Value!.Status);
    }

    [Test]
    public async Task ToChunkListDoesNotSwallowAFault()
    {
        // The one method that hands back exactly what arrived, documented as such.
        await Should.ThrowAsync<InvalidOperationException>(() => Exploding().ToChunkListAsync());

        static async IAsyncEnumerable<Chunk> Exploding()
        {
            yield return CqrsTestStreams.Started();
            await Task.Yield();
            throw new InvalidOperationException("transport died");
        }
    }

    [Test]
    public async Task ToOutcomeAsyncKeepsTheResultTheProgressAndTheChunks()
    {
        var outcome = await ThreeChunkStream().ToOutcomeAsync();

        outcome.IsSuccess.ShouldBeTrue();
        outcome.Value!.Status.ShouldBe("done");
        outcome.Problem.ShouldBeNull();

        outcome.Chunks.Count.ShouldBe(3);
        outcome.Chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        outcome.Chunks[1].Kind.ShouldBe(CqrsStreamChunkKind.Progress);
        outcome.Chunks[2].Kind.ShouldBe(CqrsStreamChunkKind.Completed);

        outcome.Progress.Select(p => p.State).ShouldBe(["started", "half"]);
    }

    [Test]
    public async Task AnOutcomeConvertsToItsResult()
    {
        var outcome = await ThreeChunkStream().ToOutcomeAsync();

        Result<FinalResult> implicitly = outcome;
        implicitly.Value!.Status.ShouldBe("done");
        outcome.ToResult().Value!.Status.ShouldBe("done");
    }

    [Test]
    public async Task ToChunkListAsyncKeepsEverythingIncludingPastTheTerminalChunk()
    {
        var chunks = await ThreeChunkStream().ToChunkListAsync();

        chunks.Count.ShouldBe(3);
        chunks[^1].IsTerminal.ShouldBeTrue();
    }

    [Test]
    public void AlreadyCollectedChunksMaterializeToAResult()
    {
        var chunks = new List<Chunk>
        {
            CqrsTestStreams.Started(),
            CqrsTestStreams.Progress("half"),
            CqrsTestStreams.Completed("done")
        };

        chunks.ToStreamResult().Value!.Status.ShouldBe("done");
        new List<Chunk> { CqrsTestStreams.Started() }.ToStreamResult().Problem!.Title
            .ShouldBe(CqrsStreamProblems.IncompleteStream);
    }

    [Test]
    public async Task ATerminalChunkWithNoPayloadAtAllStillYieldsAFailure()
    {
        var result = await Malformed().ToResultAsync();

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(CqrsStreamProblems.IncompleteStream);

        static async IAsyncEnumerable<Chunk> Malformed()
        {
            await Task.CompletedTask;
            yield return new Chunk { Kind = CqrsStreamChunkKind.Completed };
        }
    }

    [Test]
    public async Task CancellationPropagates()
    {
        using var cts = new CancellationTokenSource();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await Cancelling(cts).ToResultAsync(cts.Token));

        static async IAsyncEnumerable<Chunk> Cancelling(CancellationTokenSource cts)
        {
            yield return CqrsTestStreams.Started();
            await cts.CancelAsync();
            cts.Token.ThrowIfCancellationRequested();
            yield return CqrsTestStreams.Completed();
        }
    }

    [Test]
    public async Task NullArgumentsAreRejected()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IAsyncEnumerable<Chunk>)null!).ToResultAsync());
        Should.Throw<ArgumentNullException>(() =>
            ThreeChunkStream().ToResultAsync((Action<ProgressUpdate>)null!));
        Should.Throw<ArgumentNullException>(() =>
            ThreeChunkStream().ToResultAsync((Func<ProgressUpdate, CancellationToken, Task>)null!));
        Should.Throw<ArgumentNullException>(() =>
            ((IEnumerable<Chunk>)null!).ToStreamResult());

        await Should.ThrowAsync<ArgumentNullException>(() =>
            ((IAsyncEnumerable<Chunk>)null!).ToOutcomeAsync());
        await Should.ThrowAsync<ArgumentNullException>(() =>
            ((IAsyncEnumerable<Chunk>)null!).ToChunkListAsync());
    }

    [Test]
    public async Task TheResultFeedsStraightIntoTheAsyncRailway()
    {
        // The point of returning Task<Result<T>>: a stream is just the start of a chain.
        var status = await ThreeChunkStream()
            .ToResultAsync(_ => { })
            .EnsureAsync(report => report.Status.Length > 0, Problem.Validation(("status", "is empty")))
            .Map(report => report.Status.ToUpperInvariant())
            .CompensateAsync(_ => Result<string>.Succeed("fallback"));

        status.Value.ShouldBe("DONE");

        var recovered = await FailingStream()
            .ToResultAsync()
            .Map(report => report.Status)
            .CompensateAsync(problem => Result<string>.Succeed($"recovered from {problem.StatusCode}"));

        recovered.Value.ShouldBe("recovered from 409");
    }
}
