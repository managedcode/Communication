using System.Threading.Tasks;
using Orleans;

namespace ManagedCode.Communication.Tests.TestApp.Grains;

public interface ITestGrain : IGrainWithIntegerKey
{
    Task<Result> TestResult();
    Task<Result<int>> TestResultInt();
    
    Task<Result> TestResultError();
    Task<Result<int>> TestResultIntError();
}