using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.CQRS.Extensions.Http;
using CommunicationCqrsExtensions = ManagedCode.Communication.CQRS.AspNetCore.Extensions;
using ManagedCode.Communication.CQRS.AspNetCore.Extensions;
using CommunicationCoreExtensions = ManagedCode.Communication.AspNetCore.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.AspNetCore.Filters;

public class CqrsResultActionFilterIntegrationTests
{
    [Fact]
    public async Task WithCommunicationCqrsFilters_StreamsIAsyncEnumerableAndConvertsExceptionsToFailedChunk()
    {
        await using var app = await CreateAppAsync();

        using var response = await app.GetTestClient().GetAsync("/api/cqrs/action-filter/stream-exception");
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
        chunks[2].Final!.Value.Problem!.Detail.ShouldBe("Action filter stream failure");
    }

    [Fact]
    public async Task WithCommunicationCqrsFilters_StreamsCompletedIAsyncEnumerable()
    {
        await using var app = await CreateAppAsync();

        using var response = await app.GetTestClient().GetAsync("/api/cqrs/action-filter/stream");
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
        chunks[2].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
        chunks[2].Final.ShouldNotBeNull();
        chunks[2].Final!.Value.IsSuccess.ShouldBeTrue();
        chunks[2].Final!.Value.Value!.Status.ShouldBe("done");
    }

