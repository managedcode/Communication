using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.ServerSentEvents;
using System.Threading;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.CQRS.AspNetCore.Extensions;
using ManagedCode.Communication.CQRS.Extensions.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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

    [Fact]
    public async Task GetForCqrsStreamAsync_Integration_StreamLongRunningOperationEmitsProgressPerSecondAndCompletes()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/cqrs-long-running", static () => RunLongRunningStream(delay: TimeSpan.FromSeconds(1)))
                .WithCommunicationCqrsResults();
        });

        using var client = app.GetTestClient();

        var chunks = new List<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>>();
        await foreach (var chunk in client.GetForCqrsStreamAsync<IntegrationProgressUpdate, IntegrationFinalResult>("/cqrs-long-running"))
        {
            chunks.Add(chunk);
        }

        chunks.Count.ShouldBe(12);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
        chunks[^1].Final!.Value.IsSuccess.ShouldBeTrue();

        var progressCount = 0;
        foreach (var chunk in chunks)
        {
            if (chunk.Kind == CqrsStreamChunkKind.Progress)
            {
                progressCount++;
            }
        }

        progressCount.ShouldBe(10);
    }

    [Fact]
    public async Task GetForCqrsStreamAsync_Integration_ServerErrorReturnsFailedChunk()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/cqrs-server-error", static () =>
                TypedResults.Problem(
                    title: "server unavailable",
                    detail: "critical dependency failed",
                    statusCode: 500));
        });

        using var client = app.GetTestClient();

        var chunks = new List<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>>();
        await foreach (var chunk in client.GetForCqrsStreamAsync<IntegrationProgressUpdate, IntegrationFinalResult>("/cqrs-server-error"))
        {
            chunks.Add(chunk);
        }

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[0].Final.ShouldNotBeNull();
        chunks[0].Final!.Value.IsSuccess.ShouldBeFalse();
        chunks[0].Final!.Value.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
        chunks[0].Final!.Value.Problem!.Detail.ShouldBe("critical dependency failed");
    }

    [Fact]
    public async Task GetForCqrsStreamAsync_Integration_ServerErrorReturnsFailedChunkFromTextBody()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/cqrs-server-error-text", static () =>
                TypedResults.Text("server unavailable", statusCode: 500));
        });

        using var client = app.GetTestClient();

        var chunks = new List<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>>();
        await foreach (var chunk in client.GetForCqrsStreamAsync<IntegrationProgressUpdate, IntegrationFinalResult>("/cqrs-server-error-text"))
        {
            chunks.Add(chunk);
        }

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[0].Final.ShouldNotBeNull();
        chunks[0].Final!.Value.IsSuccess.ShouldBeFalse();
        chunks[0].Final!.Value.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
        chunks[0].Final!.Value.Problem!.Detail.ShouldBe("server unavailable");
    }

    [Fact]
    public async Task GetForCqrsStreamAsync_Integration_ServerErrorProblemWithMissingFieldsFallsBackToResponseMetadata()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/cqrs-server-error-problem-missing-fields", static () =>
                TypedResults.Text(
                    "{\"detail\":\"dependency failed\",\"status\":0}",
                    statusCode: 500,
                    contentType: "application/problem+json"));
        });

        using var client = app.GetTestClient();

        var chunks = new List<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>>();
        await foreach (var chunk in client.GetForCqrsStreamAsync<IntegrationProgressUpdate, IntegrationFinalResult>(
                           "/cqrs-server-error-problem-missing-fields"))
        {
            chunks.Add(chunk);
        }

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[0].Final.ShouldNotBeNull();
        chunks[0].Final!.Value.IsSuccess.ShouldBeFalse();
        chunks[0].Final!.Value.Problem.ShouldNotBeNull();
        chunks[0].Final!.Value.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
        chunks[0].Final!.Value.Problem!.Title.ShouldBe("Internal Server Error");
        chunks[0].Final!.Value.Problem!.Type.ShouldBe("InternalServerError");
        chunks[0].Final!.Value.Problem!.Detail.ShouldBe("dependency failed");
    }

    [Fact]
    public async Task GetForCqrsStreamAsync_Integration_ServerErrorProblemPreservesProvidedTypeAndTitle()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/cqrs-server-error-problem-preserve-metadata", static () =>
                TypedResults.Text(
                    "{\"type\":\"https://errors.example/cqrs\",\"title\":\"service_unavailable\",\"status\":500,\"detail\":\"custom issue\"}",
                    statusCode: 500,
                    contentType: "application/problem+json"));
        });

        using var client = app.GetTestClient();

        var chunks = new List<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>>();
        await foreach (var chunk in client.GetForCqrsStreamAsync<IntegrationProgressUpdate, IntegrationFinalResult>(
                           "/cqrs-server-error-problem-preserve-metadata"))
        {
            chunks.Add(chunk);
        }

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[0].Final.ShouldNotBeNull();
        chunks[0].Final!.Value.IsSuccess.ShouldBeFalse();
        chunks[0].Final!.Value.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
        chunks[0].Final!.Value.Problem!.Type.ShouldBe("https://errors.example/cqrs");
        chunks[0].Final!.Value.Problem!.Title.ShouldBe("service_unavailable");
        chunks[0].Final!.Value.Problem!.Detail.ShouldBe("custom issue");
    }

    [Fact]
    public async Task GetForCqrsStreamAsync_Integration_ServerErrorProblemBodyNullFallsBackToStringResponse()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/cqrs-server-error-problem-null", static () =>
                TypedResults.Text("null", statusCode: 500, contentType: "application/problem+json"));
        });

        using var client = app.GetTestClient();

        var chunks = new List<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>>();
        await foreach (var chunk in client.GetForCqrsStreamAsync<IntegrationProgressUpdate, IntegrationFinalResult>(
                           "/cqrs-server-error-problem-null"))
        {
            chunks.Add(chunk);
        }

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[0].Final.ShouldNotBeNull();
        chunks[0].Final!.Value.IsSuccess.ShouldBeFalse();
        chunks[0].Final!.Value.Problem.ShouldNotBeNull();
        chunks[0].Final!.Value.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
        chunks[0].Final!.Value.Problem!.Detail.ShouldBe("null");
    }

    [Fact]
    public async Task GetForCqrsStreamAsync_Integration_UnhandledCrashWithoutExceptionHandlingThrows()
    {
        await AssertUnhandledCrashEndpointReturnsFailedChunkOrThrows();
    }

    [Fact]
    public async Task GetForCqrsStreamAsync_Integration_HandledStreamFailureIsReturnedAsFailedChunk()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/cqrs-stream-unhandled", static () => RunExceptionThenFailStream())
                .WithCommunicationCqrsResults();
        });

        using var client = app.GetTestClient();

        var chunks = new List<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>>();
        await foreach (var chunk in client.GetForCqrsStreamAsync<IntegrationProgressUpdate, IntegrationFinalResult>("/cqrs-stream-unhandled"))
        {
            chunks.Add(chunk);
        }

        chunks.Count.ShouldBe(3);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunks[1].Kind.ShouldBe(CqrsStreamChunkKind.Progress);
        chunks[2].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[2].Final.ShouldNotBeNull();
        chunks[2].Final!.Value.IsSuccess.ShouldBeFalse();
        chunks[2].Final!.Value.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task SendForCqrsStreamAsync_Integration_UsesRequestFactoryOverload()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/cqrs-facade-request-factory", static () => RunCompletedStream())
                .WithCommunicationCqrsResults();
        });

        using var client = app.GetTestClient();
        var chunks = new List<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>>();

        await foreach (var chunk in ManagedCode.Communication.AspNetCore.Extensions.Http.CqrsHttpClientExtensions.SendForCqrsStreamAsync<IntegrationProgressUpdate, IntegrationFinalResult>(
                           client,
                           () => new HttpRequestMessage(HttpMethod.Get, "/cqrs-facade-request-factory")))
        {
            chunks.Add(chunk);
        }

        chunks.Count.ShouldBe(3);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunks[1].Kind.ShouldBe(CqrsStreamChunkKind.Progress);
        chunks[2].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
    }

    [Fact]
    public async Task GetForCqrsStreamAsync_Integration_InvalidSsePayloadThrowsParsingError()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/cqrs-invalid-sse", static () =>
                TypedResults.Text("data: {invalid-json\n\n", statusCode: 200, contentType: "text/event-stream"));
        });

        using var client = app.GetTestClient();

        await Should.ThrowAsync<System.Text.Json.JsonException>(async () =>
        {
            await foreach (var _ in client.GetForCqrsStreamAsync<IntegrationProgressUpdate, IntegrationFinalResult>("/cqrs-invalid-sse"))
            {
            }
        });
    }

    [Fact]
    public async Task GetForCqrsStreamAsync_Integration_NullSseChunkPayloadThrowsParsingError()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/cqrs-null-sse", static () =>
                TypedResults.Text("data: null\n\n", statusCode: 200, contentType: "text/event-stream"));
        });

        using var client = app.GetTestClient();

        await Should.ThrowAsync<System.Text.Json.JsonException>(async () =>
        {
            await foreach (var _ in client.GetForCqrsStreamAsync<IntegrationProgressUpdate, IntegrationFinalResult>("/cqrs-null-sse"))
            {
            }
        });
    }

    [Fact]
    public async Task GetForCqrsStreamAsync_Integration_StreamThatFailsImmediatelyReturnsFailedChunk()
    {
        await AssertImmediateFailureEndpointReturnsFailedChunkOrThrows();
    }

    [Fact]
    public async Task GetForCqrsStreamAsync_Integration_StreamThatFailsImmediatelyThrows()
    {
        await AssertImmediateFailureEndpointReturnsFailedChunkOrThrows();
    }

    private static async Task AssertImmediateFailureEndpointReturnsFailedChunkOrThrows()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/cqrs-stream-immediate-failure", static () => RunStreamThatThrowsImmediately())
                .WithCommunicationCqrsResults();
        });

        using var client = app.GetTestClient();

        var chunks = new List<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>>();
        InvalidOperationException? exception = null;

        try
        {
            await foreach (var chunk in client.GetForCqrsStreamAsync<IntegrationProgressUpdate, IntegrationFinalResult>(
                               "/cqrs-stream-immediate-failure"))
            {
                chunks.Add(chunk);
            }
        }
        catch (InvalidOperationException ex)
        {
            exception = ex;
        }

        if (exception is not null)
        {
            chunks.Count.ShouldBe(0);
            return;
        }

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[0].Final.ShouldNotBeNull();
        chunks[0].Final!.Value.IsSuccess.ShouldBeFalse();
        chunks[0].Final!.Value.Problem.ShouldNotBeNull();
        chunks[0].Final!.Value.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
        chunks[0].Final!.Value.Problem!.Title.ShouldBe("InvalidOperationException");
        chunks[0].Final!.Value.Problem!.Detail.ShouldBe("Immediate stream failure");
    }

    private static async Task AssertUnhandledCrashEndpointReturnsFailedChunkOrThrows()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/cqrs-server-crash", static () => CrashEndpoint());
        });

        using var client = app.GetTestClient();

        var chunks = new List<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>>();
        InvalidOperationException? exception = null;

        try
        {
            await foreach (var chunk in client.GetForCqrsStreamAsync<IntegrationProgressUpdate, IntegrationFinalResult>("/cqrs-server-crash"))
            {
                chunks.Add(chunk);
            }
        }
        catch (InvalidOperationException ex)
        {
            exception = ex;
        }

        if (exception is not null)
        {
            chunks.Count.ShouldBe(0);
            return;
        }

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[0].Final.ShouldNotBeNull();
        chunks[0].Final!.Value.IsSuccess.ShouldBeFalse();
        chunks[0].Final!.Value.Problem.ShouldNotBeNull();
        chunks[0].Final!.Value.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task PostForCqrsStreamAsync_Integration_UsesBodyAndReturnsStreamChunks()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapPost("/cqrs-post-body", static (IntegrationCommand command) =>
                    RunCompletedStream(command))
                .WithCommunicationCqrsResults();
        });

        using var client = app.GetTestClient();
        var chunks = new List<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>>();

        await foreach (var chunk in client.PostForCqrsStreamAsync<IntegrationProgressUpdate, IntegrationFinalResult, IntegrationCommand>(
                           "/cqrs-post-body",
                           new IntegrationCommand("job-99")))
        {
            chunks.Add(chunk);
        }

        chunks.Count.ShouldBe(3);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunks[0].EventType.ShouldBe("cqrs-started");
        chunks[1].Kind.ShouldBe(CqrsStreamChunkKind.Progress);
        chunks[2].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
        chunks[2].Final!.Value.IsSuccess.ShouldBeTrue();
        chunks[2].Final!.Value.Value!.Status.ShouldBe("done job-99");
    }

    [Fact]
    public async Task GetForCqrsStreamAsync_Integration_TotalProgressCanBeZero()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/cqrs-no-progress", static () =>
                RunLongRunningStream(totalTicks: 0, delay: TimeSpan.FromMilliseconds(1)))
                .WithCommunicationCqrsResults();
        });

        using var client = app.GetTestClient();

        var chunks = new List<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>>();
        await foreach (var chunk in client.GetForCqrsStreamAsync<IntegrationProgressUpdate, IntegrationFinalResult>("/cqrs-no-progress"))
        {
            chunks.Add(chunk);
        }

        chunks.Count.ShouldBe(2);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunks[1].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
    }

    [Fact]
    public async Task GetForCqrsStreamAsync_Integration_CancelledTokenBeforeCallThrows()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/cqrs-cancel-before-start", static () => RunCompletedStream())
                .WithCommunicationCqrsResults();
        });

        using var client = app.GetTestClient();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in client.GetForCqrsStreamAsync<IntegrationProgressUpdate, IntegrationFinalResult>(
                               "/cqrs-cancel-before-start", cancellation.Token))
            {
            }
        });
    }

    [Fact]
    public async Task GetForCqrsStreamAsync_Integration_UnhandledServerCrashReturnsFailedChunk()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        app.UseExceptionHandler("/cqrs-server-error");
        app.MapGet("/cqrs-server-error", static () => TypedResults.Text("server exploded", statusCode: 500));
        app.MapGet("/cqrs-server-crash", static () => CrashEndpoint());

        await app.StartAsync();

        using var client = app.GetTestClient();

        var chunks = new List<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>>();
        await foreach (var chunk in client.GetForCqrsStreamAsync<IntegrationProgressUpdate, IntegrationFinalResult>("/cqrs-server-crash"))
        {
            chunks.Add(chunk);
        }

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[0].Final.ShouldNotBeNull();
        chunks[0].Final!.Value.IsSuccess.ShouldBeFalse();
        chunks[0].Final!.Value.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
        chunks[0].Final!.Value.Problem!.Detail.ShouldBe("server exploded");
    }

    [Fact]
    public async Task GetForCqrsStreamAsync_Integration_ClientStopsConsumingWithoutCancellationToken()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/cqrs-long-running-consumer-stop", static () => RunLongRunningStream(delay: TimeSpan.FromSeconds(1)))
                .WithCommunicationCqrsResults();
        });

        using var client = app.GetTestClient();
        var chunks = new List<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>>();

        await using (var enumerator = client
            .GetForCqrsStreamAsync<IntegrationProgressUpdate, IntegrationFinalResult>("/cqrs-long-running-consumer-stop")
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
    }

    [Fact]
    public async Task GetForCqrsStreamAsync_Integration_ClientDisconnectWithoutTokenSignalsServerCancellation()
    {
        var cancellationDetected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var app = await CreateAppAsync(app =>
        {
            app.MapGet("/cqrs-long-running-consumer-stop-signaled", (CancellationToken cancellationToken) =>
                    RunLongRunningStreamWithServerCancellationSignal(cancellationDetected, cancellationToken))
                .WithCommunicationCqrsResults();
        });

        using var client = app.GetTestClient();
        var chunks = new List<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>>();

        await using (var enumerator = client
                   .GetForCqrsStreamAsync<IntegrationProgressUpdate, IntegrationFinalResult>(
                       "/cqrs-long-running-consumer-stop-signaled")
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

    [Fact]
    public async Task GetForCqrsStreamAsync_Integration_ClientDisconnectsRawHttpResponseCanBeCancelled()
    {
        using var cancellation = new CancellationTokenSource();

        await using var app = await CreateAppWithRawDisconnectEndpointAsync();

        using var client = app.GetTestClient();
        var chunks = new List<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>>();

        using var response = await client.GetAsync(
            "/cqrs-long-running-consumer-raw-disconnect",
            HttpCompletionOption.ResponseHeadersRead,
            cancellation.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        await using var responseStream = await response.Content.ReadAsStreamAsync();
        var parser = SseParser.Create(
            responseStream,
            (_, data) => JsonSerializer.Deserialize<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>>(data, jsonOptions) ??
                                  throw new JsonException("Chunk payload missing."));

        await using (var enumerator = parser.EnumerateAsync(cancellation.Token).GetAsyncEnumerator())
        {
            for (var i = 0; i < 4; i++)
            {
                if (!await enumerator.MoveNextAsync())
                {
                    break;
                }

                    chunks.Add(enumerator.Current.Data);
            }

            cancellation.Cancel();

            await Should.ThrowAsync<OperationCanceledException>(async () =>
            {
                await enumerator.MoveNextAsync();
            });
        }
        
        chunks.Count.ShouldBe(4);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunks.ShouldContain(chunk => chunk.Kind == CqrsStreamChunkKind.Progress);
    }

    private static async Task<WebApplication> CreateAppWithRawDisconnectEndpointAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        var app = builder.Build();
        app.MapGet(
                "/cqrs-long-running-consumer-raw-disconnect",
                (CancellationToken cancellationToken) =>
                    RunLongRunningStream(cancellationToken, delay: TimeSpan.FromMilliseconds(10)))
            .WithCommunicationCqrsResults();

        await app.StartAsync();
        return app;
    }

    [Fact]
    public async Task GetForCqrsStreamAsync_Integration_ClientStopsAndCanResumeTheStreamFromStart()
    {
        await using var app = await CreateAppAsync(static app =>
        {
            app.MapGet("/cqrs-long-running-resume", static () => RunLongRunningStream(delay: TimeSpan.FromMilliseconds(20)))
                .WithCommunicationCqrsResults();
        });

        using var client = app.GetTestClient();

        var firstRunChunks = new List<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>>();
        await using (var enumerator = client
                   .GetForCqrsStreamAsync<IntegrationProgressUpdate, IntegrationFinalResult>("/cqrs-long-running-resume")
                   .GetAsyncEnumerator())
        {
            for (var i = 0; i < 4; i++)
            {
                if (!await enumerator.MoveNextAsync())
                {
                    break;
                }

                firstRunChunks.Add(enumerator.Current);
            }
        }

        firstRunChunks.Count.ShouldBe(4);
        firstRunChunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        firstRunChunks.ShouldContain(chunk => chunk.Kind == CqrsStreamChunkKind.Progress);

        var secondRunChunks = new List<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>>();
        await foreach (var chunk in client.GetForCqrsStreamAsync<IntegrationProgressUpdate, IntegrationFinalResult>("/cqrs-long-running-resume"))
        {
            secondRunChunks.Add(chunk);
        }

        secondRunChunks.Count.ShouldBe(12);
        secondRunChunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        secondRunChunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
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

    private static async IAsyncEnumerable<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>> RunCompletedStream(
        IntegrationCommand command)
    {
        yield return CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>.Started(
            Result<IntegrationProgressUpdate>.Succeed(new IntegrationProgressUpdate("started")),
            sequence: 1);

        await Task.Delay(1);
        yield return CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>.Progress(
            Result<IntegrationProgressUpdate>.Succeed(new IntegrationProgressUpdate($"processing {command.Task}")),
            sequence: 2);

        await Task.Delay(1);
        yield return CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>.Completed(
            Result<IntegrationFinalResult>.Succeed(new IntegrationFinalResult($"done {command.Task}")),
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
        [EnumeratorCancellation] System.Threading.CancellationToken cancellationToken = default,
        int totalTicks = 10,
        TimeSpan? delay = null)
    {
        yield return CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>.Started(
            Result<IntegrationProgressUpdate>.Succeed(new IntegrationProgressUpdate("started")),
            sequence: 0);

        var effectiveDelay = delay ?? TimeSpan.FromMilliseconds(10);

        for (var i = 1; i <= totalTicks; i++)
        {
            await Task.Delay(effectiveDelay, cancellationToken).ConfigureAwait(false);
            yield return CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>.Progress(
                Result<IntegrationProgressUpdate>.Succeed(new IntegrationProgressUpdate($"tick {i}")),
                sequence: i);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            yield break;
        }

        yield return CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>.Completed(
            Result<IntegrationFinalResult>.Succeed(new IntegrationFinalResult("done")),
            sequence: 11);
    }

    private static async IAsyncEnumerable<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>> RunExceptionThenFailStream()
    {
        yield return CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>.Started(
            Result<IntegrationProgressUpdate>.Succeed(new IntegrationProgressUpdate("started")),
            sequence: 1);

        await Task.Delay(1);
        yield return CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>.Progress(
            Result<IntegrationProgressUpdate>.Succeed(new IntegrationProgressUpdate("processing")),
            sequence: 2);

        throw new InvalidOperationException("Unhandled stream failure");
    }

    private static async IAsyncEnumerable<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>> RunStreamThatThrowsImmediately()
    {
        await Task.Yield();
        yield return null!;
        throw new InvalidOperationException("Immediate stream failure");
    }

    private static async IAsyncEnumerable<CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>>
        RunLongRunningStreamWithServerCancellationSignal(
            TaskCompletionSource<bool> cancellationDetected,
            [EnumeratorCancellation] CancellationToken cancellationToken = default,
            TimeSpan? delay = null)
    {
        try
        {
            yield return CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>.Started(
                Result<IntegrationProgressUpdate>.Succeed(new IntegrationProgressUpdate("started")),
                sequence: 0);

            var effectiveDelay = delay ?? TimeSpan.FromMilliseconds(10);

            for (var i = 1; i <= 10; i++)
            {
                await Task.Delay(effectiveDelay, cancellationToken).ConfigureAwait(false);
                yield return CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>.Progress(
                    Result<IntegrationProgressUpdate>.Succeed(new IntegrationProgressUpdate($"tick {i}")),
                    sequence: i);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            yield return CqrsStreamChunk<IntegrationProgressUpdate, IntegrationFinalResult>.Completed(
                Result<IntegrationFinalResult>.Succeed(new IntegrationFinalResult("done")),
                sequence: 11);
        }
        finally
        {
            cancellationDetected.TrySetResult(cancellationToken.IsCancellationRequested);
        }
    }

    private static IResult CrashEndpoint()
    {
        throw new InvalidOperationException("Server exploded");
    }

    private sealed record IntegrationProgressUpdate(string State);
    private sealed record IntegrationFinalResult(string Status);
    private sealed record IntegrationCommand(string Task);
}
