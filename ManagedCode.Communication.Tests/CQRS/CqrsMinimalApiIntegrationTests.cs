using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.CQRS.AspNetCore;
using ManagedCode.Communication.CQRS.AspNetCore.Extensions;
using ManagedCode.Communication.CQRS.Extensions.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.CQRS;

/// <summary>
///     End-to-end behaviour of <c>WithCommunicationCqrsResults()</c> on Minimal API endpoints, over a real HTTP
///     pipeline: happy paths, handler-reported failures, unhandled exceptions, and everything the transport must
///     leave untouched.
/// </summary>
public class CqrsMinimalApiIntegrationTests
{
    // ---------- positive ----------

    [Fact]
    public async Task CompletedStream_IsDeliveredAsEventStream()
    {
        await using var app = await StartAsync(app => app
            .MapGet("/cqrs", () => CqrsTestStreams.CompletedAsync())
            .WithCommunicationCqrsResults());

        using var response = await app.GetTestClient().GetAsync("/cqrs");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/event-stream");

        var chunks = await SseTestReader.ReadChunksAsync(response);

        chunks.Select(chunk => chunk.Kind).ShouldBe([
            CqrsStreamChunkKind.Started,
            CqrsStreamChunkKind.Progress,
            CqrsStreamChunkKind.Completed
        ]);
        chunks[^1].TryGetResult(out var result).ShouldBeTrue();
        result.Status.ShouldBe("done");
    }

    [Fact]
    public async Task RouteGroup_AppliesToEveryEndpointInTheGroup()
    {
        await using var app = await StartAsync(app =>
        {
            var group = app.MapGroup("/api").WithCommunicationCqrsResults();
            group.MapGet("/first", () => CqrsTestStreams.CompletedAsync());
            group.MapGet("/second", () => CqrsTestStreams.CompletedAsync());
        });

        using var client = app.GetTestClient();

        foreach (var route in new[] { "/api/first", "/api/second" })
        {
            using var response = await client.GetAsync(route);
            response.Content.Headers.ContentType?.MediaType.ShouldBe("text/event-stream");
            (await SseTestReader.ReadChunksAsync(response))[^1].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
        }
    }

    [Fact]
    public async Task PushStyleHandler_StreamsProgressAndCompletes()
    {
        await using var app = await StartAsync(app => app
            .MapGet("/cqrs", (CancellationToken cancellationToken) =>
                CqrsStream.Create<ProgressUpdate, FinalResult>(async writer =>
                {
                    await writer.StartedAsync(new ProgressUpdate("started"));
                    await writer.ProgressAsync(new ProgressUpdate("half"));
                    return Result<FinalResult>.Succeed(new FinalResult("done"));
                }, cancellationToken))
            .WithCommunicationCqrsResults());

        using var response = await app.GetTestClient().GetAsync("/cqrs");

        var chunks = await SseTestReader.ReadChunksAsync(response);

        chunks.Count.ShouldBe(3);
        chunks.Select(chunk => chunk.Sequence).ShouldBe([1L, 2L, 3L]);
        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
    }

    [Fact]
    public async Task PostWithBody_ReachesTheHandler()
    {
        await using var app = await StartAsync(app => app
            .MapPost("/cqrs", (SubmitCommand command) => CqrsTestStreams.CompletedAsync(command))
            .WithCommunicationCqrsResults());

        var chunks = new List<Chunk>();
        await foreach (var chunk in app.GetTestClient()
                           .PostForCqrsStreamAsync<ProgressUpdate, FinalResult, SubmitCommand>("/cqrs", new SubmitCommand("job-99")))
        {
            chunks.Add(chunk);
        }

        chunks[^1].TryGetResult(out var result).ShouldBeTrue();
        result.Status.ShouldBe("done job-99");
    }

    // ---------- handled failures ----------

    [Fact]
    public async Task HandlerReportedFailure_ArrivesAsATerminalFailedChunkOnA200Response()
    {
        await using var app = await StartAsync(app => app
            .MapGet("/cqrs", () => CqrsTestStreams.FailedByHandlerAsync())
            .WithCommunicationCqrsResults());

        using var response = await app.GetTestClient().GetAsync("/cqrs");

        // The HTTP exchange itself succeeded; the command did not. Those are different things.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var chunks = await SseTestReader.ReadChunksAsync(response);

        chunks.Count.ShouldBe(2);
        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[^1].Problem!.Title.ShouldBe("payment_declined");
        chunks[^1].Problem!.StatusCode.ShouldBe(402);
    }

    // ---------- unhandled failures ----------

