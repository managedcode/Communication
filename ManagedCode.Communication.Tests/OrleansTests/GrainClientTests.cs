using System.Threading.Tasks;
using Shouldly;
using ManagedCode.Communication.Tests.Common.TestApp;
using ManagedCode.Communication.Tests.Common.TestApp.Grains;
using Xunit;
using Xunit.Abstractions;
using ManagedCode.Communication.Tests.TestHelpers;

namespace ManagedCode.Communication.Tests.OrleansTests;

[Collection(nameof(TestClusterApplication))]
public class GrainClientTests
{
    private readonly TestClusterApplication _application;
    private readonly ITestOutputHelper _outputHelper;

    public GrainClientTests(ITestOutputHelper outputHelper, TestClusterApplication application)
    {
        _outputHelper = outputHelper;
        _application = application;
    }

    [Fact]
    public async Task IntResult()
    {
        var intResult = await _application.Cluster
            .Client
            .GetGrain<ITestGrain>(0)
            .TestResultInt();
        intResult.IsSuccess
            .ShouldBe(true);
        intResult.Value
            .ShouldBe(5);
    }

    [Fact]
    public async Task Result()
    {
        var intResult = await _application.Cluster
            .Client
            .GetGrain<ITestGrain>(0)
            .TestResult();
        intResult.IsSuccess
            .ShouldBe(true);
    }

    [Fact]
    public async Task IntResultError()
    {
        var intResult = await _application.Cluster
            .Client
            .GetGrain<ITestGrain>(0)
            .TestResultIntError();
        intResult.IsFailed
            .ShouldBe(true);
    }

    [Fact]
    public async Task ResultError()
    {
        var intResult = await _application.Cluster
            .Client
            .GetGrain<ITestGrain>(0)
            .TestResultError();
        intResult.IsFailed
            .ShouldBe(true);
    }

    [Fact]
    public async Task ValueTaskResult()
    {
        var result = await _application.Cluster
            .Client
            .GetGrain<ITestGrain>(0)
            .TestValueTaskResult();
        result.IsSuccess
            .ShouldBe(true);
    }

    [Fact]
    public async Task ValueTaskResultString()
    {
        var result = await _application.Cluster
            .Client
            .GetGrain<ITestGrain>(0)
            .TestValueTaskResultString();
        result.IsSuccess
            .ShouldBe(true);
        result.Value
            .ShouldBe("test");
    }

    [Fact]
    public async Task ValueTaskResultError()
    {
        var result = await _application.Cluster
            .Client
            .GetGrain<ITestGrain>(0)
            .TestValueTaskResultError();
        result.IsFailed
            .ShouldBe(true);
    }

    [Fact]
    public async Task ValueTaskResultStringError()
    {
        var result = await _application.Cluster
            .Client
            .GetGrain<ITestGrain>(0)
            .TestValueTaskResultStringError();
        result.IsFailed
            .ShouldBe(true);
    }

    [Fact]
    public async Task ValueTaskResultComplexObject()
    {
        var result = await _application.Cluster
            .Client
            .GetGrain<ITestGrain>(0)
            .TestValueTaskResultComplexObject();
        result.IsSuccess
            .ShouldBe(true);
        result.Value
            .ShouldNotBeNull();
        result.Value!.Id
            .ShouldBe(123);
        result.Value
            .Name
            .ShouldBe("Test Model");
        result.Value
            .Tags
            .ShouldHaveCount(3);
        result.Value
            .Properties
            .ShouldHaveCount(3);
        result.Value
            .Nested
            .ShouldNotBeNull();
        result.Value.Nested!.Value
            .ShouldBe("nested value");
        result.Value
            .Nested
            .Score
            .ShouldBe(95.5);
    }

    [Fact]
    public async Task ValueTaskResultComplexObjectError()
    {
        var result = await _application.Cluster
            .Client
            .GetGrain<ITestGrain>(0)
            .TestValueTaskResultComplexObjectError();
        result.IsFailed
            .ShouldBe(true);
    }
}