using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Runtime.CompilerServices;
using ManagedCode.Communication;
using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.CQRS.AspNetCore.Extensions;
using ManagedCode.Communication.CQRS.Extensions.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.Extensions;

public class CqrsHttpClientIntegrationTests
{
    [Fact]
    public async Task GetForCqrsStreamAsync_Integration_ReadsTerminalCompletedChunk()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/cqrs-completed", static () => RunCompletedStream())
                .WithCommunicationCqrsResults();
        });

        using var client = app.GetTestClient();

        var chunks = new List<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>>();
        await foreach (var chunk in client.GetForCqrsStreamAsync<IntegrationProgressUpdate, IntegrationFinalResult>("/cqrs-completed"))
        {
            chunks.Add(chunk);
        }

        chunks.Count.ShouldBe(3);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunks[1].Kind.ShouldBe(CqrsStreamChunkKind.Progress);
        chunks[2].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
        chunks[2].Final!.Value.IsSuccess.ShouldBeTrue();
        chunks[2].Final!.Value.Value!.Status.ShouldBe("done");
    }

    [Fact]
    public async Task GetForCqrsStreamAsync_Integration_ParsesTerminalFailedChunk()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/cqrs-failed", static () => RunFailedStream())
                .WithCommunicationCqrsResults();
        });

        using var client = app.GetTestClient();

        var chunks = new List<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>>();
        await foreach (var chunk in client.GetForCqrsStreamAsync<IntegrationProgressUpdate, IntegrationFinalResult>("/cqrs-failed"))
        {
            chunks.Add(chunk);
        }

        chunks.Count.ShouldBe(2);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunks[1].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[1].Final.ShouldNotBeNull();
        chunks[1].Final!.Value.IsSuccess.ShouldBeFalse();
        chunks[1].Final!.Value.Problem!.StatusCode.ShouldBe(500);
    }

    [Fact]
    public async Task GetForCqrsStreamAsync_Integration_StopsWhenClientCancels()
    {
        using var cancellation = new System.Threading.CancellationTokenSource();

        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/cqrs-long", static (System.Threading.CancellationToken cancellationToken) =>
                    RunLongRunningStream(cancellationToken))
                .WithCommunicationCqrsResults();
        });

        using var client = app.GetTestClient();
        var chunks = new List<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>>();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var chunk in client.GetForCqrsStreamAsync<IntegrationProgressUpdate, IntegrationFinalResult>(
                               "/cqrs-long", cancellation.Token))
            {
                chunks.Add(chunk);

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

    private static async IAsyncEnumerable<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>> RunCompletedStream()
    {
        yield return CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>.Started(
            Result<IntegrationProgressUpdate>.Succeed(new IntegrationProgressUpdate("started")),
            sequence: 1);

        yield return CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>.Progress(
            Result<IntegrationProgressUpdate>.Succeed(new IntegrationProgressUpdate("processing")),
            sequence: 2);

        await Task.Delay(1);
        yield return CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>.Completed(
            Result<IntegrationFinalResult>.Succeed(new IntegrationFinalResult("done")),
            sequence: 3);
    }

    private static async IAsyncEnumerable<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>> RunFailedStream()
    {
        yield return CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>.Started(
            Result<IntegrationProgressUpdate>.Succeed(new IntegrationProgressUpdate("started")),
            sequence: 1);

        await Task.Delay(1);
        yield return CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>.Failed(
            Problem.Create("failed", "Boom", 500),
            message: "operation failed",
            sequence: 2);
    }

    private static async IAsyncEnumerable<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>> RunLongRunningStream(
        [EnumeratorCancellation] System.Threading.CancellationToken cancellationToken = default)
    {
        yield return CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>.Started(
            Result<IntegrationProgressUpdate>.Succeed(new IntegrationProgressUpdate("started")),
            sequence: 0);

        for (var i = 1; i <= 10; i++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken).ConfigureAwait(false);
            yield return CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>.Progress(
                Result<IntegrationProgressUpdate>.Succeed(new IntegrationProgressUpdate($"tick {i}")),
                sequence: i);
        }

        yield return CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>.Completed(
            Result<IntegrationFinalResult>.Succeed(new IntegrationFinalResult("done")),
            sequence: 11);
    }

    private sealed record IntegrationProgressUpdate(string State);
    private sealed record IntegrationFinalResult(string Status);
}