    [Fact]
    public async Task ExceptionMidStream_BecomesATerminalFailedChunk()
    {
        await using var app = await StartAsync(app => app
            .MapGet("/cqrs", () => CqrsTestStreams.ThrowsAfterProgressAsync())
            .WithCommunicationCqrsResults());

        using var response = await app.GetTestClient().GetAsync("/cqrs");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var chunks = await SseTestReader.ReadChunksAsync(response);

        chunks.Count.ShouldBe(3);
        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[^1].Problem!.Title.ShouldBe(nameof(InvalidOperationException));
        chunks[^1].Problem!.Detail.ShouldBe("Command failed unexpectedly");
        chunks[^1].Problem!.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ExceptionBeforeTheFirstChunk_BecomesTheOnlyChunk()
    {
        await using var app = await StartAsync(app => app
            .MapGet("/cqrs", () => CqrsTestStreams.ThrowsImmediatelyAsync())
            .WithCommunicationCqrsResults());

        using var response = await app.GetTestClient().GetAsync("/cqrs");

        var chunks = await SseTestReader.ReadChunksAsync(response);

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[0].Problem!.Detail.ShouldBe("Immediate stream failure");
    }

    [Fact]
    public async Task ExceptionThrownBeforeTheStreamIsReturned_IsNotTheTransportsToHandle()
    {
        // The filter converts return values. A handler that throws before returning one never reaches it, so the
        // failure must surface through the host's normal exception handling instead of being silently swallowed.
        await using var app = await StartAsync(app => app
            .MapGet("/cqrs", IAsyncEnumerable<Chunk> () => throw new InvalidOperationException("crashed before stream"))
            .WithCommunicationCqrsResults());

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await app.GetTestClient().GetAsync("/cqrs"));

        exception.Message.ShouldBe("crashed before stream");
    }

    [Fact]
    public async Task ExceptionThrownBeforeTheStream_IsHandledByAnExceptionHandlerWhenOneIsConfigured()
    {
        await using var app = await CqrsTestHost.StartMinimalApiAsync(app =>
        {
            app.UseExceptionHandler("/error");
            app.MapGet("/error", () => TypedResults.Problem(title: "boom", detail: "handled centrally", statusCode: 500));
            app.MapGet("/cqrs", IAsyncEnumerable<Chunk> () => throw new InvalidOperationException("crashed before stream"))
                .WithCommunicationCqrsResults();
        });

        // A client asking for a stream still gets a terminal failed chunk, synthesized from the error response.
        var chunks = new List<Chunk>();
        await foreach (var chunk in app.GetTestClient().GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("/cqrs"))
        {
            chunks.Add(chunk);
        }

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[0].Problem!.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
        chunks[0].Problem!.Detail.ShouldBe("handled centrally");
    }

    [Fact]
    public async Task StreamWithoutATerminalChunk_GetsOneAppended()
    {
        await using var app = await StartAsync(app => app
            .MapGet("/cqrs", () => CqrsTestStreams.WithoutTerminalChunkAsync())
            .WithCommunicationCqrsResults());

        using var response = await app.GetTestClient().GetAsync("/cqrs");

        var chunks = await SseTestReader.ReadChunksAsync(response);

        chunks.Count.ShouldBe(3);
        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[^1].Problem!.Title.ShouldBe(CqrsStreamProblems.IncompleteStream);
    }

