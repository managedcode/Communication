using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.Tests.Common.TestApp;
using ManagedCode.Communication.Tests.Common.TestApp.Controllers;
using Microsoft.AspNetCore.SignalR.Client;
using Shouldly;

namespace ManagedCode.Communication.Tests.CQRS;

/// <summary>
///     CQRS chunks over a SignalR streaming hub method.
/// </summary>
/// <remarks>
///     SignalR is a different transport from Server-Sent Events, so none of the SSE plumbing applies here. What
///     makes the contract hold is <see cref="CqrsStream.Normalize{TProgress,TResult}" />, which a hub method
///     applies itself. These tests pin that the guarantees survive the hub's own serializer and streaming
///     protocol, not just the HTTP path.
/// </remarks>
[ClassDataSource<TestClusterApplication>(Shared = SharedType.Keyed, Key = nameof(TestClusterApplication))]
[NotInParallel(nameof(TestClusterApplication))]
public class CqrsSignalRIntegrationTests(TestClusterApplication application)
{
    private HubConnection _connection = null!;

    [Before(HookType.Test)]
    public async Task InitializeAsync()
    {
        _connection = application.CreateSignalRClient(nameof(TestHub));
        await _connection.StartAsync();
        _connection.State.ShouldBe(HubConnectionState.Connected);
    }

    [After(HookType.Test)]
    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    [Test]
    public async Task AWellFormedStreamArrivesIntact()
    {
        var chunks = await StreamAsync("StreamCommand");

        chunks.Select(chunk => chunk.Kind).ShouldBe([
            CqrsStreamChunkKind.Started,
            CqrsStreamChunkKind.Progress,
            CqrsStreamChunkKind.Completed
        ]);

        chunks[0].TryGetProgress(out var progress).ShouldBeTrue();
        progress.State.ShouldBe("started");
        chunks[^1].TryGetResult(out var report).ShouldBeTrue();
        report.Status.ShouldBe("done");
    }

    [Test]
    public async Task SequenceNumbersAreAssignedOverSignalRToo()
    {
        var chunks = await StreamAsync("StreamCommand");

        chunks.Select(chunk => chunk.Sequence).ShouldBe([1L, 2L, 3L]);
    }

    [Test]
    public async Task EventTypeSurvivesTheHubSerializer()
    {
        var chunks = await StreamAsync("StreamCommand");

        chunks[0].EventType.ShouldBe(CqrsStreamChunk<HubProgress, HubReport>.StartedEventType);
        chunks[^1].EventType.ShouldBe(CqrsStreamChunk<HubProgress, HubReport>.CompletedEventType);
    }

    [Test]
    public async Task AStreamWithoutATerminalChunkStillGetsOne()
    {
        var chunks = await StreamAsync("StreamWithoutTerminal");

        chunks.Count.ShouldBe(3);
        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[^1].TryGetProblem(out var problem).ShouldBeTrue();
        problem.Title.ShouldBe(CqrsStreamProblems.IncompleteStream);
    }

    [Test]
    public async Task AnExceptionMidStreamBecomesATerminalFailedChunk()
    {
        // Without normalization the hub would fault the stream and the client would see a HubException
        // instead of a chunk it can inspect.
        var chunks = await StreamAsync("StreamThatThrows");

        chunks.Count.ShouldBe(2);
        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[^1].TryGetProblem(out var problem).ShouldBeTrue();
        problem.Title.ShouldBe(nameof(InvalidOperationException));
        problem.Detail.ShouldBe("hub command exploded");
    }

    [Test]
    public async Task ThePushStyleWriterWorksFromAHubMethod()
    {
        var chunks = await StreamAsync("StreamViaWriter");

        chunks.Count.ShouldBe(3);
        chunks.Select(chunk => chunk.Sequence).ShouldBe([1L, 2L, 3L]);
        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
        chunks[^1].TryGetResult(out var report).ShouldBeTrue();
        report.Status.ShouldBe("done");
    }

    [Test]
    public async Task AnUnnormalizedHubStreamFaultsTheEnumerator()
    {
        // The baseline the client-side helper exists to fix: without normalization anywhere, a hub method that
        // throws gives the consumer a transport exception rather than a chunk carrying a Problem.
        await Should.ThrowAsync<Exception>(async () =>
        {
            await foreach (var _ in _connection
                               .StreamAsync<CqrsStreamChunk<HubProgress, HubReport>>("StreamRawThatThrows"))
            {
            }
        });
    }

    [Test]
    public async Task TheClientCanRestoreTheContractWhenTheServerDoesNot()
    {
        var chunks = new List<CqrsStreamChunk<HubProgress, HubReport>>();

        await foreach (var chunk in _connection
                           .StreamAsync<CqrsStreamChunk<HubProgress, HubReport>>("StreamRawThatThrows")
                           .AsCqrsStream())
        {
            chunks.Add(chunk);
        }

        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[^1].TryGetProblem(out var problem).ShouldBeTrue();
        problem.ShouldNotBeNull();
    }

    [Test]
    public async Task AClientSideStreamDrainsToAResultLikeTheHttpOne()
    {
        var seen = new List<string>();

        // One call, and no AsCqrsStream: draining applies the guarantees itself, so a SignalR stream reads
        // exactly like the HTTP one.
        var result = await _connection
            .StreamAsync<CqrsStreamChunk<HubProgress, HubReport>>("StreamCommand")
            .ToResultAsync(progress => seen.Add(progress.State));

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Status.ShouldBe("done");
        seen.ShouldBe(["started", "half"]);
    }

    [Test]
    public async Task AFaultingHubStreamBecomesAFailedResultRatherThanAThrow()
    {
        // The hub method does not normalize and throws part-way through; without this the caller would catch a
        // HubException instead of reading a Problem.
        var result = await _connection
            .StreamAsync<CqrsStreamChunk<HubProgress, HubReport>>("StreamRawThatThrows")
            .ToResultAsync();

        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
    }

    [Test]
    public async Task AClientSideStreamThatNeverTerminatesFailsRatherThanStopsQuietly()
    {
        var result = await _connection
            .StreamAsync<CqrsStreamChunk<HubProgress, HubReport>>("StreamRawWithoutTerminal")
            .ToResultAsync();

        result.IsFailed.ShouldBeTrue();
        result.Problem!.Title.ShouldBe(CqrsStreamProblems.IncompleteStream);
    }

    private async Task<IReadOnlyList<CqrsStreamChunk<HubProgress, HubReport>>> StreamAsync(string method)
    {
        var chunks = new List<CqrsStreamChunk<HubProgress, HubReport>>();

        await foreach (var chunk in _connection.StreamAsync<CqrsStreamChunk<HubProgress, HubReport>>(method))
        {
            chunks.Add(chunk);
        }

        return chunks;
    }
}
