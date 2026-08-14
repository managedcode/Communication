using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.CQRS.Extensions.Http;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.CQRS;

/// <summary>
///     Client-side reading, driven by a stub handler so every response shape — including ones a real server would
///     struggle to produce — can be exercised deterministically.
/// </summary>
public class CqrsHttpClientTests
{
    // ---------- positive ----------

    [Fact]
    public async Task ReadsACompleteChunkStream()
    {
        using var client = Client(StubHttpMessageHandler.RespondingWithSse(
            CqrsTestStreams.Started(sequence: 1),
            CqrsTestStreams.Progress("processing", 2),
            CqrsTestStreams.Completed(sequence: 3)));

        var chunks = await CollectAsync(client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("https://example.com/cqrs"));

        chunks.Select(chunk => chunk.Kind).ShouldBe([
            CqrsStreamChunkKind.Started,
            CqrsStreamChunkKind.Progress,
            CqrsStreamChunkKind.Completed
        ]);
        chunks[^1].TryGetResult(out var result).ShouldBeTrue();
        result.Status.ShouldBe("done");
    }

    [Fact]
    public async Task RequestsTheEventStreamMediaType()
    {
        var handler = StubHttpMessageHandler.RespondingWithSse(CqrsTestStreams.Completed(sequence: 1));
        using var client = Client(handler);

        await CollectAsync(client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("https://example.com/cqrs"));

        handler.LastRequest!.Method.ShouldBe(HttpMethod.Get);
        handler.LastRequest!.RequestUri!.ToString().ShouldBe("https://example.com/cqrs");
        handler.LastRequest!.Headers.Accept.ShouldContain(header => header.MediaType == "text/event-stream");
    }

    [Fact]
    public async Task PostWithBody_SendsCamelCaseJson()
    {
        var handler = StubHttpMessageHandler.RespondingWithSse(CqrsTestStreams.Completed(sequence: 1));
        using var client = Client(handler);

        await CollectAsync(client.PostForCqrsStreamAsync<ProgressUpdate, FinalResult, SubmitCommand>(
            "https://example.com/cqrs", new SubmitCommand("hello")));

        handler.LastRequest!.Method.ShouldBe(HttpMethod.Post);
        handler.LastRequest!.Content!.Headers.ContentType!.MediaType.ShouldBe("application/json");
        handler.LastRequestBody.ShouldBe("""{"payload":"hello"}""");
    }

    [Fact]
    public async Task PostWithoutBody_StillUsesPost()
    {
        var handler = StubHttpMessageHandler.RespondingWithSse(CqrsTestStreams.Completed(sequence: 1));
        using var client = Client(handler);

        await CollectAsync(client.PostForCqrsStreamAsync<ProgressUpdate, FinalResult>("https://example.com/cqrs"));

        handler.LastRequest!.Method.ShouldBe(HttpMethod.Post);
        handler.LastRequest!.Content.ShouldBeNull();
    }

    [Fact]
    public async Task SendWithAnArbitraryMethodIsSupported()
    {
        var handler = StubHttpMessageHandler.RespondingWithSse(CqrsTestStreams.Completed(sequence: 1));
        using var client = Client(handler);

        await CollectAsync(client.SendForCqrsStreamAsync<ProgressUpdate, FinalResult>(
            HttpMethod.Put, "https://example.com/cqrs"));

        handler.LastRequest!.Method.ShouldBe(HttpMethod.Put);
    }

    [Fact]
    public async Task TheRequestFactoryIsInvokedExactlyOncePerEnumeration()
    {
        var handler = StubHttpMessageHandler.RespondingWithSse(CqrsTestStreams.Completed(sequence: 1));
        using var client = Client(handler);
        var factoryCalls = 0;

        var stream = client.SendForCqrsStreamAsync<ProgressUpdate, FinalResult>(() =>
        {
            factoryCalls++;
            return new HttpRequestMessage(HttpMethod.Get, "https://example.com/cqrs");
        });

        factoryCalls.ShouldBe(0, "the request must not be built before enumeration starts");

        await CollectAsync(stream);
        factoryCalls.ShouldBe(1);

        await CollectAsync(stream);
        factoryCalls.ShouldBe(2, "re-enumerating issues a fresh request");
    }

    // ---------- transport failures become terminal chunks ----------

    [Fact]
    public async Task ProblemJsonErrorResponse_BecomesATerminalFailedChunk()
    {
        var payload = JsonSerializer.Serialize(
            Problem.Create("server unavailable", "dependency failed", 503),
            CqrsStreamSerialization.Default);

        using var client = Client(StubHttpMessageHandler.RespondingWith(
            HttpStatusCode.ServiceUnavailable, payload, "application/problem+json"));

        var chunks = await CollectAsync(client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("https://example.com/cqrs"));

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[0].Problem!.StatusCode.ShouldBe(503);
        chunks[0].Problem!.Title.ShouldBe("server unavailable");
        chunks[0].Problem!.Detail.ShouldBe("dependency failed");
    }

