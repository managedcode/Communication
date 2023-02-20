using System;
using System.Net;
using System.Threading.Tasks;
using ManagedCode.Communication.Tests.TestClusterApp.Grains.Abstractions;
using Orleans;

namespace ManagedCode.Communication.Tests.TestClusterApp.Grains;

public class TestGrain : Grain, ITestGrain
{
    public async ValueTask<Result> GetFailedResult()
    {
        await Task.Delay(TimeSpan.FromSeconds(10));
        return Result.Fail(HttpStatusCode.Unauthorized);
    }
}