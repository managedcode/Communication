using System;
using System.Net;
using System.Text.Json;
using ManagedCode.Communication.CQRS;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.CQRS;

/// <summary>
///     The chunk contract itself: factories, guard rails, defaults, and the exact shape that goes on the wire.
/// </summary>
public class CqrsStreamChunkTests
{
    private static readonly JsonSerializerOptions Json = CqrsStreamSerialization.Default;

    [Fact]
    public void Started_UsesStartedKindAndEventType()
    {
        var before = DateTime.UtcNow;

        var chunk = Chunk.Started(message: "command accepted", sequence: 1);

        chunk.Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunk.EventType.ShouldBe(Chunk.StartedEventType);
        chunk.Message.ShouldBe("command accepted");
        chunk.Sequence.ShouldBe(1);
        chunk.IsProgress.ShouldBeTrue();
        chunk.IsTerminal.ShouldBeFalse();
        chunk.TimestampUtc.Kind.ShouldBe(DateTimeKind.Utc);
        chunk.TimestampUtc.ShouldBeGreaterThanOrEqualTo(before);
    }

    [Fact]
    public void Started_AllowsOmittingProgressPayload()
    {
        var chunk = Chunk.Started();

        chunk.ProgressResult.ShouldBeNull();
        chunk.TryGetProgress(out _).ShouldBeFalse();
    }

    [Fact]
    public void Progress_CarriesPayloadAndProgressEventType()
    {
        var progress = Result<ProgressUpdate>.Succeed(new ProgressUpdate("running"));

        var chunk = Chunk.Progress(progress, message: "step 1", sequence: 2);

        chunk.Kind.ShouldBe(CqrsStreamChunkKind.Progress);
        chunk.EventType.ShouldBe(Chunk.ProgressEventType);
        chunk.ProgressResult.ShouldBe(progress);
        chunk.IsTerminal.ShouldBeFalse();
        chunk.TryGetProgress(out var value).ShouldBeTrue();
        value.State.ShouldBe("running");
    }

    [Fact]
    public void Progress_FromBarePayload_WrapsInSuccessfulResult()
    {
        var chunk = Chunk.Progress(new ProgressUpdate("running"), "step 1", sequence: 7);

        chunk.Kind.ShouldBe(CqrsStreamChunkKind.Progress);
        chunk.Sequence.ShouldBe(7);
        chunk.ProgressResult!.Value.IsSuccess.ShouldBeTrue();
        chunk.TryGetProgress(out var value).ShouldBeTrue();
        value.State.ShouldBe("running");
    }

    [Fact]
    public void Completed_CarriesTerminalSuccess()
    {
        var result = Result<FinalResult>.Succeed(new FinalResult("ok"));

        var chunk = Chunk.Completed(result, message: "done");

        chunk.Kind.ShouldBe(CqrsStreamChunkKind.Completed);
        chunk.EventType.ShouldBe(Chunk.CompletedEventType);
        chunk.Final.ShouldBe(result);
        chunk.IsTerminal.ShouldBeTrue();
        chunk.IsCompleted.ShouldBeTrue();
        chunk.IsFailed.ShouldBeFalse();
        chunk.Problem.ShouldBeNull();
        chunk.TryGetResult(out var value).ShouldBeTrue();
        value.Status.ShouldBe("ok");
    }

    [Fact]
    public void Completed_FromBarePayload_WrapsInSuccessfulResult()
    {
        var chunk = Chunk.Completed(new FinalResult("ok"), "done", sequence: 9);

        chunk.Sequence.ShouldBe(9);
        chunk.TryGetResult(out var value).ShouldBeTrue();
        value.Status.ShouldBe("ok");
    }

    [Fact]
    public void Completed_RejectsFailedResult()
    {
        var failed = Result<FinalResult>.Fail("cannot complete");

        var exception = Should.Throw<ArgumentException>(() => Chunk.Completed(failed));
        exception.ParamName.ShouldBe("final");
    }

    [Fact]
    public void Failed_CarriesTerminalFailure()
    {
        var failed = Result<FinalResult>.Fail("failed", "boom", HttpStatusCode.InternalServerError);

        var chunk = Chunk.Failed(failed, message: "error");

        chunk.Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunk.EventType.ShouldBe(Chunk.FailedEventType);
        chunk.IsTerminal.ShouldBeTrue();
        chunk.IsFailed.ShouldBeTrue();
        chunk.IsCompleted.ShouldBeFalse();
        chunk.Message.ShouldBe("error");
        chunk.TryGetResult(out _).ShouldBeFalse();
        chunk.TryGetProblem(out var problem).ShouldBeTrue();
        problem.Title.ShouldBe("failed");
    }

