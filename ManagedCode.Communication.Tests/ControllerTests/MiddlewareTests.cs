using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Communication.Tests.TestApp;
using ManagedCode.Communication.Tests.TestApp.Controllers;
using ManagedCode.Communication.Tests.TestApp.Grains;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;
using Xunit.Abstractions;

namespace ManagedCode.Communication.Tests.ControllerTests;

[Collection(nameof(TestClusterApplication))]
public class MiddlewareTests 
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly TestClusterApplication _application;

    public MiddlewareTests(ITestOutputHelper outputHelper, TestClusterApplication application)
    {
        _outputHelper = outputHelper;
        _application = application;
    }

    [Fact]
    public async Task ValidationException()
    {
        var response = await _application.CreateClient().GetAsync($"test/test1");
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var content = await response.Content.ReadAsStringAsync();
        var result = await response.Content.ReadFromJsonAsync<Result>();
        result.IsFailed.Should().BeTrue();
        result.GetError().Value.Message.Should().Be("ValidationException");
    }
    
    [Fact]
    public async Task InvalidDataException()
    {
        var response = await _application.CreateClient().GetAsync($"test/test2");
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var content = await response.Content.ReadAsStringAsync();
        var result = await response.Content.ReadFromJsonAsync<Result<int>>();
        result.IsFailed.Should().BeTrue();
        result.GetError().Value.Message.Should().Be("InvalidDataException");
    }
    
    [Fact]
    public async Task ValidationExceptionSginalR()
    {
        var connection = _application.CreateSignalRClient(nameof(TestHub));
        await connection.StartAsync();
        connection.State.Should().Be(HubConnectionState.Connected);
        var result = await connection.InvokeAsync<Result<int>>("DoTest");
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(5);
    }
    
    [Fact]
    public async Task InvalidDataExceptionSignalR()
    {
        var connection = _application.CreateSignalRClient(nameof(TestHub));
        await connection.StartAsync();
        connection.State.Should().Be(HubConnectionState.Connected);
        var result = await connection.InvokeAsync<Result<int>>("Throw");
        result.IsFailed.Should().BeTrue();
        result.GetError().Value.Message.Should().Be("InvalidDataException");
    }
}