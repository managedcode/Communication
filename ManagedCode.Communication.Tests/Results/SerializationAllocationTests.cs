using System;
using System.Text;
using System.Text.Json;
using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.Tests.CQRS;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.Results;

/// <summary>
///     Allocation budgets for the types this library serializes most.
/// </summary>
/// <remarks>
///     These assert on allocated bytes, not on time: allocation is deterministic, so the numbers are stable
///     across machines while timings are not. The budgets sit above the measured cost with room to spare — they
///     exist to catch a structural regression, not to police a few bytes. A serialization change that quietly
///     reintroduces the reflection path, or puts empty members back on the wire, blows straight through them.
/// </remarks>
public class SerializationAllocationTests
{
    private static readonly JsonSerializerOptions Json = CqrsStreamSerialization.Default;

    private static double BytesPerOperation(Action action, int iterations = 20_000)
    {
        for (var i = 0; i < 1_000; i++)
        {
            action();
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < iterations; i++)
        {
            action();
        }

        return (GC.GetAllocatedBytesForCurrentThread() - before) / (double)iterations;
    }

    [Fact]
    public void DeserializingAResultCostsLittleMoreThanItsPayload()
    {
        var payload = Encoding.UTF8.GetBytes("""{"state":"tick 12345"}""");
        var result = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(Result<ProgressUpdate>.Succeed(new ProgressUpdate("tick 12345")), Json));

        var payloadCost = BytesPerOperation(() => JsonSerializer.Deserialize<ProgressUpdate>(payload, Json));
        var resultCost = BytesPerOperation(() => JsonSerializer.Deserialize<Result<ProgressUpdate>>(result, Json));

        // Before Result<T> had its own converter this overhead was ~512 bytes: System.Text.Json fell back to a
        // reflection path because the struct mixes init-only members with a private [JsonInclude] field.
        (resultCost - payloadCost).ShouldBeLessThan(128);
    }

    [Fact]
    public void DeserializingAChunkStaysWithinBudget()
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(CqrsTestStreams.Progress("tick 12345", 12345), Json));

        BytesPerOperation(() => JsonSerializer.Deserialize<Chunk>(bytes, Json)).ShouldBeLessThan(512);
    }

    [Fact]
    public void DeserializingAChunkCostsLittleMoreThanBuildingOneByHand()
    {
        // A string payload, so that what is measured is the transport's own cost rather than whatever the
        // serializer spends on a caller's payload type. The chunk object and the string inside it have to be
        // allocated either way; the difference is what deserializing adds on top, measured at ~32 bytes.
        var chunk = CqrsStreamChunk<string, string>.Progress("tick 12345", sequence: 12345);
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(chunk, Json));

        var payloadOnly = BytesPerOperation(() => new string('x', 10));
        var byHand = BytesPerOperation(() => CqrsStreamChunk<string, string>.Progress("tick 12345", sequence: 12345));
        var deserialized = BytesPerOperation(() => JsonSerializer.Deserialize<CqrsStreamChunk<string, string>>(bytes, Json));

        // Building by hand reuses the string literal; deserializing has to materialise the string, so that
        // cost is charged to the hand-built baseline rather than counted as serializer overhead.
        (deserialized - byHand - payloadOnly).ShouldBeLessThan(96);
    }

    [Fact]
    public void SerializingAChunkStaysWithinBudget()
    {
        var chunk = CqrsTestStreams.Progress("tick 12345", 12345);

        BytesPerOperation(() => JsonSerializer.SerializeToUtf8Bytes(chunk, Json)).ShouldBeLessThan(320);
    }

    [Fact]
    public void AProgressChunkStaysCompactOnTheWire()
    {
        var chunk = CqrsTestStreams.Progress("tick 12345", 12345);

        var json = JsonSerializer.Serialize(chunk, Json);

        // Empty members and the implied event name used to add ~70 bytes to every frame of every stream.
        json.ShouldNotContain("\"final\"");
        json.ShouldNotContain("\"message\"");
        json.ShouldNotContain("\"eventId\"");
        json.ShouldNotContain("\"eventType\"");
        Encoding.UTF8.GetByteCount(json).ShouldBeLessThan(180);
    }

    [Fact]
    public void ResultsStillRoundTripThroughTheirConverters()
    {
        var success = JsonSerializer.Deserialize<Result<int>>(
            JsonSerializer.Serialize(Result<int>.Succeed(0), Json), Json);
        success.IsSuccess.ShouldBeTrue();
        success.Value.ShouldBe(0);

        var failure = JsonSerializer.Deserialize<Result<string>>(
            JsonSerializer.Serialize(Result<string>.Fail(Problem.Create("boom", "detail", 409)), Json), Json);
        failure.IsFailed.ShouldBeTrue();
        failure.Problem!.StatusCode.ShouldBe(409);
        failure.Problem!.Title.ShouldBe("boom");

        var plain = JsonSerializer.Deserialize<Result>(
            JsonSerializer.Serialize(Result.Fail(Problem.Create("nope", "d", 403)), Json), Json);
        plain.IsFailed.ShouldBeTrue();
        plain.Problem!.StatusCode.ShouldBe(403);

        // A failure is allowed to carry a value; the converter must not drop it. Result<int> is used because
        // Result<string>.Fail("x") binds to the overload that takes a title, not the one that takes a value.
        var failedWithValue = JsonSerializer.Deserialize<Result<int>>(
            JsonSerializer.Serialize(Result<int>.Fail(42), Json), Json);
        failedWithValue.IsFailed.ShouldBeTrue();
        failedWithValue.Value.ShouldBe(42);
    }

    [Fact]
    public void TheConvertersAcceptPascalCasePayloads()
    {
        var result = JsonSerializer.Deserialize<Result<int>>("""{"IsSuccess":true,"Value":7}""", Json);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(7);
    }

    [Fact]
    public void TheConvertersIgnoreMembersTheyDoNotKnow()
    {
        var result = JsonSerializer.Deserialize<Result<int>>(
            """{"isSuccess":true,"value":7,"somethingElse":{"nested":[1,2,3]}}""", Json);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(7);
    }
}
