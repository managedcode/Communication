using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;
using Microsoft.AspNetCore.SignalR;

namespace ManagedCode.Communication.Tests.Common.TestApp.Controllers;

public class TestHub : Hub
{
    public Task<Result<int>> DoTest()
    {
        return Result.Succeed(5)
            .AsTask();
    }

    public Task<Result<int>> Throw()
    {
        throw new InvalidDataException("InvalidDataException");
    }

    /// <summary>A hub method streaming CQRS chunks, normalized so it honours the same contract as SSE.</summary>
    public IAsyncEnumerable<CqrsStreamChunk<HubProgress, HubReport>> StreamCommand(CancellationToken cancellationToken)
    {
        return CqrsStream.Normalize(WellFormedAsync(), cancellationToken: cancellationToken);
    }

    /// <summary>A handler that forgets its terminal chunk; normalization must supply one.</summary>
    public IAsyncEnumerable<CqrsStreamChunk<HubProgress, HubReport>> StreamWithoutTerminal(CancellationToken cancellationToken)
    {
        return CqrsStream.Normalize(NoTerminalAsync(), cancellationToken: cancellationToken);
    }

    /// <summary>A handler that throws mid-stream; normalization must turn it into a terminal failure.</summary>
    public IAsyncEnumerable<CqrsStreamChunk<HubProgress, HubReport>> StreamThatThrows(CancellationToken cancellationToken)
    {
        return CqrsStream.Normalize(ThrowingAsync(), cancellationToken: cancellationToken);
    }

    /// <summary>The push-style authoring helper, used straight from a hub method.</summary>
    public IAsyncEnumerable<CqrsStreamChunk<HubProgress, HubReport>> StreamViaWriter(CancellationToken cancellationToken)
    {
        return CqrsStream.Create<HubProgress, HubReport>(async writer =>
        {
            await writer.StartedAsync(new HubProgress("started"));
            await writer.ProgressAsync(new HubProgress("half"));
            return Result<HubReport>.Succeed(new HubReport("done"));
        }, cancellationToken);
    }

    private static async IAsyncEnumerable<CqrsStreamChunk<HubProgress, HubReport>> WellFormedAsync()
    {
        yield return CqrsStreamChunk<HubProgress, HubReport>.Started(new HubProgress("started"));
        await Task.Yield();
        yield return CqrsStreamChunk<HubProgress, HubReport>.Progress(new HubProgress("half"));
        await Task.Yield();
        yield return CqrsStreamChunk<HubProgress, HubReport>.Completed(new HubReport("done"));
    }

    private static async IAsyncEnumerable<CqrsStreamChunk<HubProgress, HubReport>> NoTerminalAsync()
    {
        yield return CqrsStreamChunk<HubProgress, HubReport>.Started(new HubProgress("started"));
        await Task.Yield();
        yield return CqrsStreamChunk<HubProgress, HubReport>.Progress(new HubProgress("half"));
    }

    private static async IAsyncEnumerable<CqrsStreamChunk<HubProgress, HubReport>> ThrowingAsync()
    {
        yield return CqrsStreamChunk<HubProgress, HubReport>.Started(new HubProgress("started"));
        await Task.Yield();
        throw new InvalidOperationException("hub command exploded");
    }
}

public sealed record HubProgress(string State);

public sealed record HubReport(string Status);
