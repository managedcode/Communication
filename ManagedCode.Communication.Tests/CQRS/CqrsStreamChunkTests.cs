using System;
using System.Net;
using System.Text.Json;
using Shouldly;
using ManagedCode.Communication.CQRS;
using Xunit;

namespace ManagedCode.Communication.Tests.CQRS;

public class CqrsStreamChunkTests
{
    [Fact]
    public void StartedFactory_SetsStartDefaults()
    {
        var before = DateTime.UtcNow;
        var chunk = CqrsStreamChunk<string, int>.Started(
            message: "command accepted",
            sequence: 1);

        chunk.Kind.ShouldBe(CqrsStreamChunkKind.Started);
        chunk.IsTerminal.ShouldBeFalse();
        chunk.TimestampUtc.Kind.ShouldBe(DateTimeKind.Utc);
        chunk.TimestampUtc.ShouldBeGreaterThanOrEqualTo(before);
        chunk.EventType.ShouldBe("cqrs-started");
        chunk.Sequence.ShouldBe(1);
        chunk.Message.ShouldBe("command accepted");
    }

    [Fact]
    public void ProgressFactory_RequiresResult()
    {
        var progress = Result<string>.Succeed("running");
        var chunk = CqrsStreamChunk<string, string>.Progress(
            progress,
            message: "step 1",
            sequence: 2);

        chunk.Kind.ShouldBe(CqrsStreamChunkKind.Progress);
        chunk.ProgressResult.ShouldBe(progress);
        chunk.IsTerminal.ShouldBeFalse();
        chunk.Sequence.ShouldBe(2);
    }

    [Fact]
    public void CompletedFactory_AcceptsOnlySuccessfulFinal()
    {
        var result = Result<int>.Succeed(42);
        var chunk = CqrsStreamChunk<string, int>.Completed(result, message: "done");

        chunk.Kind.ShouldBe(CqrsStreamChunkKind.Completed);
        chunk.Final.ShouldBe(result);
        chunk.IsTerminal.ShouldBeTrue();
        chunk.EventType.ShouldBe("cqrs-completed");
    }

    [Fact]
    public void FailedFactory_RequiresFailure()
    {
        var failed = Result<int>.Fail("failed", "boom", HttpStatusCode.InternalServerError);
        var chunk = CqrsStreamChunk<string, int>.Failed(failed, message: "error");

        chunk.Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunk.Final.ShouldBe(failed);
        chunk.IsTerminal.ShouldBeTrue();
        chunk.EventType.ShouldBe("cqrs-failed");
        chunk.Message.ShouldBe("error");
    }

    [Fact]
    public void FailedFactoryFromProblem_PreservesProblem()
    {
        var problem = Problem.Create("bad", "No progress", 500);
        var chunk = CqrsStreamChunk<string, int>.Failed(problem);

        chunk.Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunk.Final.ShouldNotBeNull();
        chunk.Final!.Value.IsSuccess.ShouldBeFalse();
        chunk.Final!.Value.Problem.ShouldNotBeNull();
        chunk.Final!.Value.Problem!.Title.ShouldBe("bad");
    }

    [Fact]
    public void Deserialization_DefaultEventTypeFallbacksWhenMissing()
    {
        const string payload =
            "{\"kind\":0," +
            "\"progressResult\":{\"isSuccess\":true,\"value\":\"running\"}," +
            "\"message\":\"accepted\",\"eventId\":\"evt-1\",\"sequence\":1}";

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        var deserialized = JsonSerializer.Deserialize<CqrsStreamChunk<string, int>>(payload, options);
        deserialized.ShouldNotBeNull();
        deserialized.Kind.ShouldBe(CqrsStreamChunkKind.Started);
        deserialized.EventType.ShouldBe("cqrs-started");
        deserialized.Message.ShouldBe("accepted");
        deserialized.EventId.ShouldBe("evt-1");
        deserialized.Sequence.ShouldBe(1);
        deserialized.ProgressResult.ShouldNotBeNull();
        deserialized.ProgressResult!.Value.IsSuccess.ShouldBeTrue();
        deserialized.ProgressResult!.Value.Value.ShouldBe("running");
    }

    [Fact]
    public void CompletedFactory_ThrowsOnFailedResult()
    {
        var failed = Result<int>.Fail("cannot complete");

        Should.Throw<ArgumentException>(() =>
            CqrsStreamChunk<string, int>.Completed(failed, message: "done"));
    }

    [Fact]
    public void FailedFactory_ThrowsOnSuccessfulResult()
    {
        var success = Result<int>.Succeed(42);

        Should.Throw<ArgumentException>(() =>
            CqrsStreamChunk<string, int>.Failed(success, message: "oops"));
    }

    [Fact]
    public void StartedFactory_RespectsCustomEventType()
    {
        var chunk = CqrsStreamChunk<string, int>.Started(
            message: "custom event",
            eventType: "custom-event");

        chunk.EventType.ShouldBe("custom-event");
    }

    [Fact]
    public void ConstructorWithBlankEventTypeFallsBackToKindDefault()
    {
        var chunk = new CqrsStreamChunk<string, string>(
            CqrsStreamChunkKind.Started,
            Result<string>.Succeed("started"),
            null,
            "state",
            "   ",
            null,
            null,
            DateTime.UtcNow);

        chunk.EventType.ShouldBe("cqrs-started");
    }
}
