using System;
using System.Collections.Generic;
using System.Net;
using System.Net.ServerSentEvents;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.CQRS.AspNetCore.Extensions;
using ManagedCode.Communication.CQRS.Extensions.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.AspNetCore.Filters;

public class CqrsResultEndpointFilterIntegrationTests
{
    [Fact]
    public async Task WithCommunicationCqrsResults_SkipsNullChunkItems()
    {
        await using var app = await CreateAppAsync();

        using var response = await app.GetTestClient().GetAsync("/api/cqrs/endpoint-filter/null-chunk");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/event-stream");

        await using var stream = await response.Content.ReadAsStreamAsync();
        var parser = SseParser.Create(
            stream,
            static (_, data) => JsonSerializer.Deserialize<CqrsStreamChunk<EndpointProgressUpdate, EndpointFinalResult>>(data, JsonOptions) ??
                                  throw new JsonException("Chunk payload missing."));

        var chunks = new List<CqrsStreamChunk<EndpointProgressUpdate, EndpointFinalResult>>();
        await foreach (var item in parser.EnumerateAsync())
        {
            chunks.Add(item.Data);
        }

        chunks.Count.ShouldBe(2);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunks[1].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
    }

    [Fact]
    public async Task WithCommunicationCqrsResults_ReturnsNoContentWhenDelegateReturnsNull()
    {
        await using var app = await CreateAppAsync();

        using var response = await app.GetTestClient().GetAsync("/api/cqrs/endpoint-filter/null-result");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldNotBe("text/event-stream");
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.ShouldBe("null");
    }

    [Fact]
    public async Task WithCommunicationCqrsResults_DoesNotConvertIResultResult()
    {
        await using var app = await CreateAppAsync();

        using var response = await app.GetTestClient().GetAsync("/api/cqrs/endpoint-filter/ires-result");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/plain");
        var body = await response.Content.ReadAsStringAsync();
        body.Trim().ShouldBe("already-result");
    }

    [Fact]
    public async Task WithCommunicationCqrsResults_DoesNotConvertNonChunkAsyncEnumerable()
    {
        await using var app = await CreateAppAsync();

        using var response = await app.GetTestClient().GetAsync("/api/cqrs/endpoint-filter/not-chunk-stream");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldNotBe("text/event-stream");
    }

    [Fact]
    public async Task WithCommunicationCqrsResults_ConvertsStreamExceptionToFailedChunk()
    {
        await using var app = await CreateAppAsync();

        using var response = await app.GetTestClient().GetAsync("/api/cqrs/endpoint-filter/stream-exception");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/event-stream");

        await using var stream = await response.Content.ReadAsStreamAsync();
        var parser = SseParser.Create(
            stream,
            static (_, data) => JsonSerializer.Deserialize<CqrsStreamChunk<EndpointProgressUpdate, EndpointFinalResult>>(data, JsonOptions) ??
                                  throw new JsonException("Chunk payload missing."));

        var chunks = new List<CqrsStreamChunk<EndpointProgressUpdate, EndpointFinalResult>>();
        await foreach (var item in parser.EnumerateAsync())
        {
            chunks.Add(item.Data);
        }

        chunks.Count.ShouldBe(3);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunks[1].Kind.ShouldBe(CqrsStreamChunkKind.Progress);
        chunks[2].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[2].Final.ShouldNotBeNull();
        chunks[2].Final!.Value.IsSuccess.ShouldBeFalse();
        chunks[2].Final!.Value.Problem.ShouldNotBeNull();
        chunks[2].Final!.Value.Problem!.StatusCode.ShouldBe(500);
        chunks[2].Final!.Value.Problem!.Detail.ShouldBe("Endpoint stream failure");
    }

