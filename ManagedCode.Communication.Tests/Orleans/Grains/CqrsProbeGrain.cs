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
    public async Task<bool> DrainOnGrainSchedulerAsync(bool collectOutcome)
    {
        var scheduler = TaskScheduler.Current;
        var preserved = scheduler != TaskScheduler.Default;
        var stream = CheckContextAsync();
        if (collectOutcome)
        {
            await stream.ToOutcomeAsync();
        }
        else
        {
            await stream.ToResultAsync(async (_, _) =>
            {
                preserved &= TaskScheduler.Current == scheduler;
                await Task.Delay(10);
                preserved &= TaskScheduler.Current == scheduler;
            });
        }
        return preserved;

        async IAsyncEnumerable<CqrsStreamChunk<int, bool>> CheckContextAsync()
        {
            for (var index = 0; index < 3; index++)
            {
                preserved &= TaskScheduler.Current == scheduler;
                await Task.Delay(10);
                preserved &= TaskScheduler.Current == scheduler;
                yield return CqrsStreamChunk<int, bool>.Progress(index);
            }
            yield return CqrsStreamChunk<int, bool>.Completed(true);
        }
    }
}
