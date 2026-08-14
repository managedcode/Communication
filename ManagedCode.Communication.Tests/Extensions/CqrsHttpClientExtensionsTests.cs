using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.CQRS.Extensions.Http;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.Extensions;

public class CqrsHttpClientExtensionsTests
{
    [Fact]
    public async Task SendForCqrsStreamAsync_ParsesChunkStream()
    {
        using var client = new HttpClient(new StubHandler(static (_, cancellationToken) =>
        {
            var payload = BuildPayload(
                CqrsStreamChunk<ProgressUpdate, FinalResult>.Started(
                    Result<ProgressUpdate>.Succeed(new ProgressUpdate("started")),
                    sequence: 1),
                CqrsStreamChunk<ProgressUpdate, FinalResult>.Progress(
                    Result<ProgressUpdate>.Succeed(new ProgressUpdate("progress")),
                    sequence: 2),
                CqrsStreamChunk<ProgressUpdate, FinalResult>.Completed(
                    Result<FinalResult>.Succeed(new FinalResult("done")),
                    sequence: 3));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "text/event-stream")
            });
        }));

        var chunks = new List<CqrsStreamChunk<ProgressUpdate, FinalResult>>();
        await foreach (var item in client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("https://example.com/api/cqrs"))
        {
            chunks.Add(item);
        }

        chunks.Count.ShouldBe(3);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunks[1].Kind.ShouldBe(CqrsStreamChunkKind.Progress);
        chunks[2].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
        chunks[2].Final!.Value.Value!.Status.ShouldBe("done");
    }

    [Fact]
    public async Task GetForCqrsStreamAsync_AddsEventStreamAcceptHeader()
    {
        HttpRequestMessage? request = null;

        using var client = new HttpClient(new StubHandler(async (requestMessage, cancellationToken) =>
        {
            request = requestMessage;

            var payload = BuildPayload(
                CqrsStreamChunk<ProgressUpdate, FinalResult>.Completed(
                    Result<FinalResult>.Succeed(new FinalResult("done")),
                    sequence: 1));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "text/event-stream")
            };
        }));

        await foreach (var _ in client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("https://example.com/api/cqrs"))
        {
        }

        request.ShouldNotBeNull();
        request!.Method.ShouldBe(HttpMethod.Get);
        request!.RequestUri!.ToString().ShouldBe("https://example.com/api/cqrs");
        request!.Headers.Accept.ShouldContain(x => x.MediaType == "text/event-stream");
    }

    [Fact]
    public async Task PostForCqrsStreamAsync_WithBody_SendsJsonPayloadAndAcceptsStream()
    {
        HttpRequestMessage? request = null;
        string? requestPayload = null;
        var command = new SubmitCommand("hello");

        using var client = new HttpClient(new StubHandler(async (requestMessage, cancellationToken) =>
        {
            request = requestMessage;
            requestPayload = await requestMessage.Content!.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            var payload = BuildPayload(
                CqrsStreamChunk<ProgressUpdate, FinalResult>.Completed(
                    Result<FinalResult>.Succeed(new FinalResult("done")),
                    sequence: 1));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "text/event-stream")
            };
        }));

        await foreach (var _ in client.PostForCqrsStreamAsync<ProgressUpdate, FinalResult, SubmitCommand>(
                           "https://example.com/api/cqrs", command))
        {
        }

        request.ShouldNotBeNull();
        requestPayload.ShouldNotBeNull();
        request!.Method.ShouldBe(HttpMethod.Post);
        request!.Content.ShouldNotBeNull();
        request!.Content.Headers.ContentType!.MediaType.ShouldBe("application/json");
        request!.Headers.Accept.ShouldContain(x => x.MediaType == "text/event-stream");

        var deserializedBody = JsonSerializer.Deserialize<SubmitCommand>(requestPayload!, JsonOptions);
        deserializedBody.ShouldNotBeNull();
        deserializedBody.ShouldBe(command);
    }

    [Fact]
    public async Task PostForCqrsStreamAsync_WithoutBody_SendsPostMethodAndAcceptsStream()
    {
        HttpRequestMessage? request = null;

        using var client = new HttpClient(new StubHandler(async (requestMessage, cancellationToken) =>
        {
            request = requestMessage;

            var payload = BuildPayload(
                CqrsStreamChunk<ProgressUpdate, FinalResult>.Completed(
                    Result<FinalResult>.Succeed(new FinalResult("done")),
                    sequence: 1));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "text/event-stream")
            };
        }));

        await foreach (var _ in client.PostForCqrsStreamAsync<ProgressUpdate, FinalResult>("https://example.com/api/cqrs"))
        {
        }

        request.ShouldNotBeNull();
        request!.Method.ShouldBe(HttpMethod.Post);
        request!.Headers.Accept.ShouldContain(x => x.MediaType == "text/event-stream");
    }

    [Fact]
    public async Task SendForCqrsStreamAsync_WithPreCanceledToken_ThrowsOperationCanceled()
    {
        using var client = new HttpClient(new StubHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(1000, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(string.Empty, Encoding.UTF8, "text/event-stream")
            };
        }));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>(
                "https://example.com/api/cqrs", cancellation.Token))
            {
            }
        });
    }

    private static string BuildPayload(params object[] chunks)
    {
        var payloads = chunks.Select(static chunk =>
            JsonSerializer.Serialize(chunk, JsonOptions));
        return string.Concat(payloads.Select(item => $"data: {item}\n\n"));
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }

    internal record ProgressUpdate(string State);

    internal record SubmitCommand(string Payload);

    internal record FinalResult(string Status);
}