    [Fact]
    public async Task WithCommunicationCqrsResults_UnhandledEndpointExceptionNotHandledByStreamFilter()
    {
        await using var app = await CreateAppAsync();

        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await app.GetTestClient().GetAsync("/api/cqrs/endpoint-filter/throw-before-stream");
        });
    }

    [Fact]
    public async Task WithCommunicationCqrsResults_ClientDisposesEnumeratorWithoutTokenSignalsServerCancellation()
    {
        var cancellationDetected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var app = await CreateAppWithSignalEndpointAsync(cancellationDetected);
        using var client = app.GetTestClient();
        var chunks = new List<CqrsStreamChunk<EndpointProgressUpdate, EndpointFinalResult>>();

        await using (var enumerator = client
                   .GetForCqrsStreamAsync<EndpointProgressUpdate, EndpointFinalResult>("/api/cqrs/endpoint-filter/stream-signal-cancel")
                   .GetAsyncEnumerator())
        {
            for (var i = 0; i < 4; i++)
            {
                if (!await enumerator.MoveNextAsync())
                {
                    break;
                }

                chunks.Add(enumerator.Current);
            }
        }

        chunks.Count.ShouldBe(4);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunks.ShouldContain(chunk => chunk.Kind == CqrsStreamChunkKind.Progress);

        var cancellationObserved = await Task.WhenAny(
            cancellationDetected.Task,
            Task.Delay(TimeSpan.FromSeconds(3)));

        cancellationObserved.ShouldBe(cancellationDetected.Task);
        (await cancellationDetected.Task).ShouldBeTrue();
    }

    private static async Task<WebApplication> CreateAppAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        var app = builder.Build();

        app.MapGet("/api/cqrs/endpoint-filter/null-chunk", static () => RunChunkStreamWithNullItem())
            .WithCommunicationCqrsResults();

        app.MapGet("/api/cqrs/endpoint-filter/null-result", static () => RunNullChunkStream())
            .WithCommunicationCqrsResults();

        app.MapGet("/api/cqrs/endpoint-filter/ires-result", static () => TypedResults.Text("already-result"))
            .WithCommunicationCqrsResults();

        app.MapGet("/api/cqrs/endpoint-filter/not-chunk-stream", static () => RunNonChunkStream())
            .WithCommunicationCqrsResults();

        app.MapGet("/api/cqrs/endpoint-filter/throw-before-stream", static () =>
                ThrowBeforeReturningStream())
            .WithCommunicationCqrsResults();

        app.MapGet("/api/cqrs/endpoint-filter/stream-exception", static () => RunStreamThatThrows())
            .WithCommunicationCqrsResults();

        await app.StartAsync();
        return app;
    }

    private static async Task<WebApplication> CreateAppWithSignalEndpointAsync(TaskCompletionSource<bool> cancellationDetected)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        var app = builder.Build();
        app.MapGet(
                "/api/cqrs/endpoint-filter/stream-signal-cancel",
                (CancellationToken cancellationToken) =>
                    RunLongRunningStreamWithServerCancellationSignal(cancellationDetected, cancellationToken))
            .WithCommunicationCqrsResults();

        await app.StartAsync();
        return app;
    }

    private static async IAsyncEnumerable<CqrsStreamChunk<EndpointProgressUpdate, EndpointFinalResult>> RunChunkStreamWithNullItem()
    {
        yield return null!;
        yield return CqrsStreamChunk<EndpointProgressUpdate, EndpointFinalResult>.Started(
            Result<EndpointProgressUpdate>.Succeed(new EndpointProgressUpdate("started")),
            sequence: 1);
        await Task.Delay(1);
        yield return CqrsStreamChunk<EndpointProgressUpdate, EndpointFinalResult>.Completed(
            Result<EndpointFinalResult>.Succeed(new EndpointFinalResult("done")),
            sequence: 2);
    }

    private static async IAsyncEnumerable<CqrsStreamChunk<EndpointProgressUpdate, EndpointFinalResult>> RunStreamThatThrows()
    {
        yield return CqrsStreamChunk<EndpointProgressUpdate, EndpointFinalResult>.Started(
            Result<EndpointProgressUpdate>.Succeed(new EndpointProgressUpdate("started")),
            sequence: 1);

        await Task.Delay(1);
        yield return CqrsStreamChunk<EndpointProgressUpdate, EndpointFinalResult>.Progress(
            Result<EndpointProgressUpdate>.Succeed(new EndpointProgressUpdate("processing")),
            sequence: 2);

        throw new InvalidOperationException("Endpoint stream failure");
    }

    private static IAsyncEnumerable<CqrsStreamChunk<EndpointProgressUpdate, EndpointFinalResult>>? RunNullChunkStream()
    {
        return null;
    }

    private static int ThrowBeforeReturningStream()
    {
        throw new InvalidOperationException("Endpoint crashed before stream");
    }

    private static async IAsyncEnumerable<CqrsStreamChunk<EndpointProgressUpdate, EndpointFinalResult>> RunLongRunningStreamWithServerCancellationSignal(
        TaskCompletionSource<bool> cancellationDetected,
        [EnumeratorCancellation] CancellationToken cancellationToken = default,
        TimeSpan? delay = null)
    {
        try
        {
            yield return CqrsStreamChunk<EndpointProgressUpdate, EndpointFinalResult>.Started(
                Result<EndpointProgressUpdate>.Succeed(new EndpointProgressUpdate("started")),
                message: "command started",
                sequence: 0);

            var effectiveDelay = delay ?? TimeSpan.FromSeconds(1);

            for (var i = 1; i <= 10; i++)
            {
                await Task.Delay(effectiveDelay, cancellationToken).ConfigureAwait(false);
                yield return CqrsStreamChunk<EndpointProgressUpdate, EndpointFinalResult>.Progress(
                    Result<EndpointProgressUpdate>.Succeed(new EndpointProgressUpdate($"tick {i}")),
                    message: "in progress",
                    sequence: i);
            }
        }
        finally
        {
            cancellationDetected.TrySetResult(cancellationToken.IsCancellationRequested);
        }
    }

    private static async IAsyncEnumerable<int> RunNonChunkStream()
    {
        yield return 1;
        await Task.Delay(1);
        yield return 2;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}

public sealed record EndpointProgressUpdate(string State);
public sealed record EndpointFinalResult(string Status);
