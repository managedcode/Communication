using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Shouldly;
using ManagedCode.Communication.Tests.Common.TestApp;
using ManagedCode.Communication.Tests.Common.TestApp.Controllers;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;
using Xunit.Abstractions;

namespace ManagedCode.Communication.Tests.ControllerTests;

[Collection(nameof(TestClusterApplication))]
public class MiddlewareTests(ITestOutputHelper outputHelper, TestClusterApplication application)
{
    private readonly ITestOutputHelper _outputHelper = outputHelper;

    [Fact]
    public async Task ValidationException()
    {
        var response = await application.CreateClient()
            .GetAsync("test/test1");
        response.StatusCode
            .ShouldBe(HttpStatusCode.InternalServerError);
        var content = await response.Content.ReadAsStringAsync();
        var result = await response.Content.ReadFromJsonAsync<Result>();
        result.IsFailed
            .ShouldBeTrue();
        result.Problem
            .ShouldNotBeNull();
        result.Problem!.Detail
            .ShouldBe("ValidationException");
    }

    [Fact]
    public async Task InvalidDataException()
    {
        var response = await application.CreateClient()
            .GetAsync("test/test2");
        response.StatusCode
            .ShouldBe(HttpStatusCode.Conflict);
        var content = await response.Content.ReadAsStringAsync();
        var result = await response.Content.ReadFromJsonAsync<Result>();
        result.IsFailed
            .ShouldBeTrue();
        result.Problem
            .ShouldNotBeNull();
        result.Problem!.Detail
            .ShouldBe("InvalidDataException");
    }

    [Fact]
    public async Task ValidationExceptionSginalR()
    {
        var connection = application.CreateSignalRClient(nameof(TestHub));
        await connection.StartAsync();
        connection.State
            .ShouldBe(HubConnectionState.Connected);
        var result = await connection.InvokeAsync<Result<int>>("DoTest");
        result.IsSuccess
            .ShouldBeTrue();
        result.Value
            .ShouldBe(5);
    }

    [Fact]
    public async Task InvalidDataExceptionSignalR()
    {
        var connection = application.CreateSignalRClient(nameof(TestHub));
        await connection.StartAsync();
        connection.State
            .ShouldBe(HubConnectionState.Connected);
        var result = await connection.InvokeAsync<Result>("Throw");
        result.IsFailed
            .ShouldBeTrue();
        result.Problem
            .ShouldNotBeNull();
        result.Problem!.Detail
            .ShouldBe("InvalidDataException");
    }

    [Fact]
    public async Task UnauthorizedAccess()
    {
        var response = await application.CreateClient()
            .GetAsync("test/test3");
        response.StatusCode
            .ShouldBe(HttpStatusCode.Unauthorized);

        // Authorization attribute returns empty 401 by default in ASP.NET Core
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldBeEmpty();
    }

    [Fact]
    public async Task UnauthorizedResult()
    {
        // Test endpoint that returns Result.FailUnauthorized()
        var response = await application.CreateClient()
            .GetAsync("test/result-unauthorized");
        response.StatusCode
            .ShouldBe(HttpStatusCode.Unauthorized);

        var result = await response.Content.ReadFromJsonAsync<Result>();
        result.IsFailed
            .ShouldBeTrue();
        result.Problem
            .ShouldNotBeNull();
        result.Problem!.StatusCode
            .ShouldBe((int)HttpStatusCode.Unauthorized);
        result.Problem
            .Detail
            .ShouldBe("You need to log in to access this resource");
    }

    [Fact]
    public async Task ForbiddenResult()
    {
        // Test endpoint that returns Result.FailForbidden()
        var response = await application.CreateClient()
            .GetAsync("test/result-forbidden");
        response.StatusCode
            .ShouldBe(HttpStatusCode.Forbidden);

        var result = await response.Content.ReadFromJsonAsync<Result>();
        result.IsFailed
            .ShouldBeTrue();
        result.Problem
            .ShouldNotBeNull();
        result.Problem!.StatusCode
            .ShouldBe((int)HttpStatusCode.Forbidden);
        result.Problem
            .Detail
            .ShouldBe("You don't have permission to perform this action");
    }

    [Fact]
    public async Task NotFoundResult()
    {
        // Test endpoint that returns Result<string>.FailNotFound()
        var response = await application.CreateClient()
            .GetAsync("test/result-not-found");
        response.StatusCode
            .ShouldBe(HttpStatusCode.NotFound);

        var result = await response.Content.ReadFromJsonAsync<Result<string>>();
        result.IsFailed
            .ShouldBeTrue();
        result.Problem
            .ShouldNotBeNull();
        result.Problem!.StatusCode
            .ShouldBe((int)HttpStatusCode.NotFound);
        result.Problem
            .Detail
            .ShouldBe("User with ID 123 not found");
    }

    [Fact]
    public async Task SuccessResult()
    {
        // Test endpoint that returns Result.Succeed()
        var response = await application.CreateClient()
            .GetAsync("test/result-success");
        response.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result>();
        result.IsSuccess
            .ShouldBeTrue();
        result.Problem
            .ShouldBeNull();
    }

    [Fact]
    public async Task FailResult()
    {
        // Test endpoint that returns Result.Fail()
        var response = await application.CreateClient()
            .GetAsync("test/result-fail");
        response.StatusCode
            .ShouldBe(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<Result>();
        result.IsFailed
            .ShouldBeTrue();
        result.Problem
            .ShouldNotBeNull();
        result.Problem!.StatusCode
            .ShouldBe((int)HttpStatusCode.BadRequest);
        result.Problem
            .Title
            .ShouldBe("Operation failed");
        result.Problem
            .Detail
            .ShouldBe("Something went wrong");
    }
}
