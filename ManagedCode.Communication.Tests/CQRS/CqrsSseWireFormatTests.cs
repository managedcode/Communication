using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.AspNetCore;
using ManagedCode.Communication.AspNetCore.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.CQRS;

/// <summary>
///     Asserts the bytes on the wire, not just what a typed client can reconstruct from them. The SSE
///     <c>event:</c> and <c>id:</c> fields are what browsers and <c>Last-Event-ID</c> reconnects rely on, and a
///     round-trip through a typed reader would hide a mistake in either of them.
/// </summary>
public class CqrsSseWireFormatTests
{
    [Fact]
    public async Task EachChunkBecomesAFrameWithItsKindAsTheEventName()
    {
        await using var app = await StartAsync("/wire/completed", () => CqrsTestStreams.CompletedAsync());
        using var response = await app.GetTestClient().GetAsync("/wire/completed");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/event-stream");

        var frames = await SseTestReader.ReadFramesAsync(response);

        frames.Select(frame => frame.EventType).ShouldBe([
            Chunk.StartedEventType,
            Chunk.ProgressEventType,
            Chunk.CompletedEventType
        ]);
    }

    [Fact]
    public async Task SequenceNumbersSuppliedByTheHandlerAreWrittenAsTheEventId()
    {
        await using var app = await StartAsync("/wire/completed", () => CqrsTestStreams.CompletedAsync());
        using var response = await app.GetTestClient().GetAsync("/wire/completed");

        var frames = await SseTestReader.ReadFramesAsync(response);

        frames.Select(frame => frame.Id).ShouldBe(["1", "2", "3"]);
    }

    [Fact]
    public async Task SequenceNumbersAreAssignedAndWrittenEvenWhenTheHandlerOmitsThem()
    {
        await using var app = await StartAsync("/wire/no-sequences", () => CqrsTestStreams.CompletedWithoutSequencesAsync());
        using var response = await app.GetTestClient().GetAsync("/wire/no-sequences");

        var frames = await SseTestReader.ReadFramesAsync(response);

        // Without this, a client can neither order chunks nor resume by Last-Event-ID.
        frames.Select(frame => frame.Id).ShouldBe(["1", "2", "3"]);
        frames.Select(frame => frame.DeserializeChunk().Sequence).ShouldBe([1L, 2L, 3L]);
    }

    [Fact]
    public async Task SequenceAssignmentCanBeTurnedOff()
    {
        await using var app = await StartAsync(
            "/wire/unsequenced",
            () => CqrsTestStreams.CompletedWithoutSequencesAsync(),
            new CqrsStreamServerOptions { AssignSequenceNumbers = false });

        using var response = await app.GetTestClient().GetAsync("/wire/unsequenced");

        var frames = await SseTestReader.ReadFramesAsync(response);

        frames.ShouldAllBe(frame => frame.Id == null);
        frames.ShouldAllBe(frame => frame.DeserializeChunk().Sequence == null);
    }

    [Fact]
    public async Task ACustomEventIdWinsOverTheSequenceNumber()
    {
        await using var app = await StartAsync("/wire/custom-id", () => Single(
            Chunk.Completed(Result<FinalResult>.Succeed(new FinalResult("ok")), eventId: "evt-custom", sequence: 7)));

        using var response = await app.GetTestClient().GetAsync("/wire/custom-id");

        var frames = await SseTestReader.ReadFramesAsync(response);

        frames.Count.ShouldBe(1);
        frames[0].Id.ShouldBe("evt-custom");
    }

    [Fact]
    public async Task ACustomEventTypeIsWrittenAsTheEventName()
    {
        await using var app = await StartAsync("/wire/custom-event", () => Single(
            Chunk.Completed(Result<FinalResult>.Succeed(new FinalResult("ok")), eventType: "order-placed", sequence: 1)));

        using var response = await app.GetTestClient().GetAsync("/wire/custom-event");

        var frames = await SseTestReader.ReadFramesAsync(response);

        frames[0].EventType.ShouldBe("order-placed");
    }

    [Fact]
    public async Task TheDataFieldCarriesTheWholeChunkAsJson()
    {
        await using var app = await StartAsync("/wire/completed", () => CqrsTestStreams.CompletedAsync());
        using var response = await app.GetTestClient().GetAsync("/wire/completed");

        var frames = await SseTestReader.ReadFramesAsync(response);
        var terminal = frames[^1].DeserializeChunk();

        terminal.Kind.ShouldBe(CqrsStreamChunkKind.Completed);
        terminal.EventType.ShouldBe(Chunk.CompletedEventType);
        terminal.TryGetResult(out var result).ShouldBeTrue();
        result.Status.ShouldBe("done");

        // Kind travels as a string so adding enum members never renumbers the existing ones.
        frames[^1].Data.ShouldContain("\"kind\":\"Completed\"");
    }

    [Fact]
    public async Task ASynthesizedTerminalChunkIsAlsoWrittenAsARealFrame()
    {
        await using var app = await StartAsync("/wire/no-terminal", () => CqrsTestStreams.WithoutTerminalChunkAsync());
        using var response = await app.GetTestClient().GetAsync("/wire/no-terminal");

        var frames = await SseTestReader.ReadFramesAsync(response);

        frames.Count.ShouldBe(3);
        frames[^1].EventType.ShouldBe(Chunk.FailedEventType);
        frames[^1].Id.ShouldBe("3");
        frames[^1].DeserializeChunk().Problem!.Title.ShouldBe(CqrsStreamProblems.IncompleteStream);
    }

    private static async IAsyncEnumerable<Chunk> Single(Chunk chunk)
    {
        await Task.Yield();
        yield return chunk;
    }

    private static Task<WebApplication> StartAsync(
        string route,
        Func<IAsyncEnumerable<Chunk>> handler,
        CqrsStreamServerOptions? options = null)
    {
        return CqrsTestHost.StartMinimalApiAsync(app =>
        {
            var builder = app.MapGet(route, () => handler());

            if (options is null)
            {
                builder.WithCommunicationCqrsResults();
            }
            else
            {
                builder.WithCommunicationCqrsResults(options);
            }
        });
    }
}
