using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ManagedCode.Communication.AspNetCore;
using ManagedCode.Communication.CQRS;
using Microsoft.AspNetCore.Http;
using Shouldly;
using AspNetResult = Microsoft.AspNetCore.Http.IResult;

namespace ManagedCode.Communication.Tests.CQRS;

/// <summary>
///     Detection rules for "is this return value a CQRS chunk stream?". Every response of every endpoint passes
///     through here, so both the positive and the negative answer matter.
/// </summary>
public class CqrsStreamResultFactoryTests
{
    private static readonly CqrsStreamServerOptions Options = CqrsStreamServerOptions.Default;

    [Test]
    public void Convert_NullValue_IsNotAStream()
    {
        CqrsStreamResultFactory.TryCreateServerSentEventsResult(null, Options, out var converted).ShouldBeFalse();
        converted.ShouldBeNull();
    }

    [Test]
    public void Convert_ChunkStream_ProducesAResult()
    {
        CqrsStreamResultFactory.TryCreateServerSentEventsResult(CqrsTestStreams.CompletedAsync(), Options, out var converted)
            .ShouldBeTrue();

        converted.ShouldNotBeNull();
    }

    [Test]
    public void ExplicitHttpResult_NullStream_ThrowsArgumentNullException()
    {
        IAsyncEnumerable<Chunk> updates = null!;

        Should.Throw<ArgumentNullException>(() => CqrsStreamHttpResults.ServerSentEvents(updates));
    }

    [Test]
    public void Convert_NonChunkAsyncEnumerable_IsNotAStream()
    {
        CqrsStreamResultFactory.TryCreateServerSentEventsResult(CqrsTestStreams.NonChunkAsync(), Options, out var converted)
            .ShouldBeFalse();

        converted.ShouldBeNull();
    }

    [Test]
    public void Convert_PlainObject_IsNotAStream()
    {
        CqrsStreamResultFactory.TryCreateServerSentEventsResult(new FinalResult("plain"), Options, out var converted)
            .ShouldBeFalse();

        converted.ShouldBeNull();
    }

    [Test]
    public void Convert_ExistingResult_IsLeftAloneAndDoesNotFillTheOutParameter()
    {
        var existing = TypedResults.Ok("already-converted");

        CqrsStreamResultFactory.TryCreateServerSentEventsResult(existing, Options, out var converted).ShouldBeFalse();

        // A Try* method that reports failure must not hand back a value.
        converted.ShouldBeNull();
    }

    [Test]
    public void Convert_TypeThatIsBothResultAndChunkStream_PrefersTheExistingResult()
    {
        CqrsStreamResultFactory
            .TryCreateServerSentEventsResult(new ResultThatIsAlsoAChunkStream(), Options, out var converted)
            .ShouldBeFalse();

        converted.ShouldBeNull();
    }

    [Test]
    public void Convert_TypeWithTwoChunkContracts_FailsLoudlyInsteadOfPickingOne()
    {
        var exception = Should.Throw<InvalidOperationException>(() =>
            CqrsStreamResultFactory.TryCreateServerSentEventsResult(new AmbiguousChunkStream(), Options, out _));

        exception.Message.ShouldContain("more than one CqrsStreamChunk contract");
    }

    [Test]
    public void Convert_ActionResultFlavour_WrapsTheStream()
    {
        CqrsStreamResultFactory
            .TryCreateServerSentEventsActionResult(CqrsTestStreams.CompletedAsync(), Options, out var converted)
            .ShouldBeTrue();

        converted.ShouldBeOfType<CqrsServerSentEventsActionResult>();
    }

    [Test]
    public void Convert_ActionResultFlavour_NonStreamIsNotConverted()
    {
        CqrsStreamResultFactory
            .TryCreateServerSentEventsActionResult(new FinalResult("plain"), Options, out var converted)
            .ShouldBeFalse();

        converted.ShouldBeNull();
    }

    [Test]
    public void Convert_RejectsNullOptions()
    {
        Should.Throw<ArgumentNullException>(() =>
            CqrsStreamResultFactory.TryCreateServerSentEventsResult(CqrsTestStreams.CompletedAsync(), null!, out _));
    }

    [Test]
    public void Convert_RepeatedCallsForTheSameTypeStayConsistent()
    {
        // The converter is cached per runtime type; a second call must behave exactly like the first.
        for (var i = 0; i < 3; i++)
        {
            CqrsStreamResultFactory.TryCreateServerSentEventsResult(CqrsTestStreams.CompletedAsync(), Options, out var stream)
                .ShouldBeTrue();
            stream.ShouldNotBeNull();

            CqrsStreamResultFactory.TryCreateServerSentEventsResult(CqrsTestStreams.NonChunkAsync(), Options, out var other)
                .ShouldBeFalse();
            other.ShouldBeNull();
        }
    }

    private sealed class ResultThatIsAlsoAChunkStream : AspNetResult, IAsyncEnumerable<Chunk>
    {
        public IAsyncEnumerator<Chunk> GetAsyncEnumerator(System.Threading.CancellationToken cancellationToken = default)
        {
            return CqrsTestStreams.CompletedAsync().GetAsyncEnumerator(cancellationToken);
        }

        public Task ExecuteAsync(HttpContext httpContext)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class AmbiguousChunkStream :
        IAsyncEnumerable<Chunk>,
        IAsyncEnumerable<CqrsStreamChunk<FinalResult, ProgressUpdate>>
    {
        public IAsyncEnumerator<Chunk> GetAsyncEnumerator(System.Threading.CancellationToken cancellationToken = default)
        {
            return CqrsTestStreams.CompletedAsync().GetAsyncEnumerator(cancellationToken);
        }

        IAsyncEnumerator<CqrsStreamChunk<FinalResult, ProgressUpdate>>
            IAsyncEnumerable<CqrsStreamChunk<FinalResult, ProgressUpdate>>.GetAsyncEnumerator(
                System.Threading.CancellationToken cancellationToken)
        {
            return Empty().GetAsyncEnumerator(cancellationToken);

            static async IAsyncEnumerable<CqrsStreamChunk<FinalResult, ProgressUpdate>> Empty()
            {
                await Task.Yield();
                yield break;
            }
        }
    }
}
