using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Communication.Tests.TestApp;
using Xunit;
using Xunit.Abstractions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ManagedCode.Communication.Tests;

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
    }
    
    [Fact]
    public async Task InvalidDataException()
    {
        var response = await _application.CreateClient().GetAsync($"test/test2");
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var content = await response.Content.ReadAsStringAsync();
        var result = await response.Content.ReadFromJsonAsync<Result>();
    }
}