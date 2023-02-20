using System.Net;
using System.Threading.Tasks;
using ManagedCode.Communication.Tests.TestClusterApp.Grains.Abstractions;
using Orleans;
using Orleans.Serialization.Invocation;

namespace ManagedCode.Communication.Tests.TestClusterApp.Grains;

public class FilteredGrain : Grain, IFilteredGrain, IIncomingGrainCallFilter
{
    public ValueTask<Result<int>> GetNumber()
    {
        return Result<int>.Succeed(2).AsValueTask();
    }

    public Task Invoke(IIncomingGrainCallContext context)
    {
        context.Response = Response.FromResult(Result.Fail(HttpStatusCode.Unauthorized));
        return Task.CompletedTask;
    }
}