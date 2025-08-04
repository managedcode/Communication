using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Communication.Tests.Common.TestApp;
using ManagedCode.Communication.Tests.TestApp;
using ManagedCode.Communication.Tests.TestApp.Controllers;
using ManagedCode.Communication.Tests.TestApp.Grains;
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
        var response = await application.CreateClient().GetAsync($"test/test1");
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var content = await response.Content.ReadAsStringAsync();
        var result = await response.Content.ReadFromJsonAsync<Result<Problem>>();
        result.IsFailed.Should().BeTrue();
        result.GetError()?.Message.Should().Be("ValidationException");
    }
    
    [Fact]
    public async Task InvalidDataException()
    {
        var response = await application.CreateClient().GetAsync($"test/test2");
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var content = await response.Content.ReadAsStringAsync();
        var result = await response.Content.ReadFromJsonAsync<Result<Problem>>();
        result.IsFailed.Should().BeTrue();
        result.GetError()?.Message.Should().Be("InvalidDataException");
    }
    
    [Fact]
    public async Task ValidationExceptionSginalR()
    {
        var connection = application.CreateSignalRClient(nameof(TestHub));
        await connection.StartAsync();
        connection.State.Should().Be(HubConnectionState.Connected);
        var result = await connection.InvokeAsync<Result<int>>("DoTest");
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(5);
    }
    
    [Fact]
    public async Task InvalidDataExceptionSignalR()
    {
        var connection = application.CreateSignalRClient(nameof(TestHub));
        await connection.StartAsync();
        connection.State.Should().Be(HubConnectionState.Connected);
        var result = await connection.InvokeAsync<Result<Problem>>("Throw");
        result.IsFailed.Should().BeTrue();
        result.GetError().Should().NotBeNull();
        result.GetError()!.Value.Message.Should().Be("InvalidDataException");
    }
}