    [Fact]
    public async Task TerminalGuaranteeCanBeDisabled()
    {
        await using var app = await StartAsync(app => app
            .MapGet("/cqrs", () => CqrsTestStreams.WithoutTerminalChunkAsync())
            .WithCommunicationCqrsResults(new CqrsStreamServerOptions { EnsureTerminalChunk = false }));

        using var response = await app.GetTestClient().GetAsync("/cqrs");

        var chunks = await SseTestReader.ReadChunksAsync(response);

        chunks.Count.ShouldBe(2);
        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Progress);
    }

    [Fact]
    public async Task NullChunksAreDropped()
    {
        await using var app = await StartAsync(app => app
            .MapGet("/cqrs", () => CqrsTestStreams.WithNullChunkAsync())
            .WithCommunicationCqrsResults());

        using var response = await app.GetTestClient().GetAsync("/cqrs");

        var chunks = await SseTestReader.ReadChunksAsync(response);

        chunks.Select(chunk => chunk.Kind).ShouldBe([CqrsStreamChunkKind.Started, CqrsStreamChunkKind.Completed]);
    }

    [Fact]
    public async Task EmptyStream_StillEndsOnATerminalChunk()
    {
        await using var app = await StartAsync(app => app
            .MapGet("/cqrs", () => CqrsTestStreams.EmptyAsync())
            .WithCommunicationCqrsResults());

        using var response = await app.GetTestClient().GetAsync("/cqrs");

        var chunks = await SseTestReader.ReadChunksAsync(response);

        chunks.Count.ShouldBe(1);
        chunks[0].Problem!.Title.ShouldBe(CqrsStreamProblems.IncompleteStream);
    }

    // ---------- things the transport must not touch ----------

    [Fact]
    public async Task AnIResultReturnedByTheHandlerIsPassedThroughUnchanged()
    {
        await using var app = await StartAsync(app => app
            .MapGet("/plain", () => TypedResults.Text("already-result"))
            .WithCommunicationCqrsResults());

        using var response = await app.GetTestClient().GetAsync("/plain");

        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/plain");
        (await response.Content.ReadAsStringAsync()).Trim().ShouldBe("already-result");
    }

    [Fact]
    public async Task ANonChunkAsyncEnumerableIsPassedThroughUnchanged()
    {
        await using var app = await StartAsync(app => app
            .MapGet("/numbers", () => CqrsTestStreams.NonChunkAsync())
            .WithCommunicationCqrsResults());

        using var response = await app.GetTestClient().GetAsync("/numbers");

        response.Content.Headers.ContentType?.MediaType.ShouldNotBe("text/event-stream");
        (await response.Content.ReadAsStringAsync()).ShouldBe("[1,2]");
    }

    [Fact]
    public async Task APlainObjectIsPassedThroughUnchanged()
    {
        await using var app = await StartAsync(app => app
            .MapGet("/plain", () => new FinalResult("not-a-stream"))
            .WithCommunicationCqrsResults());

        using var response = await app.GetTestClient().GetAsync("/plain");

        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        (await response.Content.ReadAsStringAsync()).ShouldContain("not-a-stream");
    }

    [Fact]
    public async Task ANullReturnValueIsPassedThroughUnchanged()
    {
        await using var app = await StartAsync(app => app
            .MapGet("/nothing", () => (IAsyncEnumerable<Chunk>?)null)
            .WithCommunicationCqrsResults());

        using var response = await app.GetTestClient().GetAsync("/nothing");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldNotBe("text/event-stream");
    }

    // ---------- cancellation and disconnects ----------

    [Fact]
    public async Task ClientDisconnect_CancelsTheHandler()
    {
        var cancellationObserved = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var app = await StartAsync(app => app
            .MapGet("/cqrs", (CancellationToken cancellationToken) =>
                CqrsTestStreams.ReportingCancellationAsync(cancellationObserved, cancellationToken))
            .WithCommunicationCqrsResults());

        var chunks = new List<Chunk>();

        await using (var enumerator = app.GetTestClient()
                         .GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("/cqrs")
                         .GetAsyncEnumerator())
        {
            for (var i = 0; i < 3 && await enumerator.MoveNextAsync(); i++)
            {
                chunks.Add(enumerator.Current);
            }
        }

        chunks.Count.ShouldBe(3);
        (await cancellationObserved.Task.WaitAsync(TimeSpan.FromSeconds(30))).ShouldBeTrue();
    }

    [Fact]
    public async Task CancellingTheClientTokenStopsEnumerationPromptly()
    {
        await using var app = await StartAsync(app => app
            .MapGet("/cqrs", (CancellationToken cancellationToken) =>
                CqrsTestStreams.LongRunningAsync(cancellationToken, tickCount: 10_000))
            .WithCommunicationCqrsResults());

        using var cancellation = new CancellationTokenSource();
        var chunks = new List<Chunk>();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var chunk in app.GetTestClient()
                               .GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("/cqrs", cancellation.Token))
            {
                chunks.Add(chunk);

                if (chunks.Count == 3)
                {
                    await cancellation.CancelAsync();
                }
            }
        });

        // The stream is far longer than what was consumed, so this cannot pass by simply running to completion.
        chunks.Count.ShouldBeGreaterThanOrEqualTo(3);
        chunks.ShouldAllBe(chunk => chunk.Kind != CqrsStreamChunkKind.Completed);
    }

    [Fact]
    public async Task APreCancelledTokenNeverReachesTheServer()
    {
        await using var app = await StartAsync(app => app
            .MapGet("/cqrs", () => CqrsTestStreams.CompletedAsync())
            .WithCommunicationCqrsResults());

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in app.GetTestClient()
                               .GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("/cqrs", cancellation.Token))
            {
            }
        });
    }

    [Fact]
    public async Task AStoppedConsumerCanStartTheStreamAgainFromScratch()
    {
        await using var app = await StartAsync(app => app
            .MapGet("/cqrs", (CancellationToken cancellationToken) => CqrsTestStreams.LongRunningAsync(cancellationToken))
            .WithCommunicationCqrsResults());

        using var client = app.GetTestClient();

        var firstRun = new List<Chunk>();
        await using (var enumerator = client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("/cqrs").GetAsyncEnumerator())
        {
            for (var i = 0; i < 3 && await enumerator.MoveNextAsync(); i++)
            {
                firstRun.Add(enumerator.Current);
            }
        }

        firstRun.Count.ShouldBe(3);

        var secondRun = new List<Chunk>();
        await foreach (var chunk in client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("/cqrs"))
        {
            secondRun.Add(chunk);
        }

        secondRun.Count.ShouldBe(CqrsTestStreams.DefaultTickCount + 2);
        secondRun[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        secondRun[^1].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
    }

    private static Task<WebApplication> StartAsync(Action<WebApplication> configure)
    {
        return CqrsTestHost.StartMinimalApiAsync(configure);
    }
}
