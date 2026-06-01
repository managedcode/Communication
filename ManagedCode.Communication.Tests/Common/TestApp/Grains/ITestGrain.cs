using System.Threading.Tasks;
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Tests.Common.TestApp.Models;
using Orleans;

namespace ManagedCode.Communication.Tests.Common.TestApp.Grains;

public interface ITestGrain : IGrainWithIntegerKey
{
    Task TestPlainTaskError();
    Task<int> TestPlainTaskIntError();

    Task<Result> TestResult();
    Task<Result<int>> TestResultInt();

    Task<Result> TestResultError();
    Task<Result<int>> TestResultIntError();
    Task<Result<int>> TestResultIntInvalidOperationError();
    Task<CollectionResult<int>> TestCollectionResultIntError();

    ValueTask<int> TestPlainValueTaskIntError();

    ValueTask<Result> TestValueTaskResult();
    ValueTask<Result<string>> TestValueTaskResultString();
    ValueTask<Result<ComplexTestModel>> TestValueTaskResultComplexObject();

    ValueTask<Result> TestValueTaskResultError();
    ValueTask<Result<string>> TestValueTaskResultStringError();
    ValueTask<Result<ComplexTestModel>> TestValueTaskResultComplexObjectError();
    ValueTask<CollectionResult<string>> TestValueTaskCollectionResultStringError();
    ValueTask<CollectionResult<string>> TestValueTaskCollectionResultStringUnauthorizedError();
}
