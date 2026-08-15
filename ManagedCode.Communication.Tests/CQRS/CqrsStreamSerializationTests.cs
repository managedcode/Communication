using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ManagedCode.Communication.CQRS;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.CQRS;

/// <summary>
///     Covers <see cref="CqrsStreamSerialization.WithPayloadContext" />: that a caller's source-generated
///     contracts are used, that the transport's own types keep working alongside them, and that the saving
///     it exists for is real.
/// </summary>
public class CqrsStreamSerializationTests
{
    private static readonly JsonSerializerOptions WithContext =
        CqrsStreamSerialization.WithPayloadContext(StreamPayloads.Default);

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
    public void ARejectedResolverIsReportedImmediately()
    {
        Should.Throw<ArgumentNullException>(() => CqrsStreamSerialization.WithPayloadContext(null!));
    }

    [Fact]
    public void TheReturnedOptionsAreFrozen()
    {
        // Shared between streams, so nothing may reconfigure them after the fact.
        WithContext.IsReadOnly.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => WithContext.PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower);
    }

    [Fact]
    public void ChunksCarryingAContextBackedPayloadRoundTrip()
    {
        var chunk = CqrsStreamChunk<Positional, Positional>.Progress(new Positional("tick 12345"), sequence: 7);

        var json = JsonSerializer.Serialize(chunk, WithContext);
        var read = JsonSerializer.Deserialize<CqrsStreamChunk<Positional, Positional>>(json, WithContext);

        read.ShouldNotBeNull();
        read.Kind.ShouldBe(CqrsStreamChunkKind.Progress);
        read.Sequence.ShouldBe(7);
        read.ProgressResult!.Value.IsSuccess.ShouldBeTrue();
        read.ProgressResult!.Value.Value!.State.ShouldBe("tick 12345");
    }

    [Fact]
    public void TerminalChunksAndProblemsSurviveAlongsideACallerContext()
    {
        // The context knows nothing about Problem or the chunk itself; the combined resolver has to cover them.
        var failed = CqrsStreamChunk<Positional, Positional>.Failed(
            Problem.Create("boom", "detail", 409), "it broke");

        var read = JsonSerializer.Deserialize<CqrsStreamChunk<Positional, Positional>>(
            JsonSerializer.Serialize(failed, WithContext), WithContext);

        read.ShouldNotBeNull();
        read.Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        read.Message.ShouldBe("it broke");
        read.Final!.Value.Problem!.StatusCode.ShouldBe(409);
        read.Final!.Value.Problem!.Title.ShouldBe("boom");
    }

    [Fact]
    public void TheWireFormatIsIdenticalWithAndWithoutAContext()
    {
        // A context is a performance choice, not a protocol change: one end may use it and the other not.
        var chunk = CqrsStreamChunk<Positional, Positional>.Progress(new Positional("tick 12345"), sequence: 7);

        JsonSerializer.Serialize(chunk, WithContext)
            .ShouldBe(JsonSerializer.Serialize(chunk, CqrsStreamSerialization.Default));
    }

    [Fact]
    public void APositionalRecordPayloadCostsLessThroughAContext()
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
            CqrsStreamChunk<Positional, Positional>.Progress(new Positional("tick 12345"), sequence: 7),
            CqrsStreamSerialization.Default));

        var reflection = BytesPerOperation(() => JsonSerializer.Deserialize<CqrsStreamChunk<Positional, Positional>>(
            bytes, CqrsStreamSerialization.Default));
        var sourceGenerated = BytesPerOperation(() => JsonSerializer.Deserialize<CqrsStreamChunk<Positional, Positional>>(
            bytes, WithContext));

        // Measured at 48 bytes per chunk. The payload saves more (96) when deserialized on its own, but inside
        // a chunk part of the cost is shared. Asserted below the measurement so this catches the saving
        // disappearing rather than drifting by a few bytes.
        (reflection - sourceGenerated).ShouldBeGreaterThan(24);
    }
}

/// <summary>
///     The shape this exists for: a constructor parameter sends System.Text.Json down its allocating path.
/// </summary>
public sealed record Positional(string State);

/// <summary>
///     Source-generated contracts for <see cref="Positional" />, standing in for what a caller would declare.
/// </summary>
[JsonSerializable(typeof(Positional))]
internal partial class StreamPayloads : JsonSerializerContext;
