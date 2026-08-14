using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.Tests.Common.TestApp;
using ManagedCode.Communication.Tests.Common.TestApp.Controllers;
using Microsoft.AspNetCore.SignalR.Client;
using Shouldly;
using Xunit;

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
[Collection(nameof(TestClusterApplication))]
public class CqrsSignalRIntegrationTests(TestClusterApplication application) : IAsyncLifetime
{
    private HubConnection _connection = null!;

    public async Task InitializeAsync()
    {
        _connection = application.CreateSignalRClient(nameof(TestHub));
        await _connection.StartAsync();
        _connection.State.ShouldBe(HubConnectionState.Connected);
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    [Fact]
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

    [Fact]
    public async Task SequenceNumbersAreAssignedOverSignalRToo()
    {
        var chunks = await StreamAsync("StreamCommand");

        chunks.Select(chunk => chunk.Sequence).ShouldBe([1L, 2L, 3L]);
    }

    [Fact]
    public async Task EventTypeSurvivesTheHubSerializer()
    {
        var chunks = await StreamAsync("StreamCommand");

        chunks[0].EventType.ShouldBe(CqrsStreamChunk<HubProgress, HubReport>.StartedEventType);
        chunks[^1].EventType.ShouldBe(CqrsStreamChunk<HubProgress, HubReport>.CompletedEventType);
    }

    [Fact]
    public async Task AStreamWithoutATerminalChunkStillGetsOne()
    {
        var chunks = await StreamAsync("StreamWithoutTerminal");

        chunks.Count.ShouldBe(3);
        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[^1].TryGetProblem(out var problem).ShouldBeTrue();
        problem.Title.ShouldBe(CqrsStreamProblems.IncompleteStream);
    }

    [Fact]
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

    [Fact]
    public async Task ThePushStyleWriterWorksFromAHubMethod()
    {
        var chunks = await StreamAsync("StreamViaWriter");

        chunks.Count.ShouldBe(3);
        chunks.Select(chunk => chunk.Sequence).ShouldBe([1L, 2L, 3L]);
        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
        chunks[^1].TryGetResult(out var report).ShouldBeTrue();
        report.Status.ShouldBe("done");
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
