using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;
using Microsoft.AspNetCore.TestHost;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.CQRS;

/// <summary>
///     The MVC side of the transport, driven through <see cref="CqrsTestController" />. Mirrors
///     <see cref="CqrsMinimalApiIntegrationTests" /> so both pipelines are held to the same contract.
/// </summary>
public class CqrsMvcIntegrationTests
{
    [Fact]
    public async Task CompletedStream_IsDeliveredAsEventStream()
    {
        await using var app = await CqrsTestHost.StartMvcAsync();
        using var response = await app.GetTestClient().GetAsync("/api/cqrs/mvc/completed");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/event-stream");

        var chunks = await SseTestReader.ReadChunksAsync(response);

        chunks.Select(chunk => chunk.Kind).ShouldBe([
            CqrsStreamChunkKind.Started,
            CqrsStreamChunkKind.Progress,
            CqrsStreamChunkKind.Completed
        ]);
    }

    [Fact]
    public async Task SequenceNumbersAreAssignedForActionsToo()
    {
        await using var app = await CqrsTestHost.StartMvcAsync();
        using var response = await app.GetTestClient().GetAsync("/api/cqrs/mvc/completed-without-sequences");

        var frames = await SseTestReader.ReadFramesAsync(response);

        frames.Select(frame => frame.Id).ShouldBe(["1", "2", "3"]);
    }

    [Fact]
    public async Task PostWithBody_ReachesTheAction()
    {
        await using var app = await CqrsTestHost.StartMvcAsync();

        var chunks = new List<Chunk>();
        await foreach (var chunk in app.GetTestClient()
                           .PostForCqrsStreamAsync<ProgressUpdate, FinalResult, SubmitCommand>(
                               "/api/cqrs/mvc/submit", new SubmitCommand("job-7")))
        {
            chunks.Add(chunk);
        }

        chunks[^1].TryGetResult(out var result).ShouldBeTrue();
        result.Status.ShouldBe("done job-7");
    }

    [Fact]
    public async Task HandlerReportedFailure_ArrivesAsATerminalFailedChunk()
    {
        await using var app = await CqrsTestHost.StartMvcAsync();
        using var response = await app.GetTestClient().GetAsync("/api/cqrs/mvc/handler-failed");

        var chunks = await SseTestReader.ReadChunksAsync(response);

        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[^1].Problem!.Title.ShouldBe("payment_declined");
    }

    [Fact]
    public async Task ExceptionMidStream_BecomesATerminalFailedChunk()
    {
        await using var app = await CqrsTestHost.StartMvcAsync();
        using var response = await app.GetTestClient().GetAsync("/api/cqrs/mvc/stream-throws");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var chunks = await SseTestReader.ReadChunksAsync(response);

        chunks.Count.ShouldBe(3);
        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[^1].Problem!.Detail.ShouldBe("Command failed unexpectedly");
    }

    [Fact]
    public async Task ExceptionBeforeTheFirstChunk_BecomesTheOnlyChunk()
    {
        await using var app = await CqrsTestHost.StartMvcAsync();
        using var response = await app.GetTestClient().GetAsync("/api/cqrs/mvc/stream-throws-immediately");

        var chunks = await SseTestReader.ReadChunksAsync(response);

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[0].Problem!.Detail.ShouldBe("Immediate stream failure");
    }

    [Fact]
    public async Task ExceptionBeforeTheStreamIsReturned_IsNotTheTransportsToHandle()
    {
        await using var app = await CqrsTestHost.StartMvcAsync();

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await app.GetTestClient().GetAsync("/api/cqrs/mvc/throws-before-stream"));