    [Fact]
    public void Failed_RejectsSuccessfulResult()
    {
        var success = Result<FinalResult>.Succeed(new FinalResult("ok"));

        var exception = Should.Throw<ArgumentException>(() => Chunk.Failed(success));
        exception.ParamName.ShouldBe("final");
    }

    [Fact]
    public void Failed_FromProblem_PreservesProblem()
    {
        var problem = Problem.Create("bad", "No progress", 500);

        var chunk = Chunk.Failed(problem);

        chunk.Final!.Value.IsSuccess.ShouldBeFalse();
        chunk.Problem.ShouldNotBeNull();
        chunk.Problem!.Title.ShouldBe("bad");
        chunk.Problem!.Detail.ShouldBe("No progress");
    }

    [Fact]
    public void FromException_CapturesTypeAndMessage()
    {
        var chunk = Chunk.FromException(new InvalidOperationException("boom"), sequence: 4);

        chunk.Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunk.Sequence.ShouldBe(4);
        chunk.Problem!.Title.ShouldBe(nameof(InvalidOperationException));
        chunk.Problem!.Detail.ShouldBe("boom");
        chunk.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public void Failed_RejectsNullProblemOrException()
    {
        Should.Throw<ArgumentNullException>(() => Chunk.Failed((Problem)null!));
        Should.Throw<ArgumentNullException>(() => Chunk.FromException(null!));
    }

    [Fact]
    public void CustomEventType_OverridesKindDefault()
    {
        var chunk = Chunk.Started(eventType: "custom-event");

        chunk.EventType.ShouldBe("custom-event");
    }

    [Fact]
    public void BlankEventType_FallsBackToKindDefault()
    {
        var chunk = new Chunk(CqrsStreamChunkKind.Progress, eventType: "   ");

        chunk.EventType.ShouldBe(Chunk.ProgressEventType);
    }

    [Theory]
    [InlineData(CqrsStreamChunkKind.Started, Chunk.StartedEventType)]
    [InlineData(CqrsStreamChunkKind.Progress, Chunk.ProgressEventType)]
    [InlineData(CqrsStreamChunkKind.Completed, Chunk.CompletedEventType)]
    [InlineData(CqrsStreamChunkKind.Failed, Chunk.FailedEventType)]
    public void ResolveEventType_MapsEveryKind(CqrsStreamChunkKind kind, string expected)
    {
        Chunk.ResolveEventType(kind).ShouldBe(expected);
    }

    [Fact]
    public void TimestampUtc_NormalizesLocalAndUnspecifiedKinds()
    {
        var local = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Local);
        var unspecified = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Unspecified);

        var fromLocal = Chunk.Started(timestampUtc: local);
        var fromUnspecified = Chunk.Started(timestampUtc: unspecified);

