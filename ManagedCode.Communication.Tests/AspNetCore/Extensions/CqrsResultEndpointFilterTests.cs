using System;
using System.Collections.Generic;
using System.Net;
using System.Net.ServerSentEvents;
using System.Text.Json;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.CQRS.AspNetCore.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.AspNetCore.Extensions;

public class CqrsResultEndpointFilterTests
{
    [Fact]
    public async Task WithCommunicationCqrsResults_StreamsIAsyncEnumerableChunkToEventStream()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/cqrs", static () => RunCqrsStream())
                .WithCommunicationCqrsResults();
        });

        using var response = await app.GetTestClient().GetAsync("/cqrs");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/event-stream");

        await using var stream = await response.Content.ReadAsStreamAsync();
        var parser = SseParser.Create(
            stream,
            static (_, data) => JsonSerializer.Deserialize<CqrsStreamChunk<ProgressUpdate, FinalResult>>(data, JsonOptions) ??
                                  throw new JsonException("Chunk payload missing."));

        var chunks = new List<CqrsStreamChunk<ProgressUpdate, FinalResult>>();
        await foreach (var item in parser.EnumerateAsync())
        {
            chunks.Add(item.Data);
        }

        chunks.Count.ShouldBe(3);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunks[0].ProgressResult.ShouldNotBeNull();
        chunks[1].Kind.ShouldBe(CqrsStreamChunkKind.Progress);
        chunks[1].ProgressResult!.Value.IsSuccess.ShouldBeTrue();
        chunks[2].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
        chunks[2].Final.ShouldNotBeNull();
        chunks[2].Final!.Value.Value!.Status.ShouldBe("done");
    }

    [Fact]
    public async Task WithCommunicationCqrsResults_ApplyOnRouteGroup()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            var group = app.MapGroup("/api").WithCommunicationCqrsResults();
            group.MapGet("/cqrs", static () => RunCqrsStream());
        });

        using var response = await app.GetTestClient().GetAsync("/api/cqrs");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/event-stream");

        await using var stream = await response.Content.ReadAsStreamAsync();
        var parser = SseParser.Create(
            stream,
            static (_, data) => JsonSerializer.Deserialize<CqrsStreamChunk<ProgressUpdate, FinalResult>>(data, JsonOptions) ??
                                  throw new JsonException("Chunk payload missing."));

        var chunks = new List<CqrsStreamChunk<ProgressUpdate, FinalResult>>();
        await foreach (var item in parser.EnumerateAsync())
        {
            chunks.Add(item.Data);
        }

        chunks.Count.ShouldBe(3);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunks[2].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
    }

    [Fact]
    public async Task WithCommunicationCqrsResults_ConvertsFailedChunkToEventStream()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/cqrs-failed", static () => RunFailedCqrsStream())
                .WithCommunicationCqrsResults();
        });

        using var response = await app.GetTestClient().GetAsync("/cqrs-failed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/event-stream");

        await using var stream = await response.Content.ReadAsStreamAsync();
        var parser = SseParser.Create(
            stream,
            static (_, data) => JsonSerializer.Deserialize<CqrsStreamChunk<ProgressUpdate, FinalResult>>(data, JsonOptions) ??
                                  throw new JsonException("Chunk payload missing."));

        var chunks = new List<CqrsStreamChunk<ProgressUpdate, FinalResult>>();
        await foreach (var item in parser.EnumerateAsync())
        {
            chunks.Add(item.Data);
        }

        chunks.Count.ShouldBe(2);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunks[1].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[1].Final.ShouldNotBeNull();
        chunks[1].Final!.Value.IsSuccess.ShouldBeFalse();
        chunks[1].Final!.Value.Problem.ShouldNotBeNull();
        chunks[1].Final!.Value.Problem!.StatusCode.ShouldBe(500);
    }

    [Fact]
    public async Task WithCommunicationCqrsResults_ConvertsStreamExceptionToFailedChunk()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/cqrs-stream-exception", static () => RunCqrsStreamThatThrows())
                .WithCommunicationCqrsResults();
        });

        using var response = await app.GetTestClient().GetAsync("/cqrs-stream-exception");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/event-stream");

        await using var stream = await response.Content.ReadAsStreamAsync();
        var parser = SseParser.Create(
            stream,
            static (_, data) => JsonSerializer.Deserialize<CqrsStreamChunk<ProgressUpdate, FinalResult>>(data, JsonOptions) ??
                                  throw new JsonException("Chunk payload missing."));

        var chunks = new List<CqrsStreamChunk<ProgressUpdate, FinalResult>>();
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
        chunks[2].Final!.Value.Problem!.Detail.ShouldBe("Command failed unexpectedly");
    }

    [Fact]
    public async Task WithCommunicationCqrsResults_StreamLongRunningOperationEmitsProgressPerSecondAndCompletes()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/cqrs-long", () => RunCqrsLongRunningStream())
                .WithCommunicationCqrsResults();
        });

        using var response = await app.GetTestClient().GetAsync("/cqrs-long");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/event-stream");

        await using var stream = await response.Content.ReadAsStreamAsync();
        var parser = SseParser.Create(
            stream,
            static (_, data) => JsonSerializer.Deserialize<CqrsStreamChunk<ProgressUpdate, FinalResult>>(data, JsonOptions) ??
                                  throw new JsonException("Chunk payload missing."));

        var chunks = new List<CqrsStreamChunk<ProgressUpdate, FinalResult>>();
        await foreach (var item in parser.EnumerateAsync())
        {
            chunks.Add(item.Data);
        }

        chunks.Count.ShouldBe(12);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
        chunks[^1].Final!.Value.Value!.Status.ShouldBe("done");

        var progressCount = 0;
        for (var i = 1; i < chunks.Count; i++)
        {
            if (chunks[i].Kind == CqrsStreamChunkKind.Progress)
            {
                progressCount++;
            }
        }

        progressCount.ShouldBe(10);
    }

    [Fact]
    public async Task WithCommunicationCqrsResults_CancelClientTokenStopsLongRunningStream()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/cqrs-long-cancel", static (CancellationToken cancellationToken) =>
                    RunCqrsLongRunningStream(cancellationToken))
                .WithCommunicationCqrsResults();
        });

        using var response = await app.GetTestClient().GetAsync("/cqrs-long-cancel");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/event-stream");

        await using var stream = await response.Content.ReadAsStreamAsync();
        var parser = SseParser.Create(
            stream,
            static (_, data) => JsonSerializer.Deserialize<CqrsStreamChunk<ProgressUpdate, FinalResult>>(data, JsonOptions) ??
                                  throw new JsonException("Chunk payload missing."));

        using var cancellation = new CancellationTokenSource();
        var chunks = new List<CqrsStreamChunk<ProgressUpdate, FinalResult>>();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var item in parser.EnumerateAsync(cancellation.Token))
            {
                chunks.Add(item.Data);

                if (chunks.Count == 4)
                {
                    cancellation.Cancel();
                }
            }
        });

        chunks.Count.ShouldBeLessThan(12);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunks.ShouldContain(chunk => chunk.Kind == CqrsStreamChunkKind.Progress);
        chunks[^1].Kind.ShouldNotBe(CqrsStreamChunkKind.Completed);
    }

    private static async IAsyncEnumerable<CqrsStreamChunk<ProgressUpdate, FinalResult>> RunCqrsStream()
    {
        yield return CqrsStreamChunk<ProgressUpdate, FinalResult>.Started(
            Result<ProgressUpdate>.Succeed(new ProgressUpdate("started")),
            sequence: 1);
        yield return CqrsStreamChunk<ProgressUpdate, FinalResult>.Progress(
            Result<ProgressUpdate>.Succeed(new ProgressUpdate("processing")),
            sequence: 2);
        await Task.Delay(1);
        yield return CqrsStreamChunk<ProgressUpdate, FinalResult>.Completed(
            Result<FinalResult>.Succeed(new FinalResult("done")),
            sequence: 3);
    }

    private static async IAsyncEnumerable<CqrsStreamChunk<ProgressUpdate, FinalResult>> RunFailedCqrsStream()
    {
        yield return CqrsStreamChunk<ProgressUpdate, FinalResult>.Started(
            Result<ProgressUpdate>.Succeed(new ProgressUpdate("started")),
            sequence: 1);

        await Task.Delay(1);
        yield return CqrsStreamChunk<ProgressUpdate, FinalResult>.Failed(
            Problem.Create("failed", "Boom", 500),
            message: "operation failed",
            sequence: 2);
    }

    private static async IAsyncEnumerable<CqrsStreamChunk<ProgressUpdate, FinalResult>> RunCqrsStreamThatThrows()
    {
        yield return CqrsStreamChunk<ProgressUpdate, FinalResult>.Started(
            Result<ProgressUpdate>.Succeed(new ProgressUpdate("started")),
            sequence: 1);

        await Task.Delay(1);
        yield return CqrsStreamChunk<ProgressUpdate, FinalResult>.Progress(
            Result<ProgressUpdate>.Succeed(new ProgressUpdate("processing")),
            sequence: 2);

        throw new InvalidOperationException("Command failed unexpectedly");
    }

    private static async IAsyncEnumerable<CqrsStreamChunk<ProgressUpdate, FinalResult>> RunCqrsLongRunningStream(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return CqrsStreamChunk<ProgressUpdate, FinalResult>.Started(
            Result<ProgressUpdate>.Succeed(new ProgressUpdate("started")),
            message: "command started",
            sequence: 0);

        for (var i = 1; i <= 10; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
            yield return CqrsStreamChunk<ProgressUpdate, FinalResult>.Progress(
                Result<ProgressUpdate>.Succeed(new ProgressUpdate($"tick {i}")),
                message: "in progress",
                sequence: i);
        }

        yield return CqrsStreamChunk<ProgressUpdate, FinalResult>.Completed(
            Result<FinalResult>.Succeed(new FinalResult("done")),
            message: "command completed",
            sequence: 11);
    }

    private static async Task<WebApplication> CreateAppAsync(Action<WebApplication> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        configure(app);
        await app.StartAsync();
        return app;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}

public record ProgressUpdate(string State);

public record FinalResult(string Status);
