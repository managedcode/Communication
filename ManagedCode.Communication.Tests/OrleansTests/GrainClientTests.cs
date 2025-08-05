using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Communication.Tests.Common.TestApp;
using ManagedCode.Communication.Tests.Common.TestApp.Grains;
using Xunit;
using Xunit.Abstractions;

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
            .Should()
            .Be(true);
        intResult.Value
            .Should()
            .Be(5);
    }

    [Fact]
    public async Task Result()
    {
        var intResult = await _application.Cluster
            .Client
            .GetGrain<ITestGrain>(0)
            .TestResult();
        intResult.IsSuccess
            .Should()
            .Be(true);
    }

    [Fact]
    public async Task IntResultError()
    {
        var intResult = await _application.Cluster
            .Client
            .GetGrain<ITestGrain>(0)
            .TestResultIntError();
        intResult.IsFailed
            .Should()
            .Be(true);
    }

    [Fact]
    public async Task ResultError()
    {
        var intResult = await _application.Cluster
            .Client
            .GetGrain<ITestGrain>(0)
            .TestResultError();
        intResult.IsFailed
            .Should()
            .Be(true);
    }

    [Fact]
    public async Task ValueTaskResult()
    {
        var result = await _application.Cluster
            .Client
            .GetGrain<ITestGrain>(0)
            .TestValueTaskResult();
        result.IsSuccess
            .Should()
            .Be(true);
    }

    [Fact]
    public async Task ValueTaskResultString()
    {
        var result = await _application.Cluster
            .Client
            .GetGrain<ITestGrain>(0)
            .TestValueTaskResultString();
        result.IsSuccess
            .Should()
            .Be(true);
        result.Value
            .Should()
            .Be("test");
    }

    [Fact]
    public async Task ValueTaskResultError()
    {
        var result = await _application.Cluster
            .Client
            .GetGrain<ITestGrain>(0)
            .TestValueTaskResultError();
        result.IsFailed
            .Should()
            .Be(true);
    }

    [Fact]
    public async Task ValueTaskResultStringError()
    {
        var result = await _application.Cluster
            .Client
            .GetGrain<ITestGrain>(0)
            .TestValueTaskResultStringError();
        result.IsFailed
            .Should()
            .Be(true);
    }

    [Fact]
    public async Task ValueTaskResultComplexObject()
    {
        var result = await _application.Cluster
            .Client
            .GetGrain<ITestGrain>(0)
            .TestValueTaskResultComplexObject();
        result.IsSuccess
            .Should()
            .Be(true);
        result.Value
            .Should()
            .NotBeNull();
        result.Value!.Id
            .Should()
            .Be(123);
        result.Value
            .Name
            .Should()
            .Be("Test Model");
        result.Value
            .Tags
            .Should()
            .HaveCount(3);
        result.Value
            .Properties
            .Should()
            .HaveCount(3);
        result.Value
            .Nested
            .Should()
            .NotBeNull();
        result.Value.Nested!.Value
            .Should()
            .Be("nested value");
        result.Value
            .Nested
            .Score
            .Should()
            .Be(95.5);
    }

    [Fact]
    public async Task ValueTaskResultComplexObjectError()
    {
        var result = await _application.Cluster
            .Client
            .GetGrain<ITestGrain>(0)
            .TestValueTaskResultComplexObjectError();
        result.IsFailed
            .Should()
            .Be(true);
    }
}