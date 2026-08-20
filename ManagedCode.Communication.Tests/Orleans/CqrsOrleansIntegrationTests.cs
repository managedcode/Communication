using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.Tests.Orleans.Fixtures;
using ManagedCode.Communication.Tests.Orleans.Grains;
using Orleans;
using Shouldly;

namespace ManagedCode.Communication.Tests.Orleans;

/// <summary>
///     CQRS chunks crossing an Orleans grain boundary.
/// </summary>
/// <remarks>
///     Orleans validates grain interfaces at silo start, so a chunk without a registered surrogate does not fail
///     at call time — it stops the silo from booting at all. These tests exist so that failure mode can never
///     come back unnoticed: the fixture itself would refuse to start.
/// </remarks>
[ClassDataSource<OrleansClusterFixture>(Shared = SharedType.PerClass)]
[NotInParallel(nameof(CqrsOrleansIntegrationTests))]
public class CqrsOrleansIntegrationTests
{
    private readonly IGrainFactory _grainFactory;

    public CqrsOrleansIntegrationTests(OrleansClusterFixture fixture)
    {
        _grainFactory = fixture.Cluster.GrainFactory;
    }

    [Test]
    public async Task ACompletedChunkRoundTripsThroughAGrain()
    {
        var grain = _grainFactory.GetGrain<ICqrsProbeGrain>(Guid.NewGuid());
        var original = CqrsStreamChunk<OrleansProgress, OrleansReport>.Completed(
            Result<OrleansReport>.Succeed(new OrleansReport("done")),
            message: "all good",
            eventId: "evt-1",
            sequence: 7);

        var echoed = await grain.EchoChunkAsync(original);

        echoed.Kind.ShouldBe(CqrsStreamChunkKind.Completed);
        echoed.Message.ShouldBe("all good");
        echoed.EventId.ShouldBe("evt-1");
        echoed.EventType.ShouldBe(CqrsStreamChunk<OrleansProgress, OrleansReport>.CompletedEventType);
        echoed.Sequence.ShouldBe(7);
        echoed.TryGetResult(out var report).ShouldBeTrue();
        report.Status.ShouldBe("done");
    }

    [Test]
    public async Task AProgressChunkRoundTripsThroughAGrain()
    {
        var grain = _grainFactory.GetGrain<ICqrsProbeGrain>(Guid.NewGuid());

        var echoed = await grain.EchoChunkAsync(
            CqrsStreamChunk<OrleansProgress, OrleansReport>.Progress(new OrleansProgress("half"), sequence: 2));

        echoed.Kind.ShouldBe(CqrsStreamChunkKind.Progress);
        echoed.TryGetProgress(out var progress).ShouldBeTrue();
        progress.State.ShouldBe("half");
    }

    [Test]
    public async Task AFailedChunkKeepsItsProblem()
    {
        var grain = _grainFactory.GetGrain<ICqrsProbeGrain>(Guid.NewGuid());

        var echoed = await grain.EchoChunkAsync(
            CqrsStreamChunk<OrleansProgress, OrleansReport>.Failed(
                Problem.Create("rejected", "quota exceeded", 429), sequence: 3));

        echoed.Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        echoed.TryGetProblem(out var problem).ShouldBeTrue();
        problem.Title.ShouldBe("rejected");
        problem.Detail.ShouldBe("quota exceeded");
        problem.StatusCode.ShouldBe(429);
    }

    [Test]
    public async Task AGrainCanStreamChunksAsAnAsyncEnumerable()
    {
        var grain = _grainFactory.GetGrain<ICqrsProbeGrain>(Guid.NewGuid());

        var chunks = new List<CqrsStreamChunk<OrleansProgress, OrleansReport>>();
        await foreach (var chunk in grain.StreamAsync())
        {
            chunks.Add(chunk);
        }

        chunks.Count.ShouldBe(3);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunks[1].Kind.ShouldBe(CqrsStreamChunkKind.Progress);
        chunks[2].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
        chunks[2].TryGetResult(out var report).ShouldBeTrue();
        report.Status.ShouldBe("done");
    }

    [Test]
    public async Task AGrainStreamDrainsStraightToItsResult()
    {
        var grain = _grainFactory.GetGrain<ICqrsProbeGrain>(Guid.NewGuid());

        var result = await grain.StreamAsync().ToResultAsync();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Status.ShouldBe("done");
    }

    [Test]
    public async Task AGrainStreamReportsProgressThroughACallback()
    {
        var grain = _grainFactory.GetGrain<ICqrsProbeGrain>(Guid.NewGuid());
        var seen = new List<string>();

        var result = await grain.StreamAsync().ToResultAsync(progress => seen.Add(progress.State));

        result.Value!.Status.ShouldBe("done");
        seen.ShouldNotBeEmpty();
    }

    [Test]
    public async Task AGrainStreamMaterializesIntoAnOutcome()
    {
        var grain = _grainFactory.GetGrain<ICqrsProbeGrain>(Guid.NewGuid());

        var outcome = await grain.StreamAsync().ToOutcomeAsync();

        outcome.Chunks.Count.ShouldBe(3);
        outcome.Chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        outcome.Chunks[1].Kind.ShouldBe(CqrsStreamChunkKind.Progress);
        outcome.Chunks[2].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
        outcome.Progress.ShouldNotBeEmpty();
        outcome.Value!.Status.ShouldBe("done");
    }
}