    [Fact]
    public async Task ProblemJsonWithMissingFields_IsFilledInFromTheResponse()
    {
        using var client = Client(StubHttpMessageHandler.RespondingWith(
            HttpStatusCode.InternalServerError,
            """{"detail":"dependency failed","status":0}""",
            "application/problem+json"));

        var chunks = await CollectAsync(client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("https://example.com/cqrs"));

        chunks[0].Problem!.StatusCode.ShouldBe(500);
        chunks[0].Problem!.Title.ShouldBe("Internal Server Error");
        chunks[0].Problem!.Type.ShouldBe("InternalServerError");
        chunks[0].Problem!.Detail.ShouldBe("dependency failed");
    }

    [Fact]
    public async Task ProblemJsonWithItsOwnTypeAndTitle_IsPreserved()
    {
        using var client = Client(StubHttpMessageHandler.RespondingWith(
            HttpStatusCode.InternalServerError,
            """{"type":"https://errors.example/cqrs","title":"service_unavailable","status":500,"detail":"custom issue"}""",
            "application/problem+json"));

        var chunks = await CollectAsync(client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("https://example.com/cqrs"));

        chunks[0].Problem!.Type.ShouldBe("https://errors.example/cqrs");
        chunks[0].Problem!.Title.ShouldBe("service_unavailable");
        chunks[0].Problem!.Detail.ShouldBe("custom issue");
    }

    [Fact]
    public async Task PlainTextErrorResponse_BecomesATerminalFailedChunk()
    {
        using var client = Client(StubHttpMessageHandler.RespondingWith(
            HttpStatusCode.BadGateway, "upstream exploded", "text/plain"));

        var chunks = await CollectAsync(client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("https://example.com/cqrs"));

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[0].Problem!.StatusCode.ShouldBe(502);
        chunks[0].Problem!.Detail.ShouldBe("upstream exploded");
    }

    [Fact]
    public async Task EmptyErrorResponse_StillDescribesTheStatusCode()
    {
        using var client = Client(StubHttpMessageHandler.RespondingWith(
            HttpStatusCode.NotFound, string.Empty, "text/plain"));

        var chunks = await CollectAsync(client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("https://example.com/cqrs"));

        chunks[0].Problem!.StatusCode.ShouldBe(404);
        chunks[0].Problem!.Detail.ShouldNotBeNull();
        chunks[0].Problem!.Detail!.ShouldContain("Request returned");
    }

    [Fact]
    public async Task LiteralNullErrorBody_FallsBackToTheRawBody()
    {
        using var client = Client(StubHttpMessageHandler.RespondingWith(
            HttpStatusCode.InternalServerError, "null", "application/problem+json"));

        var chunks = await CollectAsync(client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("https://example.com/cqrs"));

        chunks[0].Problem!.StatusCode.ShouldBe(500);
        chunks[0].Problem!.Detail.ShouldBe("null");
    }

    [Fact]
    public async Task ANetworkFailureBecomesATerminalFailedChunkRatherThanThrowing()
    {
        using var client = Client(new StubHttpMessageHandler(
            (_, _) => throw new HttpRequestException("connection refused")));

        var chunks = await CollectAsync(client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("https://example.com/cqrs"));

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[0].Problem!.Title.ShouldBe(nameof(HttpRequestException));
        chunks[0].Problem!.Detail.ShouldBe("connection refused");
    }

    // ---------- malformed frames ----------

    [Fact]
    public async Task MalformedFrame_BecomesATerminalFailedChunkByDefault()
    {
        using var client = Client(StubHttpMessageHandler.RespondingWithRawSse(
            "event: cqrs-started\ndata: {invalid-json\n\n"));

        var chunks = await CollectAsync(client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("https://example.com/cqrs"));

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[0].Problem!.Title.ShouldBe(CqrsStreamProblems.MalformedChunk);
    }

    [Fact]
    public async Task NullFramePayload_BecomesATerminalFailedChunkByDefault()
    {
        using var client = Client(StubHttpMessageHandler.RespondingWithRawSse("data: null\n\n"));

        var chunks = await CollectAsync(client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("https://example.com/cqrs"));

        chunks.Count.ShouldBe(1);
        chunks[0].Problem!.Title.ShouldBe(CqrsStreamProblems.MalformedChunk);
    }

    [Fact]
    public async Task MalformedFrameAfterValidOnes_KeepsWhatWasAlreadyDelivered()
    {
        var valid = StubHttpMessageHandler.BuildSsePayload([CqrsTestStreams.Started(sequence: 1)]);

        using var client = Client(StubHttpMessageHandler.RespondingWithRawSse(valid + "data: {invalid\n\n"));

        var chunks = await CollectAsync(client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("https://example.com/cqrs"));

        chunks.Count.ShouldBe(2);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunks[1].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[1].Problem!.Title.ShouldBe(CqrsStreamProblems.MalformedChunk);
    }

    [Fact]
    public async Task MalformedFrame_CanBeSkipped()
    {
        var payload = "data: {invalid\n\n" +
                      StubHttpMessageHandler.BuildSsePayload([CqrsTestStreams.Completed(sequence: 1)]);

        using var client = Client(StubHttpMessageHandler.RespondingWithRawSse(payload));

        var chunks = await CollectAsync(client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>(
            "https://example.com/cqrs",
            new CqrsStreamClientOptions { MalformedChunkBehavior = CqrsMalformedChunkBehavior.Skip }));

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
    }