        exception.Message.ShouldBe("Action crashed before returning a stream");
    }

    [Fact]
    public async Task StreamWithoutATerminalChunk_GetsOneAppended()
    {
        await using var app = await CqrsTestHost.StartMvcAsync();
        using var response = await app.GetTestClient().GetAsync("/api/cqrs/mvc/no-terminal");

        var chunks = await SseTestReader.ReadChunksAsync(response);

        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[^1].Problem!.Title.ShouldBe(CqrsStreamProblems.IncompleteStream);
    }

    [Fact]
    public async Task TerminalGuaranteeFollowsTheConfiguredOptions()
    {
        await using var app = await CqrsTestHost.StartMvcAsync(options => options.EnsureTerminalChunk = false);
        using var response = await app.GetTestClient().GetAsync("/api/cqrs/mvc/no-terminal");

        var chunks = await SseTestReader.ReadChunksAsync(response);

        chunks.Count.ShouldBe(2);
        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Progress);
    }

    [Fact]
    public async Task SequenceAssignmentFollowsTheConfiguredOptions()
    {
        await using var app = await CqrsTestHost.StartMvcAsync(options => options.AssignSequenceNumbers = false);
        using var response = await app.GetTestClient().GetAsync("/api/cqrs/mvc/completed-without-sequences");

        var frames = await SseTestReader.ReadFramesAsync(response);

        frames.ShouldAllBe(frame => frame.Id == null);
    }

    [Fact]
    public async Task NullChunksAreDropped()
    {
        await using var app = await CqrsTestHost.StartMvcAsync();
        using var response = await app.GetTestClient().GetAsync("/api/cqrs/mvc/null-chunk");

        var chunks = await SseTestReader.ReadChunksAsync(response);

        chunks.Select(chunk => chunk.Kind).ShouldBe([CqrsStreamChunkKind.Started, CqrsStreamChunkKind.Completed]);
    }

    [Fact]
    public async Task EmptyStream_StillEndsOnATerminalChunk()
    {
        await using var app = await CqrsTestHost.StartMvcAsync();
        using var response = await app.GetTestClient().GetAsync("/api/cqrs/mvc/empty");

        var chunks = await SseTestReader.ReadChunksAsync(response);

        chunks.Count.ShouldBe(1);
        chunks[0].Problem!.Title.ShouldBe(CqrsStreamProblems.IncompleteStream);
    }

    [Fact]
    public async Task ANonChunkAsyncEnumerableActionIsPassedThroughUnchanged()
    {
        await using var app = await CqrsTestHost.StartMvcAsync();
        using var response = await app.GetTestClient().GetAsync("/api/cqrs/mvc/non-chunk-stream");

        response.Content.Headers.ContentType?.MediaType.ShouldNotBe("text/event-stream");
        (await response.Content.ReadFromJsonAsync<int[]>()).ShouldBe([1, 2]);
    }

    [Fact]
    public async Task APlainObjectActionIsPassedThroughUnchanged()
    {
        await using var app = await CqrsTestHost.StartMvcAsync();
        using var response = await app.GetTestClient().GetAsync("/api/cqrs/mvc/plain-object");

        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        (await response.Content.ReadFromJsonAsync<FinalResult>())!.Status.ShouldBe("not-a-stream");
    }

    [Fact]
    public async Task ClientDisconnect_CancelsTheAction()
    {
        await using var app = await CqrsTestHost.StartMvcAsync();

        var chunks = new List<Chunk>();
        await using (var enumerator = app.GetTestClient()
                         .GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("/api/cqrs/mvc/long-running")
                         .GetAsyncEnumerator())
        {
            for (var i = 0; i < 3 && await enumerator.MoveNextAsync(); i++)
            {
                chunks.Add(enumerator.Current);
            }
        }

        chunks.Count.ShouldBe(3);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
    }

    [Fact]
    public async Task CancellingTheClientTokenStopsEnumeration()
    {
        await using var app = await CqrsTestHost.StartMvcAsync();

        using var cancellation = new CancellationTokenSource();
        var chunks = new List<Chunk>();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var chunk in app.GetTestClient()
                               .GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("/api/cqrs/mvc/long-running", cancellation.Token))
            {
                chunks.Add(chunk);
                await cancellation.CancelAsync();
            }
        });

        // The endpoint streams 10 000 chunks, so stopping anywhere near the start proves cancellation took
        // effect. Pinning an exact count would only be asserting how many frames happened to be buffered.
        chunks.Count.ShouldBeGreaterThanOrEqualTo(1);
        chunks.Count.ShouldBeLessThan(50);
        chunks.ShouldAllBe(chunk => chunk.Kind != CqrsStreamChunkKind.Completed);
    }
}
