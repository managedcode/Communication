using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Shouldly;

namespace ManagedCode.Communication.Tests.CQRS;

public class CqrsHttpBoundsLifecycleIntegrationTests
{
    [Test]
    public async Task CancellationDuringBlockedFrameRead_SettlesAndAllowsAnotherRequest()
    {
        var responseStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var responseAborted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var app = await CqrsHttpBoundsTestSupport.StartAsync(async context =>
        {
            context.Response.ContentType = CqrsHttpBoundsTestSupport.EventStreamContentType;
            await context.Response.Body.WriteAsync(CqrsHttpBoundsTestSupport.IncompleteDataLine(), context.RequestAborted)
                .ConfigureAwait(false);
            await context.Response.Body.FlushAsync(context.RequestAborted).ConfigureAwait(false);
            responseStarted.TrySetResult(true);
            await CqrsHttpBoundsTestSupport.WaitForAbortAsync(context, responseAborted).ConfigureAwait(false);
        }, addHealthyRoute: true);
        using var client = app.GetTestClient();
        using var cancellation = new CancellationTokenSource();
        var enumerator = client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>(
                CqrsHttpBoundsTestSupport.StreamPath)
            .GetAsyncEnumerator(cancellation.Token);
        await CqrsHttpBoundsReadSettlement.RunCancelledMoveAndSettleAsync(
            enumerator,
            cancellation,
            responseStarted.Task,
            responseAborted.Task);

        var healthy = await CqrsHttpBoundsTestSupport.ReadAsync(
            client,
            CqrsHttpBoundsTestSupport.HealthyPath,
            CqrsStreamClientOptions.Default);
        healthy[^1].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
    }

    [Test]
    public async Task EarlyEnumeratorDisposal_AbortsAnOpenFrameResponse()
    {
        var responseStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var responseAborted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstFrame = CqrsHttpBoundsWireFactory.Frame(
            CqrsHttpBoundsTestSupport.NonTerminalPayload(),
            CqrsHttpBoundsTestSupport.NewLine((byte)'\n'));
        await using var app = await CqrsHttpBoundsTestSupport.StartAsync(async context =>
        {
            await CqrsHttpBoundsTestSupport.WriteBytesAsync(context, firstFrame).ConfigureAwait(false);
            responseStarted.TrySetResult(true);
            await CqrsHttpBoundsTestSupport.WaitForAbortAsync(context, responseAborted).ConfigureAwait(false);
        }, addHealthyRoute: true);
        using var client = app.GetTestClient();
        using var cancellation = new CancellationTokenSource();
        var enumerator = client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>(
                CqrsHttpBoundsTestSupport.StreamPath)
            .GetAsyncEnumerator(cancellation.Token);
        await CqrsHttpBoundsReadSettlement.RunEarlyDisposeAsync(
            enumerator,
            cancellation,
            responseStarted.Task,
            responseAborted.Task);

        var healthy = await CqrsHttpBoundsTestSupport.ReadAsync(
            client,
            CqrsHttpBoundsTestSupport.HealthyPath,
            CqrsStreamClientOptions.Default);
        healthy[^1].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
    }

    [Test]
    public async Task CancellationDuringBlockedFailureBodyRead_SettlesAndAllowsAnotherRequest()
    {
        var responseStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var responseAborted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var app = await CqrsHttpBoundsTestSupport.StartAsync(async context =>
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadGateway;
            context.Response.ContentType = CqrsHttpBoundsTestSupport.PlainTextContentType;
            context.Response.ContentLength = null;
            await context.Response.Body.FlushAsync(context.RequestAborted).ConfigureAwait(false);
            responseStarted.TrySetResult(true);
            await CqrsHttpBoundsTestSupport.WaitForAbortAsync(context, responseAborted).ConfigureAwait(false);
        }, addHealthyRoute: true);
        using var client = app.GetTestClient();
        using var cancellation = new CancellationTokenSource();
        var enumerator = client.GetForCqrsStreamAsync<ProgressUpdate, FinalResult>(
                CqrsHttpBoundsTestSupport.StreamPath)
            .GetAsyncEnumerator(cancellation.Token);
        await CqrsHttpBoundsReadSettlement.RunCancelledMoveAndSettleAsync(
            enumerator,
            cancellation,
            responseStarted.Task,
            responseAborted.Task);

        var healthy = await CqrsHttpBoundsTestSupport.ReadAsync(
            client,
            CqrsHttpBoundsTestSupport.HealthyPath,
            CqrsStreamClientOptions.Default);
        healthy[^1].Kind.ShouldBe(CqrsStreamChunkKind.Completed);
    }
}