    [Fact]
    public async Task MalformedFrame_CanBeConfiguredToThrow()
    {
        using var client = Client(StubHttpMessageHandler.RespondingWithRawSse("data: {invalid\n\n"));

        await Should.ThrowAsync<JsonException>(async () =>
            await CollectAsync(client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>(
                "https://example.com/cqrs",
                new CqrsStreamClientOptions { MalformedChunkBehavior = CqrsMalformedChunkBehavior.Throw })));
    }

    [Fact]
    public async Task KeepAliveAndCommentFramesAreIgnored()
    {
        var payload = ": keep-alive\n\n" +
                      "data: \n\n" +
                      StubHttpMessageHandler.BuildSsePayload([
                          CqrsTestStreams.Started(sequence: 1),
                          CqrsTestStreams.Completed(sequence: 2)
                      ]);

        using var client = Client(StubHttpMessageHandler.RespondingWithRawSse(payload));

        var chunks = await CollectAsync(client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("https://example.com/cqrs"));

        chunks.Select(chunk => chunk.Kind).ShouldBe([CqrsStreamChunkKind.Started, CqrsStreamChunkKind.Completed]);
    }

    // ---------- terminal guarantee ----------

    [Fact]
    public async Task AStreamThatEndsWithoutATerminalChunkGetsOneAppended()
    {
        using var client = Client(StubHttpMessageHandler.RespondingWithSse(
            CqrsTestStreams.Started(sequence: 1),
            CqrsTestStreams.Progress("processing", 2)));

        var chunks = await CollectAsync(client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("https://example.com/cqrs"));

        chunks.Count.ShouldBe(3);
        chunks[^1].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[^1].Problem!.Title.ShouldBe(CqrsStreamProblems.IncompleteStream);
    }

    [Fact]
    public async Task TheTerminalGuaranteeCanBeDisabled()
    {
        using var client = Client(StubHttpMessageHandler.RespondingWithSse(CqrsTestStreams.Started(sequence: 1)));

        var chunks = await CollectAsync(client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>(
            "https://example.com/cqrs",
            new CqrsStreamClientOptions { EnsureTerminalChunk = false }));

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
    }

    [Fact]
    public async Task AnEmptyResponseBodyStillProducesATerminalChunk()
    {
        using var client = Client(StubHttpMessageHandler.RespondingWithRawSse(string.Empty));

        var chunks = await CollectAsync(client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("https://example.com/cqrs"));

        chunks.Count.ShouldBe(1);
        chunks[0].Problem!.Title.ShouldBe(CqrsStreamProblems.IncompleteStream);
    }

    [Fact]
    public async Task MissingSequenceNumbersAreFilledInByTheClient()
    {
        using var client = Client(StubHttpMessageHandler.RespondingWithSse(
            CqrsTestStreams.Started(),
            CqrsTestStreams.Progress("processing"),
            CqrsTestStreams.Completed()));

        var chunks = await CollectAsync(client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("https://example.com/cqrs"));

        chunks.Select(chunk => chunk.Sequence).ShouldBe([1L, 2L, 3L]);
    }

    // ---------- cancellation ----------

    [Fact]
    public async Task APreCancelledTokenThrowsRatherThanProducingAFailedChunk()
    {
        using var client = Client(new StubHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await CollectAsync(client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>(
                "https://example.com/cqrs", cancellation.Token)));
    }

    // ---------- argument validation ----------

    [Fact]
    public void NullArgumentsAreRejectedEagerly()
    {
        using var client = Client(StubHttpMessageHandler.RespondingWithSse(CqrsTestStreams.Completed(sequence: 1)));

        Should.Throw<ArgumentNullException>(() =>
            CqrsHttpClientExtensions.SendForCqrsStreamAsync<ProgressUpdate, FinalResult>(null!, () => new HttpRequestMessage()));
        Should.Throw<ArgumentNullException>(() =>
            client.SendForCqrsStreamAsync<ProgressUpdate, FinalResult>((Func<HttpRequestMessage>)null!));
        Should.Throw<ArgumentNullException>(() =>
            client.SendForCqrsStreamAsync<ProgressUpdate, FinalResult>(null!, "https://example.com"));
        Should.Throw<ArgumentException>(() =>
            client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>("   "));
        Should.Throw<ArgumentException>(() =>
            client.PostForCqrsStreamAsync<ProgressUpdate, FinalResult, SubmitCommand>(string.Empty, new SubmitCommand("x")));
    }

    private static HttpClient Client(StubHttpMessageHandler handler)
    {
        return new HttpClient(handler);
    }

    private static async Task<IReadOnlyList<Chunk>> CollectAsync(IAsyncEnumerable<Chunk> stream)
    {
        var chunks = new List<Chunk>();

        await foreach (var chunk in stream)
        {
            chunks.Add(chunk);
        }

        return chunks;
    }
}
