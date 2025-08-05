using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
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
            .Should()
            .Be(HttpStatusCode.InternalServerError);
        var content = await response.Content.ReadAsStringAsync();
        var result = await response.Content.ReadFromJsonAsync<Result>();
        result.IsFailed
            .Should()
            .BeTrue();
        result.Problem
            .Should()
            .NotBeNull();
        result.Problem!.Detail
            .Should()
            .Be("ValidationException");
    }

    [Fact]
    public async Task InvalidDataException()
    {
        var response = await application.CreateClient()
            .GetAsync("test/test2");
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
        var content = await response.Content.ReadAsStringAsync();
        var result = await response.Content.ReadFromJsonAsync<Result>();
        result.IsFailed
            .Should()
            .BeTrue();
        result.Problem
            .Should()
            .NotBeNull();
        result.Problem!.Detail
            .Should()
            .Be("InvalidDataException");
    }

    [Fact]
    public async Task ValidationExceptionSginalR()
    {
        var connection = application.CreateSignalRClient(nameof(TestHub));
        await connection.StartAsync();
        connection.State
            .Should()
            .Be(HubConnectionState.Connected);
        var result = await connection.InvokeAsync<Result<int>>("DoTest");
        result.IsSuccess
            .Should()
            .BeTrue();
        result.Value
            .Should()
            .Be(5);
    }

    [Fact]
    public async Task InvalidDataExceptionSignalR()
    {
        var connection = application.CreateSignalRClient(nameof(TestHub));
        await connection.StartAsync();
        connection.State
            .Should()
            .Be(HubConnectionState.Connected);
        var result = await connection.InvokeAsync<Result>("Throw");
        result.IsFailed
            .Should()
            .BeTrue();
        result.Problem
            .Should()
            .NotBeNull();
        result.Problem!.Detail
            .Should()
            .Be("InvalidDataException");
    }

    [Fact]
    public async Task UnauthorizedAccess()
    {
        var response = await application.CreateClient()
            .GetAsync("test/test3");
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);

        // Authorization attribute returns empty 401 by default in ASP.NET Core
        var content = await response.Content.ReadAsStringAsync();
        content.Should()
            .BeEmpty();
    }

    [Fact]
    public async Task UnauthorizedResult()
    {
        // Test endpoint that returns Result.FailUnauthorized()
        var response = await application.CreateClient()
            .GetAsync("test/result-unauthorized");
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);

        var result = await response.Content.ReadFromJsonAsync<Result>();
        result.Should()
            .NotBeNull();
        result!.IsFailed
            .Should()
            .BeTrue();
        result.Problem
            .Should()
            .NotBeNull();
        result.Problem!.StatusCode
            .Should()
            .Be((int)HttpStatusCode.Unauthorized);
        result.Problem
            .Detail
            .Should()
            .Be("You need to log in to access this resource");
    }

    [Fact]
    public async Task ForbiddenResult()
    {
        // Test endpoint that returns Result.FailForbidden()
        var response = await application.CreateClient()
            .GetAsync("test/result-forbidden");
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Forbidden);

        var result = await response.Content.ReadFromJsonAsync<Result>();
        result.Should()
            .NotBeNull();
        result!.IsFailed
            .Should()
            .BeTrue();
        result.Problem
            .Should()
            .NotBeNull();
        result.Problem!.StatusCode
            .Should()
            .Be((int)HttpStatusCode.Forbidden);
        result.Problem
            .Detail
            .Should()
            .Be("You don't have permission to perform this action");
    }

    [Fact]
    public async Task NotFoundResult()
    {
        // Test endpoint that returns Result<string>.FailNotFound()
        var response = await application.CreateClient()
            .GetAsync("test/result-not-found");
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);

        var result = await response.Content.ReadFromJsonAsync<Result<string>>();
        result.Should()
            .NotBeNull();
        result!.IsFailed
            .Should()
            .BeTrue();
        result.Problem
            .Should()
            .NotBeNull();
        result.Problem!.StatusCode
            .Should()
            .Be((int)HttpStatusCode.NotFound);
        result.Problem
            .Detail
            .Should()
            .Be("User with ID 123 not found");
    }

    [Fact]
    public async Task SuccessResult()
    {
        // Test endpoint that returns Result.Succeed()
        var response = await application.CreateClient()
            .GetAsync("test/result-success");
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result>();
        result.Should()
            .NotBeNull();
        result!.IsSuccess
            .Should()
            .BeTrue();
        result.Problem
            .Should()
            .BeNull();
    }

    [Fact]
    public async Task FailResult()
    {
        // Test endpoint that returns Result.Fail()
        var response = await application.CreateClient()
            .GetAsync("test/result-fail");
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<Result>();
        result.Should()
            .NotBeNull();
        result!.IsFailed
            .Should()
            .BeTrue();
        result.Problem
            .Should()
            .NotBeNull();
        result.Problem!.StatusCode
            .Should()
            .Be((int)HttpStatusCode.BadRequest);
        result.Problem
            .Title
            .Should()
            .Be("Operation failed");
        result.Problem
            .Detail
            .Should()
            .Be("Something went wrong");
    }
}