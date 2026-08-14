using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;
using Orleans;

namespace ManagedCode.Communication.Tests.Orleans.Grains;

public class CqrsProbeGrain : Grain, ICqrsProbeGrain
{
    public Task<CqrsStreamChunk<OrleansProgress, OrleansReport>> EchoChunkAsync(
        CqrsStreamChunk<OrleansProgress, OrleansReport> chunk)
    {
        return Task.FromResult(chunk);
    }

    public async IAsyncEnumerable<CqrsStreamChunk<OrleansProgress, OrleansReport>> StreamAsync()
    {
        yield return CqrsStreamChunk<OrleansProgress, OrleansReport>.Started(
            Result<OrleansProgress>.Succeed(new OrleansProgress("started")), sequence: 1);

        await Task.Yield();
        yield return CqrsStreamChunk<OrleansProgress, OrleansReport>.Progress(
            new OrleansProgress("working"), sequence: 2);

        await Task.Yield();
        yield return CqrsStreamChunk<OrleansProgress, OrleansReport>.Completed(
            new OrleansReport("done"), sequence: 3);
    }
}
