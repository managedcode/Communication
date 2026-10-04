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
