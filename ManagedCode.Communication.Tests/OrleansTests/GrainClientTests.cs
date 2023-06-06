using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Communication.Tests.TestApp;
using ManagedCode.Communication.Tests.TestApp.Grains;
using Xunit;
using Xunit.Abstractions;

namespace ManagedCode.Communication.Tests.OrleansTests;

[Collection(nameof(TestClusterApplication))]
public class GrainClientTests 
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly TestClusterApplication _application;

    public GrainClientTests(ITestOutputHelper outputHelper, TestClusterApplication application)
    {
        _outputHelper = outputHelper;
        _application = application;
    }
    
    [Fact]
    public async Task IntResult()
    {
        var intResult = await _application.Cluster.Client.GetGrain<ITestGrain>(0).TestResultInt();
        intResult.IsSuccess.Should().Be(true);
        intResult.Value.Should().Be(5);
    }
    
    [Fact]
    public async Task Result()
    {
        var intResult = await _application.Cluster.Client.GetGrain<ITestGrain>(0).TestResult();
        intResult.IsSuccess.Should().Be(true);
    }
    
    [Fact]
    public async Task IntResultError()
    {
        var intResult = await _application.Cluster.Client.GetGrain<ITestGrain>(0).TestResultIntError();
        intResult.IsFailed.Should().Be(true);
    }
    
    [Fact]
    public async Task ResultError()
    {
        var intResult = await _application.Cluster.Client.GetGrain<ITestGrain>(0).TestResultError();
        intResult.IsFailed.Should().Be(true);
    }
}