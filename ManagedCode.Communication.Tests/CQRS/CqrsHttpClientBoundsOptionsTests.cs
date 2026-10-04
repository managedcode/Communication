using System;
using System.Net.Http;
using ManagedCode.Communication.CQRS;
using Shouldly;

namespace ManagedCode.Communication.Tests.CQRS;

public class CqrsHttpClientBoundsOptionsTests
{
    private const string TestRequestUri = "https://example.test/cqrs";

    [Test]
    public void DefaultBounds_AreFiniteExceptForTheOptionalAggregateLimit()
    {
        CqrsStreamClientOptions.Default.MaximumFrameBytes.ShouldBe(16 * 1024 * 1024);
        CqrsStreamClientOptions.Default.MaximumFailureBodyBytes.ShouldBe(64 * 1024);
        CqrsStreamClientOptions.Default.MaximumStreamBytes.ShouldBeNull();
    }

    [Test]
    public void InvalidBounds_AreRejectedBeforeTheLazyRequestFactoryRuns()
    {
        using var client = new HttpClient();
        var factoryCalls = 0;
        Func<HttpRequestMessage> factory = () =>
        {
            factoryCalls++;
            return new HttpRequestMessage(HttpMethod.Get, TestRequestUri);
        };

        AssertRejected(client, new CqrsStreamClientOptions { MaximumFrameBytes = 0 }, factory);
        AssertRejected(client, new CqrsStreamClientOptions { MaximumFrameBytes = CqrsStreamClientOptions.MaximumFrameBytesLimit + 1 }, factory);
        AssertRejected(client, new CqrsStreamClientOptions { MaximumFailureBodyBytes = 0 }, factory);
        AssertRejected(client, new CqrsStreamClientOptions { MaximumFailureBodyBytes = CqrsStreamClientOptions.MaximumFailureBodyBytesLimit + 1 }, factory);
        AssertRejected(client, new CqrsStreamClientOptions { MaximumStreamBytes = 0 }, factory);
        AssertRejected(client, new CqrsStreamClientOptions { MaximumStreamBytes = -1 }, factory);
        AssertRejected(client, new CqrsStreamClientOptions { MalformedChunkBehavior = (CqrsMalformedChunkBehavior)99 }, factory);
        factoryCalls.ShouldBe(0);
    }

    private static void AssertRejected(HttpClient client, CqrsStreamClientOptions options, Func<HttpRequestMessage> factory)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => client.SendForCqrsStreamAsync<ProgressUpdate, FinalResult>(
            factory,
            options));
    }
}
