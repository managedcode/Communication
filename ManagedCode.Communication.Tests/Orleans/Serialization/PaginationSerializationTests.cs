using System;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Tests.Orleans.Fixtures;
using ManagedCode.Communication.Tests.Orleans.Grains;
using Orleans;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.Orleans.Serialization;

public class PaginationSerializationTests : IClassFixture<OrleansClusterFixture>
{
    private readonly IGrainFactory _grainFactory;

    public PaginationSerializationTests(OrleansClusterFixture fixture)
    {
        _grainFactory = fixture.Cluster.GrainFactory;
    }

    [Fact]
    public async Task PaginationRequest_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        var options = new PaginationOptions(defaultPageSize: 20, maxPageSize: 100, minPageSize: 5);
        var request = PaginationRequest.Create(35, 12, options);

        // Act
        var echoed = await grain.EchoPaginationRequestAsync(request);

        // Assert
        echoed.ShouldNotBeNull();
        echoed.Skip.ShouldBe(35);
        echoed.Take.ShouldBe(12);
        echoed.PageSize.ShouldBe(12);
        echoed.PageNumber.ShouldBe((35 / 12) + 1);
        echoed.Offset.ShouldBe(35);
        echoed.Limit.ShouldBe(12);
        echoed.HasExplicitPageSize.ShouldBeTrue();
    }
}
