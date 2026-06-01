using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Tests.Common.TestApp.Models;
using Orleans;

namespace ManagedCode.Communication.Tests.Common.TestApp.Grains;

public class TestGrain : Grain, ITestGrain
{
    public Task TestPlainTaskError()
    {
        throw new InvalidOperationException("plain task error");
    }

    public Task<int> TestPlainTaskIntError()
    {
        throw new InvalidOperationException("plain task int error");
    }

    public Task<Result> TestResult()
    {
        return Result.Succeed()
            .AsTask();
    }

    public Task<Result<int>> TestResultInt()
    {
        return Result<int>.Succeed(5)
            .AsTask();
    }

    public Task<Result> TestResultError()
    {
        throw new Exception("result error");
    }

    public Task<Result<int>> TestResultIntError()
    {
        throw new Exception("result int error");
    }

    public Task<Result<int>> TestResultIntInvalidOperationError()
    {
        throw new InvalidOperationException("result int invalid operation error");
    }

    public Task<CollectionResult<int>> TestCollectionResultIntError()
    {
        throw new Exception("collection result int error");
    }

    public ValueTask<int> TestPlainValueTaskIntError()
    {
        throw new InvalidOperationException("plain valuetask int error");
    }

    public ValueTask<Result> TestValueTaskResult()
    {
        return ValueTask.FromResult(Result.Succeed());
    }

    public ValueTask<Result<string>> TestValueTaskResultString()
    {
        return ValueTask.FromResult(Result<string>.Succeed("test"));
    }

    public ValueTask<Result<ComplexTestModel>> TestValueTaskResultComplexObject()
    {
        var model = new ComplexTestModel
        {
            Id = 123,
            Name = "Test Model",
            CreatedAt = DateTime.UtcNow,
            Tags = new List<string> { "tag1", "tag2", "tag3" },
            Properties = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 42 },
                { "key3", true }
            },
            Nested = new NestedModel
            {
                Value = "nested value",
                Score = 95.5
            }
        };

        return ValueTask.FromResult(Result<ComplexTestModel>.Succeed(model));
    }

    public ValueTask<Result> TestValueTaskResultError()
    {
        throw new Exception("valuetask result error");
    }

    public ValueTask<Result<string>> TestValueTaskResultStringError()
    {
        throw new Exception("valuetask result string error");
    }

    public ValueTask<Result<ComplexTestModel>> TestValueTaskResultComplexObjectError()
    {
        throw new Exception("valuetask result complex object error");
    }

    public ValueTask<CollectionResult<string>> TestValueTaskCollectionResultStringError()
    {
        throw new Exception("valuetask collection result string error");
    }

    public ValueTask<CollectionResult<string>> TestValueTaskCollectionResultStringUnauthorizedError()
    {
        throw new UnauthorizedAccessException("valuetask collection result string unauthorized error");
    }
}
