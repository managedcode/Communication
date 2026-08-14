using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.CQRS.Extensions.Http;
using Shouldly;
using Xunit;
using Facade = ManagedCode.Communication.AspNetCore.Extensions.Http.CqrsHttpClientExtensions;

namespace ManagedCode.Communication.Tests.CQRS;

/// <summary>
///     Every method on the monolithic-package facade must reach the real implementation. A forwarding typo here would
///     otherwise only show up in a consumer's application.
/// </summary>
public class CqrsFacadeHttpClientTests
{
    [Fact]
    public async Task GetForwardsToTheCoreImplementation()
    {
        var handler = Handler();
        using var client = new HttpClient(handler);

        (await CollectAsync(Facade.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>(client, "https://example.com/get")))
            .Count.ShouldBe(1);

        handler.LastRequest!.Method.ShouldBe(HttpMethod.Get);
        handler.LastRequest!.RequestUri!.ToString().ShouldBe("https://example.com/get");
    }

    [Fact]
    public async Task PostForwardsToTheCoreImplementation()
    {
        var handler = Handler();
        using var client = new HttpClient(handler);

        (await CollectAsync(Facade.PostForCqrsStreamAsync<ProgressUpdate, FinalResult>(client, "https://example.com/post")))
            .Count.ShouldBe(1);

        handler.LastRequest!.Method.ShouldBe(HttpMethod.Post);
    }

    [Fact]
    public async Task PostWithBodyForwardsToTheCoreImplementation()
    {
        var handler = Handler();
        using var client = new HttpClient(handler);

        await CollectAsync(Facade.PostForCqrsStreamAsync<ProgressUpdate, FinalResult, SubmitCommand>(
            client, "https://example.com/post-body", new SubmitCommand("payload")));

        handler.LastRequest!.Method.ShouldBe(HttpMethod.Post);
        handler.LastRequestBody.ShouldBe("""{"payload":"payload"}""");
    }

    [Fact]
    public async Task SendWithMethodForwardsToTheCoreImplementation()
    {
        var handler = Handler();
        using var client = new HttpClient(handler);

        await CollectAsync(Facade.SendForCqrsStreamAsync<ProgressUpdate, FinalResult>(
            client, HttpMethod.Put, "https://example.com/send"));

        handler.LastRequest!.Method.ShouldBe(HttpMethod.Put);
    }

    [Fact]
    public async Task SendWithBodyForwardsToTheCoreImplementation()
    {
        var handler = Handler();
        using var client = new HttpClient(handler);

        await CollectAsync(Facade.SendForCqrsStreamAsync<ProgressUpdate, FinalResult, SubmitCommand>(
            client, "https://example.com/send-body", HttpMethod.Patch, new SubmitCommand("payload")));

        handler.LastRequest!.Method.ShouldBe(HttpMethod.Patch);
        handler.LastRequestBody.ShouldBe("""{"payload":"payload"}""");
    }

    [Fact]
    public async Task SendWithRequestFactoryForwardsToTheCoreImplementation()
    {
        var handler = Handler();
        using var client = new HttpClient(handler);

        await CollectAsync(Facade.SendForCqrsStreamAsync<ProgressUpdate, FinalResult>(
            client, () => new HttpRequestMessage(HttpMethod.Delete, "https://example.com/factory")));

        handler.LastRequest!.Method.ShouldBe(HttpMethod.Delete);
    }

    [Fact]
    public async Task TheCancellationTokenOverloadsForwardTheirToken()
    {
        var handler = Handler();
        using var client = new HttpClient(handler);
        using var cancellation = new CancellationTokenSource();

        await CollectAsync(Facade.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>(
            client, "https://example.com/get", cancellation.Token));
        await CollectAsync(Facade.PostForCqrsStreamAsync<ProgressUpdate, FinalResult>(
            client, "https://example.com/post", cancellation.Token));
        await CollectAsync(Facade.SendForCqrsStreamAsync<ProgressUpdate, FinalResult>(
            client, HttpMethod.Head, "https://example.com/send", cancellation.Token));
        await CollectAsync(Facade.SendForCqrsStreamAsync<ProgressUpdate, FinalResult>(
            client, () => new HttpRequestMessage(HttpMethod.Get, "https://example.com/factory"), cancellation.Token));
        await CollectAsync(Facade.PostForCqrsStreamAsync<ProgressUpdate, FinalResult, SubmitCommand>(
            client, "https://example.com/post-body", new SubmitCommand("x"), cancellation.Token));
        await CollectAsync(Facade.SendForCqrsStreamAsync<ProgressUpdate, FinalResult, SubmitCommand>(
            client, "https://example.com/send-body", HttpMethod.Post, new SubmitCommand("x"), cancellation.Token));

        handler.RequestCount.ShouldBe(6);
    }

    [Fact]
    public async Task TheFacadeHonoursExplicitClientOptions()
    {
        using var client = new HttpClient(StubHttpMessageHandler.RespondingWithSse(CqrsTestStreams.Started(sequence: 1)));

        var chunks = await CollectAsync(Facade.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>(
            client,
            "https://example.com/get",
            new CqrsStreamClientOptions { EnsureTerminalChunk = false }));

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
    }

    private static StubHttpMessageHandler Handler()
    {
        return StubHttpMessageHandler.RespondingWithSse(CqrsTestStreams.Completed(sequence: 1));
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
