using System;
using System.Net;
using System.Threading.Tasks;
using ManagedCode.Communication.Tests.Common.TestApp;
using ManagedCode.Communication.Tests.Common.TestApp.Grains;
using ManagedCode.Communication.Tests.TestHelpers;
using Shouldly;
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
    public async Task PlainTaskError()
    {
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await _application.Cluster
                .Client
                .GetGrain<ITestGrain>(0)
                .TestPlainTaskError();
        });

        exception.Message.ShouldContain("plain task error");
    }

    [Fact]
    public async Task PlainTaskIntError()
    {
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await _application.Cluster
                .Client
                .GetGrain<ITestGrain>(0)
                .TestPlainTaskIntError();
        });

        exception.Message.ShouldContain("plain task int error");
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
        intResult.ShouldHaveProblem()
            .WithTitle(nameof(Exception))
            .WithDetail("result int error")
            .WithStatusCode((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task IntResultInvalidOperationError()
    {
        var intResult = await _application.Cluster
            .Client
            .GetGrain<ITestGrain>(0)
            .TestResultIntInvalidOperationError();
        intResult.ShouldHaveProblem()
            .WithTitle(nameof(InvalidOperationException))
            .WithDetail("result int invalid operation error")
            .WithStatusCode((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CollectionResultIntError()
    {
        var result = await _application.Cluster
            .Client
            .GetGrain<ITestGrain>(0)
            .TestCollectionResultIntError();

        result.ShouldHaveProblem()
            .WithTitle(nameof(Exception))
            .WithDetail("collection result int error")
            .WithStatusCode((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ResultError()
    {
        var intResult = await _application.Cluster
            .Client
            .GetGrain<ITestGrain>(0)
            .TestResultError();
        intResult.ShouldHaveProblem()
            .WithTitle(nameof(Exception))
            .WithDetail("result error")
            .WithStatusCode((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task PlainValueTaskIntError()
    {
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await _application.Cluster
                .Client
                .GetGrain<ITestGrain>(0)
                .TestPlainValueTaskIntError();
        });

        exception.Message.ShouldContain("plain valuetask int error");
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
        result.ShouldHaveProblem()
            .WithTitle(nameof(Exception))
            .WithDetail("valuetask result error")
            .WithStatusCode((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ValueTaskResultStringError()
    {
        var result = await _application.Cluster
            .Client
            .GetGrain<ITestGrain>(0)
            .TestValueTaskResultStringError();
        result.ShouldHaveProblem()
            .WithTitle(nameof(Exception))
            .WithDetail("valuetask result string error")
            .WithStatusCode((int)HttpStatusCode.InternalServerError);
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
        result.ShouldHaveProblem()
            .WithTitle(nameof(Exception))
            .WithDetail("valuetask result complex object error")
            .WithStatusCode((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ValueTaskCollectionResultStringError()
    {
        var result = await _application.Cluster
            .Client
            .GetGrain<ITestGrain>(0)
            .TestValueTaskCollectionResultStringError();

        result.ShouldHaveProblem()
            .WithTitle(nameof(Exception))
            .WithDetail("valuetask collection result string error")
            .WithStatusCode((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ValueTaskCollectionResultStringUnauthorizedError()
    {
        var result = await _application.Cluster
            .Client
            .GetGrain<ITestGrain>(0)
            .TestValueTaskCollectionResultStringUnauthorizedError();

        result.ShouldHaveProblem()
            .WithTitle(nameof(UnauthorizedAccessException))
            .WithDetail("valuetask collection result string unauthorized error")
            .WithStatusCode((int)HttpStatusCode.Unauthorized);
    }
}
