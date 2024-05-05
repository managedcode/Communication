using System.Threading.Tasks;
using Orleans;

namespace ManagedCode.Communication.Tests.TestApp.Grains;

public class TestGrain : Grain, ITestGrain
{
    public Task<Result> TestResult()
    {
        return Result.Succeed().AsTask();
    }

    public Task<Result<int>> TestResultInt()
    {
        return Result<int>.Succeed(5).AsTask();
    }

    public Task<Result> TestResultError()
    {
        throw new System.Exception("result error");
    }

    public Task<Result<int>> TestResultIntError()
    {
        throw new System.Exception("result int error");
    }
}