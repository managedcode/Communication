using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;
using Shouldly;

namespace ManagedCode.Communication.Tests.CQRS;

public sealed class CqrsFatalExceptionTests
{
    private const string FatalDetail = "fatal sentinel";
    private const string StartedState = "started";
    private const string AggregateDetail = "ordinary aggregate detail";
    private const string AggregateInnerDetail = "ordinary aggregate inner detail";
    private const int DeepAggregateDepth = 10_000;

    [Test]
    public async Task CreatePropagatesOutOfMemorySentinelAfterStarted()
    {
        await AssertCreatePropagatesAfterStartedAsync(new OutOfMemoryException(FatalDetail));
    }

    [Test]
    public async Task CreatePropagatesStackOverflowSentinelAfterStarted()
    {
        await AssertCreatePropagatesAfterStartedAsync(new StackOverflowException(FatalDetail));
    }

    [Test]
    public async Task CreatePropagatesAccessViolationSentinelAfterStarted()
    {
        await AssertCreatePropagatesAfterStartedAsync(new AccessViolationException(FatalDetail));
    }

    [Test]
    public async Task CreatePropagatesFatalSentinelBeforeStarted()
    {
        await AssertCreatePropagatesAsync(new OutOfMemoryException(FatalDetail), emitStarted: false);
    }

    [Test]
    public async Task CreatePropagatesNativeAggregateFatalExceptionsAfterStarted()
    {
        foreach (var fatal in FatalSentinels())
        {
            await AssertCreatePropagatesAggregateAsync(new AggregateException(fatal), emitStarted: true);
            await AssertCreatePropagatesAggregateAsync(
                new AggregateException(new AggregateException(fatal)), emitStarted: true);
        }
    }

    [Test]
    public async Task CreatePropagatesFatalExceptionInDeepNativeAggregate()
    {
        var failure = WrapDeeply(new OutOfMemoryException(FatalDetail), DeepAggregateDepth);

        await AssertCreatePropagatesAggregateAsync(failure, emitStarted: false);
    }

    [Test]
    public async Task NormalizePropagatesNativeAggregateFatalExceptionsAndDisposesSource()
    {
        foreach (var fatal in FatalSentinels())
        {
            await AssertNormalizePropagatesAggregateAndDisposesAsync(new AggregateException(fatal), emitStarted: true);
            await AssertNormalizePropagatesAggregateAndDisposesAsync(
                new AggregateException(new AggregateException(fatal)), emitStarted: true);
        }
    }

    [Test]
    public async Task NormalizePropagatesFatalExceptionInDeepNativeAggregateAndDisposesSource()
    {
        var failure = WrapDeeply(new OutOfMemoryException(FatalDetail), DeepAggregateDepth);

        await AssertNormalizePropagatesAggregateAndDisposesAsync(failure, emitStarted: false);
    }

