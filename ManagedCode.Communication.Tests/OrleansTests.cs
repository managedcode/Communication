using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Communication.Tests.TestClusterApp;
using ManagedCode.Communication.Tests.TestClusterApp.Grains.Abstractions;
using Xunit;

namespace ManagedCode.Communication.Tests;

[Collection(nameof(TestClusterApplication))]
public class OrleansTests
{
    private readonly TestClusterApplication _testClusterApplication;

    public OrleansTests(TestClusterApplication testClusterApplication)
    {
        _testClusterApplication = testClusterApplication;
    }

    [Fact]
    public async Task GetFailedResult_ConvertToGenericResult_ConvertSuccessfully()
    {
        // Arrange
        var grain = _testClusterApplication.Cluster.Client.GetGrain<ITestGrain>(Guid.NewGuid().ToString());
        
        // Act
        Result<int> result = await grain.GetFailedResult();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.GetError().Should().NotBeNull();
        result.GetError().Value.ErrorCode.Should().Be(nameof(HttpStatusCode.Unauthorized));
    }

    [Fact]
    public async Task GetFailedResult_WhenGrainHasFilter_CastResultToGenericResult()
    {
        // Arrange
        var grain = _testClusterApplication.Cluster.Client.GetGrain<IFilteredGrain>(Guid.NewGuid().ToString());

        // Act
        // 🥔Error here because in filter it converted to object.
        Result<int> result = await grain.GetNumber();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.GetError().Should().NotBeNull();
        result.GetError().Value.ErrorCode.Should().Be(nameof(HttpStatusCode.Unauthorized));
    }
}