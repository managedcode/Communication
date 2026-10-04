using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;
using Microsoft.AspNetCore.TestHost;
using Shouldly;

namespace ManagedCode.Communication.Tests.CQRS;

public class CqrsHttpFrameBoundsIntegrationTests
{
    private const string TestServerBaseAddress = "http://localhost";

    [Test]
    public async Task PhysicalFrameLimit_IsInclusiveAndExclusiveAcrossLineEndings()
    {
        var payload = CqrsHttpBoundsTestSupport.Payload();
        var endings = new[]
        {
            CqrsHttpBoundsTestSupport.NewLine((byte)'\n'),
            CqrsHttpBoundsTestSupport.CrLf,
            CqrsHttpBoundsTestSupport.NewLine((byte)'\r')
        };

        foreach (var ending in endings)
        {
            var frame = CqrsHttpBoundsWireFactory.Frame(
                payload,
                ending,
                includeBom: true,
                includeComment: true,
                multiline: true);
            await using var app = await CqrsHttpBoundsTestSupport.StartAsync(
                context => CqrsHttpBoundsTestSupport.WriteBytesAsync(context, frame, splitEveryByte: true));
            using var client = app.GetTestClient();

            var exactChunks = await CqrsHttpBoundsTestSupport.ReadAsync(
                client,
                CqrsHttpBoundsTestSupport.StreamPath,
                new CqrsStreamClientOptions { MaximumFrameBytes = frame.Length });

            exactChunks.Count.ShouldBe(1);
            exactChunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Completed);

            var excessChunks = await CqrsHttpBoundsTestSupport.ReadAsync(
                client,
                CqrsHttpBoundsTestSupport.StreamPath,
                new CqrsStreamClientOptions
                {
                    MaximumFrameBytes = frame.Length - 1,
                    MalformedChunkBehavior = CqrsMalformedChunkBehavior.Skip
                });
            excessChunks.Count.ShouldBe(1);
            excessChunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
            excessChunks[0].Problem!.Title.ShouldBe(CqrsStreamProblems.FrameLimitExceeded);
            excessChunks[0].Problem!.Detail.ShouldBe(CqrsStreamProblems.FrameLimitDetail);
        }
    }

    [Test]
    public async Task CrLfBytesReadSeparately_AreBothIncludedInThePhysicalLimit()
    {
        var frame = CqrsHttpBoundsWireFactory.Frame(
            CqrsHttpBoundsTestSupport.Payload(),
            CqrsHttpBoundsTestSupport.CrLf);
        var observations = new List<CqrsReadObservation>();
        await using var app = await CqrsHttpBoundsTestSupport.StartAsync(
            context => CqrsHttpBoundsTestSupport.WriteBytesAsync(context, frame));
        using var handler = new CqrsHttpFragmentingHandler(app.GetTestServer().CreateHandler(), observations);
        using var client = new HttpClient(handler) { BaseAddress = new Uri(TestServerBaseAddress) };

        var chunks = await CqrsHttpBoundsTestSupport.ReadAsync(
            client,
            CqrsHttpBoundsTestSupport.StreamPath,
            new CqrsStreamClientOptions
            {
                MaximumFrameBytes = frame.Length - 1,
                MalformedChunkBehavior = CqrsMalformedChunkBehavior.Skip
            });

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[0].Problem!.Title.ShouldBe(CqrsStreamProblems.FrameLimitExceeded);
        observations.Count.ShouldBe(frame.Length);
        observations[^4].Value.ShouldBe((byte)'\r');
        observations[^3].Value.ShouldBe((byte)'\n');
        observations[^2].Value.ShouldBe((byte)'\r');
        observations[^1].Value.ShouldBe((byte)'\n');
        observations[^4].Ordinal.ShouldBeLessThan(observations[^3].Ordinal);
        observations[^2].Ordinal.ShouldBeLessThan(observations[^1].Ordinal);
        for (var index = 0; index < frame.Length; index++)
        {
            observations[index].Ordinal.ShouldBe(index + 1);
            observations[index].Value.ShouldBe(frame[index]);
        }
    }

    [Test]
    public async Task WithinLimitPartialFrameAtEndOfFile_PreservesNativeIncompleteFailure()
    {
        var frame = CqrsHttpBoundsWireFactory.FrameWithoutFinalDelimiter(
            CqrsHttpBoundsTestSupport.Payload(),
            CqrsHttpBoundsTestSupport.NewLine((byte)'\n'));
        await using var app = await CqrsHttpBoundsTestSupport.StartAsync(
            context => CqrsHttpBoundsTestSupport.WriteBytesAsync(context, frame));
        using var client = app.GetTestClient();

        var chunks = await CqrsHttpBoundsTestSupport.ReadAsync(
            client,
            CqrsHttpBoundsTestSupport.StreamPath,
            new CqrsStreamClientOptions { MaximumFrameBytes = frame.Length });

        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        chunks[0].Problem!.Title.ShouldBe(CqrsStreamProblems.IncompleteStream);
        chunks[0].Problem!.Detail.ShouldBe(CqrsStreamProblems.IncompleteDetail);
    }

    [Test]
    public async Task FrameAndAggregateLimits_AreTerminalEvenWhenMalformedFramesAreSkipped()
    {
        var frame = CqrsHttpBoundsWireFactory.Frame(
            CqrsHttpBoundsTestSupport.Payload(),
            CqrsHttpBoundsTestSupport.CrLf,
            includeComment: true);
        var wire = CqrsHttpBoundsWireFactory.Join(
            CqrsHttpBoundsWireFactory.Heartbeat(CqrsHttpBoundsTestSupport.CrLf),
            frame);
        await using var app = await CqrsHttpBoundsTestSupport.StartAsync(
            context => CqrsHttpBoundsTestSupport.WriteBytesAsync(context, wire));
        using var client = app.GetTestClient();

        await AssertExactTotalAsync(client, frame, wire);
        await AssertFrameLimitAsync(client, frame);
        await AssertTotalLimitAsync(client, frame, wire);
    }

    private static async Task AssertExactTotalAsync(HttpClient client, byte[] frame, byte[] wire)
    {
        var chunks = await CqrsHttpBoundsTestSupport.ReadAsync(
            client,
            CqrsHttpBoundsTestSupport.StreamPath,
            new CqrsStreamClientOptions
            {
                MaximumFrameBytes = frame.Length,
                MaximumStreamBytes = wire.Length
            });
        chunks.Count.ShouldBe(1);
        chunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
    }

    private static async Task AssertFrameLimitAsync(HttpClient client, byte[] frame)
    {
        var frameChunks = await CqrsHttpBoundsTestSupport.ReadAsync(
            client,
            CqrsHttpBoundsTestSupport.StreamPath,
            new CqrsStreamClientOptions
            {
                MaximumFrameBytes = frame.Length - 1,
                MalformedChunkBehavior = CqrsMalformedChunkBehavior.Skip
            });
        frameChunks.Count.ShouldBe(1);
        frameChunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        frameChunks[0].Problem!.Title.ShouldBe(CqrsStreamProblems.FrameLimitExceeded);
        frameChunks[0].Problem!.Detail.ShouldBe(CqrsStreamProblems.FrameLimitDetail);
    }

    private static async Task AssertTotalLimitAsync(HttpClient client, byte[] frame, byte[] wire)
    {
        var totalChunks = await CqrsHttpBoundsTestSupport.ReadAsync(
            client,
            CqrsHttpBoundsTestSupport.StreamPath,
            new CqrsStreamClientOptions
            {
                MaximumFrameBytes = frame.Length,
                MaximumStreamBytes = wire.Length - 1,
                MalformedChunkBehavior = CqrsMalformedChunkBehavior.Skip
            });
        totalChunks.Count.ShouldBe(1);
        totalChunks[0].Kind.ShouldBe(CqrsStreamChunkKind.Failed);
        totalChunks[0].Problem!.Title.ShouldBe(CqrsStreamProblems.TotalLimitExceeded);
        totalChunks[0].Problem!.Detail.ShouldBe(CqrsStreamProblems.TotalLimitDetail);
    }

    [Test]
    public async Task UnterminatedOversizedFrame_CancelsWriterAndAllowsFollowingRequest()
    {
        var writerAborted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var app = await CqrsHttpBoundsTestSupport.StartAsync(async context =>
        {
            context.Response.ContentType = CqrsHttpBoundsTestSupport.EventStreamContentType;
            var partial = new byte[128];
            Array.Fill(partial, (byte)'x');
            await context.Response.Body.WriteAsync(partial, context.RequestAborted).ConfigureAwait(false);
            await context.Response.Body.FlushAsync(context.RequestAborted).ConfigureAwait(false);
            await CqrsHttpBoundsTestSupport.WaitForAbortAsync(context, writerAborted).ConfigureAwait(false);
        }, addHealthyRoute: true);
        using var client = app.GetTestClient();

        var chunks = await CqrsHttpBoundsTestSupport.ReadAsync(
            client,
            CqrsHttpBoundsTestSupport.StreamPath,
            new CqrsStreamClientOptions
            {
                MaximumFrameBytes = 32,
                MalformedChunkBehavior = CqrsMalformedChunkBehavior.Skip
            });

        chunks.Count.ShouldBe(1);
        chunks[0].Problem!.Title.ShouldBe(CqrsStreamProblems.FrameLimitExceeded);
        (await writerAborted.Task.WaitAsync(TimeSpan.FromSeconds(CqrsHttpBoundsTestSupport.CleanupTimeoutSeconds)))
            .ShouldBeTrue();

        var healthy = await CqrsHttpBoundsTestSupport.ReadAsync(
            client,
            CqrsHttpBoundsTestSupport.HealthyPath,
            CqrsStreamClientOptions.Default);
        healthy[^1].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
    }
}