    [Test]
    public async Task OrdinaryNativeAggregatesStillBecomeFailedTerminals()
    {
        var failure = new AggregateException(AggregateDetail, new InvalidOperationException(AggregateInnerDetail));
        var createChunks = new List<Chunk>();
        await CollectAsync(CqrsStream.Create<ProgressUpdate, FinalResult>(
            writer => ThrowAggregateAsync(writer, failure, emitStarted: false)), createChunks);
        createChunks.Count.ShouldBe(1);
        createChunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        createChunks[0].Problem!.Title.ShouldBe(nameof(AggregateException));

        var source = new FatalSource(failure, emitStarted: false);
        var normalized = new List<Chunk>();
        await CollectAsync(CqrsStream.Normalize(source), normalized);
        source.Disposed.ShouldBeTrue();
        normalized.Count.ShouldBe(1);
        normalized[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        normalized[0].Problem!.Title.ShouldBe(nameof(AggregateException));
    }

    [Test]
    public async Task NormalizePropagatesOutOfMemorySentinelAndDisposesSource()
    {
        await AssertNormalizePropagatesAndDisposesAsync(new OutOfMemoryException(FatalDetail), emitStarted: true);
    }

    [Test]
    public async Task NormalizePropagatesStackOverflowSentinelAndDisposesSource()
    {
        await AssertNormalizePropagatesAndDisposesAsync(new StackOverflowException(FatalDetail), emitStarted: true);
    }

    [Test]
    public async Task NormalizePropagatesAccessViolationSentinelBeforeStarted()
    {
        await AssertNormalizePropagatesAndDisposesAsync(new AccessViolationException(FatalDetail), emitStarted: false);
    }

    private static async Task AssertCreatePropagatesAfterStartedAsync<TException>(TException expected)
        where TException : Exception
        => await AssertCreatePropagatesAsync(expected, emitStarted: true);

    private static async Task AssertCreatePropagatesAsync<TException>(TException expected, bool emitStarted)
        where TException : Exception
    {
        var observed = new List<Chunk>();
        var stream = CqrsStream.Create<ProgressUpdate, FinalResult>(
            writer => ThrowFatalAsync(writer, expected, emitStarted));

        var thrown = await Should.ThrowAsync<TException>(() => CollectAsync(stream, observed));

        thrown!.ShouldBeSameAs(expected);
        observed.ShouldAllBe(chunk => chunk.Kind != CqrsStreamChunkKind.Failed);
        observed.Count.ShouldBe(emitStarted ? 1 : 0);
        if (emitStarted)
        {
            observed[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        }
    }

    private static async ValueTask<Result<FinalResult>> ThrowFatalAsync<TException>(
        ICqrsStreamWriter<ProgressUpdate, FinalResult> writer,
        TException failure,
        bool emitStarted)
        where TException : Exception
    {
        if (emitStarted)
        {
            await writer.StartedAsync(new ProgressUpdate(StartedState));
        }

        throw failure;
    }

    private static async Task AssertCreatePropagatesAggregateAsync(AggregateException expected, bool emitStarted)
    {
        var observed = new List<Chunk>();
        var stream = CqrsStream.Create<ProgressUpdate, FinalResult>(
            writer => ThrowAggregateAsync(writer, expected, emitStarted));

        var thrown = await Should.ThrowAsync<AggregateException>(() => CollectAsync(stream, observed));

        thrown.ShouldBeSameAs(expected);
        observed.ShouldAllBe(chunk => chunk.Kind != CqrsStreamChunkKind.Failed);
        observed.Count.ShouldBe(emitStarted ? 1 : 0);
        if (emitStarted)
        {
            observed[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        }
    }

    private static async ValueTask<Result<FinalResult>> ThrowAggregateAsync(
        ICqrsStreamWriter<ProgressUpdate, FinalResult> writer, AggregateException failure, bool emitStarted)
    {
        if (emitStarted)
        {
            await writer.StartedAsync(new ProgressUpdate(StartedState));
        }

        throw failure;
    }

    private static async Task AssertNormalizePropagatesAggregateAndDisposesAsync(
        AggregateException expected, bool emitStarted)
    {
        var source = new FatalSource(expected, emitStarted);
        var observed = new List<Chunk>();
        var normalized = CqrsStream.Normalize(source);

        var thrown = await Should.ThrowAsync<AggregateException>(() => CollectAsync(normalized, observed));

        thrown.ShouldBeSameAs(expected);
        source.Disposed.ShouldBeTrue();
        observed.ShouldAllBe(chunk => chunk.Kind != CqrsStreamChunkKind.Failed);
        observed.Count.ShouldBe(emitStarted ? 1 : 0);
    }

    private static IEnumerable<Exception> FatalSentinels()
    {
        yield return new OutOfMemoryException(FatalDetail);
        yield return new StackOverflowException(FatalDetail);
        yield return new AccessViolationException(FatalDetail);
    }

    private static AggregateException WrapDeeply(Exception fatal, int depth)
    {
        AggregateException current = new(fatal);
        for (var index = 1; index < depth; index++)
        {
            current = new(current);
        }

        return current;
    }

    private static async Task AssertNormalizePropagatesAndDisposesAsync<TException>(
        TException expected,
        bool emitStarted)
        where TException : Exception
    {
        var source = new FatalSource(expected, emitStarted);
        var observed = new List<Chunk>();
        var normalized = CqrsStream.Normalize(source);

        var thrown = await Should.ThrowAsync<TException>(() => CollectAsync(normalized, observed));

        thrown!.ShouldBeSameAs(expected);
        source.Disposed.ShouldBeTrue();
        observed.ShouldAllBe(chunk => chunk.Kind != CqrsStreamChunkKind.Failed);
        observed.Count.ShouldBe(emitStarted ? 1 : 0);
        if (emitStarted)
        {
            observed[0].Kind.ShouldBe(CqrsStreamChunkKind.Started);
        }
    }

    private static async Task CollectAsync(IAsyncEnumerable<Chunk> stream, ICollection<Chunk> chunks)
    {
        await foreach (var chunk in stream)
        {
            chunks.Add(chunk);
        }
    }

    private sealed class FatalSource(Exception failure, bool emitStarted) : IAsyncEnumerable<Chunk>
    {
        public bool Disposed { get; private set; }

        public IAsyncEnumerator<Chunk> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new FatalEnumerator(this, failure, emitStarted);

        private sealed class FatalEnumerator(FatalSource owner, Exception failure, bool emitStarted)
            : IAsyncEnumerator<Chunk>
        {
            private bool started;

            public Chunk Current { get; private set; } = null!;

            public ValueTask<bool> MoveNextAsync()
            {
                if (emitStarted && !started)
                {
                    started = true;
                    Current = CqrsTestStreams.Started(StartedState);
                    return ValueTask.FromResult(true);
                }

                return ValueTask.FromException<bool>(failure);
            }

            public ValueTask DisposeAsync()
            {
                owner.Disposed = true;
                return ValueTask.CompletedTask;
            }
        }
    }
}