        fromLocal.TimestampUtc.Kind.ShouldBe(DateTimeKind.Utc);
        fromLocal.TimestampUtc.ShouldBe(local.ToUniversalTime());
        fromUnspecified.TimestampUtc.Kind.ShouldBe(DateTimeKind.Utc);
        fromUnspecified.TimestampUtc.ShouldBe(DateTime.SpecifyKind(unspecified, DateTimeKind.Utc));
    }

    [Fact]
    public void WithExpression_ProducesAModifiedCopy()
    {
        var original = Chunk.Progress(new ProgressUpdate("running"), "step");

        var sequenced = original with { Sequence = 42 };

        sequenced.Sequence.ShouldBe(42);
        original.Sequence.ShouldBeNull();
        sequenced.EventType.ShouldBe(original.EventType);
        sequenced.Message.ShouldBe(original.Message);
        sequenced.TimestampUtc.ShouldBe(original.TimestampUtc);
    }

    [Fact]
    public void WithExpression_PreservesCustomEventType()
    {
        var original = Chunk.Progress(new ProgressUpdate("running")) with { EventType = "custom" };

        var copy = original with { Sequence = 3 };

        copy.EventType.ShouldBe("custom");
    }

    [Fact]
    public void Serialization_WritesKindAsStringAndOmitsComputedMembers()
    {
        var chunk = Chunk.Started(Result<ProgressUpdate>.Succeed(new ProgressUpdate("running")), sequence: 1);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(chunk, Json));
        var root = document.RootElement;

        root.GetProperty("kind").GetString().ShouldBe(nameof(CqrsStreamChunkKind.Started));
        root.GetProperty("sequence").GetInt64().ShouldBe(1);

        // Derived helpers are for consumers, not for the wire.
        root.TryGetProperty("isProgress", out _).ShouldBeFalse();
        root.TryGetProperty("isTerminal", out _).ShouldBeFalse();
        root.TryGetProperty("isCompleted", out _).ShouldBeFalse();
        root.TryGetProperty("isFailed", out _).ShouldBeFalse();
        root.TryGetProperty("problem", out _).ShouldBeFalse();

        // Members that carry nothing stay off the wire: an absent member reads back exactly like a null one.
        root.TryGetProperty("final", out _).ShouldBeFalse();
        root.TryGetProperty("message", out _).ShouldBeFalse();
        root.TryGetProperty("eventId", out _).ShouldBeFalse();

        // The default event name is implied by Kind — and the SSE transport writes it into the frame's own
        // event: field — so repeating it in the body is pure payload.
        root.TryGetProperty("eventType", out _).ShouldBeFalse();
    }

    [Fact]
    public void Serialization_KeepsACustomEventTypeOnTheWire()
    {
        var chunk = Chunk.Started(eventType: "order-placed", sequence: 1);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(chunk, Json));

        document.RootElement.GetProperty("eventType").GetString().ShouldBe("order-placed");
    }

    [Fact]
    public void Deserialization_RebuildsTheDefaultEventTypeWhenItWasOmitted()
    {
        var original = Chunk.Progress(new ProgressUpdate("running"), sequence: 2);
        var json = JsonSerializer.Serialize(original, Json);

        json.ShouldNotContain("eventType");
        JsonSerializer.Deserialize<Chunk>(json, Json)!.EventType.ShouldBe(Chunk.ProgressEventType);
    }

    [Fact]
    public void Serialization_RoundTripsEveryField()
    {
        var original = Chunk.Completed(
            Result<FinalResult>.Succeed(new FinalResult("ok")),
            message: "done",
            eventType: "custom-done",
            eventId: "evt-9",
            sequence: 12,
            timestampUtc: new DateTime(2026, 5, 4, 3, 2, 1, DateTimeKind.Utc));

        var restored = JsonSerializer.Deserialize<Chunk>(JsonSerializer.Serialize(original, Json), Json);

        restored.ShouldNotBeNull();
        restored.Kind.ShouldBe(CqrsStreamChunkKind.Completed);
        restored.Message.ShouldBe("done");
        restored.EventType.ShouldBe("custom-done");
        restored.EventId.ShouldBe("evt-9");
        restored.Sequence.ShouldBe(12);
        restored.TimestampUtc.ShouldBe(original.TimestampUtc);
        restored.TryGetResult(out var value).ShouldBeTrue();
        value.Status.ShouldBe("ok");
    }

    [Fact]
    public void Deserialization_AcceptsNumericKindFromOlderProducers()
    {
        const string payload = """
            {"kind":0,"progressResult":{"isSuccess":true,"value":{"state":"running"}},"message":"accepted","eventId":"evt-1","sequence":1}
            """;

        var chunk = JsonSerializer.Deserialize<Chunk>(payload, Json);

        chunk.ShouldNotBeNull();
        chunk.Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunk.Message.ShouldBe("accepted");
        chunk.EventId.ShouldBe("evt-1");
        chunk.Sequence.ShouldBe(1);
        chunk.TryGetProgress(out var progress).ShouldBeTrue();
        progress.State.ShouldBe("running");
    }

    [Fact]
    public void Deserialization_FallsBackToKindDefaultEventTypeWhenMissing()
    {
        var chunk = JsonSerializer.Deserialize<Chunk>("""{"kind":"Failed"}""", Json);

        chunk.ShouldNotBeNull();
        chunk.EventType.ShouldBe(Chunk.FailedEventType);
    }

    [Fact]
    public void KindValues_ArePinnedToTheWireContract()
    {
        // Reordering these silently breaks producers and consumers that were built against different versions.
        ((int)CqrsStreamChunkKind.Started).ShouldBe(0);
        ((int)CqrsStreamChunkKind.Progress).ShouldBe(1);
        ((int)CqrsStreamChunkKind.Completed).ShouldBe(2);
        ((int)CqrsStreamChunkKind.Failed).ShouldBe(3);
    }
}