    [Fact]
    public async Task WithCommunicationCqrsFilters_CancelClientTokenStopsLongRunningStream()
    {
        await using var app = await CreateAppAsync();

        using var response = await app.GetTestClient().GetAsync("/api/cqrs/action-filter/stream-long-running");
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

    [Fact]
    public async Task WithCommunicationCqrsFilters_ActionThrowBeforeReturningStream_ReturnsFailedResult()
    {
        await using var app = await CreateAppAsync();

        using var response = await app.GetTestClient().GetAsync("/api/cqrs/action-filter/stream-action-exception");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<Result>();
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
        result.Problem!.Detail.ShouldBe("Action crashed before stream");
    }

    [Fact]
    public async Task WithCommunicationCqrsFilters_ClientDisposesEnumeratorWithoutTokenSignalsServerCancellation()
    {
        var cancellationDetected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var app = await CreateAppWithSignalEndpointAsync(cancellationDetected);
        using var client = app.GetTestClient();
        var chunks = new List<CqrsStreamChunk<ProgressUpdate, FinalResult>>();

        await using (var enumerator = client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("/api/cqrs/action-filter/stream-signal-cancel")
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

        builder.Services
            .AddControllers(options =>
        {
            CommunicationCqrsExtensions.MvcOptionsExtensions.AddCommunicationCqrsFilters(options);
            CommunicationCoreExtensions.MvcOptionsExtensions.AddCommunicationFilters(options);
        })
            .AddApplicationPart(typeof(CqrsActionFilterController).Assembly);

        var app = builder.Build();
        app.MapControllers();

        await app.StartAsync();
        return app;
    }

    private static async Task<WebApplication> CreateAppWithSignalEndpointAsync(
        TaskCompletionSource<bool> cancellationDetected)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddControllers(options =>
            {
                CommunicationCqrsExtensions.MvcOptionsExtensions.AddCommunicationCqrsFilters(options);
                CommunicationCoreExtensions.MvcOptionsExtensions.AddCommunicationFilters(options);
            })
            .AddApplicationPart(typeof(CqrsActionFilterController).Assembly);

        var app = builder.Build();
        app.MapGet(
                "/api/cqrs/action-filter/stream-signal-cancel",
                (CancellationToken cancellationToken) =>
                    RunLongRunningStreamWithServerCancellationSignal(cancellationDetected, cancellationToken))
            .WithCommunicationCqrsResults();

        await app.StartAsync();
        return app;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static async IAsyncEnumerable<CqrsStreamChunk<ProgressUpdate, FinalResult>> RunLongRunningStreamWithServerCancellationSignal(
        TaskCompletionSource<bool> cancellationDetected,
        [EnumeratorCancellation] CancellationToken cancellationToken = default,
        TimeSpan? delay = null)
    {
        try
        {
            yield return CqrsStreamChunk<ProgressUpdate, FinalResult>.Started(
                Result<ProgressUpdate>.Succeed(new ProgressUpdate("started")),
                message: "command started",
                sequence: 0);

            var effectiveDelay = delay ?? TimeSpan.FromSeconds(1);

            for (var i = 1; i <= 10; i++)
            {
                await Task.Delay(effectiveDelay, cancellationToken).ConfigureAwait(false);
                yield return CqrsStreamChunk<ProgressUpdate, FinalResult>.Progress(
                    Result<ProgressUpdate>.Succeed(new ProgressUpdate($"tick {i}")),
                    message: "in progress",
                    sequence: i);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            yield return CqrsStreamChunk<ProgressUpdate, FinalResult>.Completed(
                Result<FinalResult>.Succeed(new FinalResult("done")),
                message: "command completed",
                sequence: 11);
        }
        finally
        {
            cancellationDetected.TrySetResult(cancellationToken.IsCancellationRequested);
        }
    }
}

[ApiController]
[Route("api/cqrs/action-filter")]
public sealed class CqrsActionFilterController : ControllerBase
{
    [HttpGet("stream-exception")]
    public IAsyncEnumerable<CqrsStreamChunk<ProgressUpdate, FinalResult>> RunExceptionStream()
    {
        return RunStreamThatThrows();
    }

    [HttpGet("stream")]
    public IAsyncEnumerable<CqrsStreamChunk<ProgressUpdate, FinalResult>> RunCompletedStream()
    {
        return RunCompletedCqrsStream();
    }

    [HttpGet("stream-action-exception")]
    public IAsyncEnumerable<CqrsStreamChunk<ProgressUpdate, FinalResult>> RunActionThrowBeforeReturningStream()
    {
        throw new InvalidOperationException("Action crashed before stream");
    }

    [HttpGet("stream-long-running")]
    public IAsyncEnumerable<CqrsStreamChunk<ProgressUpdate, FinalResult>> RunLongStream(CancellationToken cancellationToken)
    {
        return RunLongRunningStream(cancellationToken);
    }

    private static async IAsyncEnumerable<CqrsStreamChunk<ProgressUpdate, FinalResult>> RunStreamThatThrows()
    {
        yield return CqrsStreamChunk<ProgressUpdate, FinalResult>.Started(
            Result<ProgressUpdate>.Succeed(new ProgressUpdate("started")),
            sequence: 1);

        await Task.Delay(1);
        yield return CqrsStreamChunk<ProgressUpdate, FinalResult>.Progress(
            Result<ProgressUpdate>.Succeed(new ProgressUpdate("processing")),
            sequence: 2);

        throw new InvalidOperationException("Action filter stream failure");
    }

    private static async IAsyncEnumerable<CqrsStreamChunk<ProgressUpdate, FinalResult>> RunCompletedCqrsStream()
    {
        yield return CqrsStreamChunk<ProgressUpdate, FinalResult>.Started(
            Result<ProgressUpdate>.Succeed(new ProgressUpdate("started")),
            sequence: 1);

        await Task.Delay(1);
        yield return CqrsStreamChunk<ProgressUpdate, FinalResult>.Progress(
            Result<ProgressUpdate>.Succeed(new ProgressUpdate("processing")),
            sequence: 2);

        yield return CqrsStreamChunk<ProgressUpdate, FinalResult>.Completed(
            Result<FinalResult>.Succeed(new FinalResult("done")),
            sequence: 3);
    }

    private static async IAsyncEnumerable<CqrsStreamChunk<ProgressUpdate, FinalResult>> RunLongRunningStream(
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

}

public sealed record ProgressUpdate(string State);
public sealed record FinalResult(string Status);
