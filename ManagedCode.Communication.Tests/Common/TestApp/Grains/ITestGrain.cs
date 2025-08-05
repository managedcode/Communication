using System.Threading.Tasks;
using ManagedCode.Communication.Tests.Common.TestApp.Models;
using Orleans;

namespace ManagedCode.Communication.Tests.Common.TestApp.Grains;

public interface ITestGrain : IGrainWithIntegerKey
{
    Task<Result> TestResult();
    Task<Result<int>> TestResultInt();

    Task<Result> TestResultError();
    Task<Result<int>> TestResultIntError();

    ValueTask<Result> TestValueTaskResult();
    ValueTask<Result<string>> TestValueTaskResultString();
    ValueTask<Result<ComplexTestModel>> TestValueTaskResultComplexObject();

    ValueTask<Result> TestValueTaskResultError();
    ValueTask<Result<string>> TestValueTaskResultStringError();
    ValueTask<Result<ComplexTestModel>> TestValueTaskResultComplexObjectError();
}