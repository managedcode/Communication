using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Tests.Orleans.Fixtures;
using ManagedCode.Communication.Tests.Orleans.Grains;
using ManagedCode.Communication.Tests.Orleans.Models;
using Orleans;
using Xunit;

namespace ManagedCode.Communication.Tests.Orleans.Serialization;

/// <summary>
/// Tests for CollectionResult serialization through Orleans grain calls
/// </summary>
public class CollectionResultSerializationTests : IClassFixture<OrleansClusterFixture>
{
    private readonly IGrainFactory _grainFactory;

    public CollectionResultSerializationTests(OrleansClusterFixture fixture)
    {
        _grainFactory = fixture.Cluster.GrainFactory;
    }

    [Fact]
    public async Task CollectionResult_WithPagination_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        
        var items = Enumerable.Range(1, 5).Select(i => new TestItem
        {
            Id = i,
            Name = $"Item {i}",
            Tags = new[] { $"tag{i}", "common" }
        }).ToArray();
        
        var collectionResult = CollectionResult<TestItem>.Succeed(
            items,
            pageNumber: 2,
            pageSize: 5,
            totalItems: 50
        );

        // Act
        var echoed = await grain.EchoCollectionResultAsync(collectionResult);

        // Assert
        echoed.Should().NotBeNull();
        echoed.IsSuccess.Should().BeTrue();
        echoed.Collection.Should().NotBeNull();
        echoed.Collection.Should().HaveCount(5);
        echoed.PageNumber.Should().Be(2);
        echoed.PageSize.Should().Be(5);
        echoed.TotalItems.Should().Be(50);
        echoed.TotalPages.Should().Be(10);
        
        for (int i = 0; i < 5; i++)
        {
            echoed.Collection[i].Id.Should().Be(i + 1);
            echoed.Collection[i].Name.Should().Be($"Item {i + 1}");
            echoed.Collection[i].Tags.Should().NotBeNull();
            echoed.Collection[i].Tags.Should().Contain($"tag{i + 1}");
            echoed.Collection[i].Tags.Should().Contain("common");
        }
    }

    [Fact]
    public async Task CollectionResult_EmptyCollection_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        
        var collectionResult = CollectionResult<TestItem>.Succeed(
            Array.Empty<TestItem>(),
            pageNumber: 1,
            pageSize: 10,
            totalItems: 0
        );

        // Act
        var echoed = await grain.EchoCollectionResultAsync(collectionResult);

        // Assert
        echoed.Should().NotBeNull();
        echoed.IsSuccess.Should().BeTrue();
        echoed.Collection.Should().NotBeNull();
        echoed.Collection.Should().BeEmpty();
        echoed.PageNumber.Should().Be(1);
        echoed.PageSize.Should().Be(10);
        echoed.TotalItems.Should().Be(0);
        echoed.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task CollectionResult_WithProblem_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        
        var problem = Problem.FromStatusCode(System.Net.HttpStatusCode.ServiceUnavailable, "Database connection failed");
        var collectionResult = CollectionResult<TestItem>.Fail(problem);

        // Act
        var echoed = await grain.EchoCollectionResultAsync(collectionResult);

        // Assert
        echoed.Should().NotBeNull();
        echoed.IsSuccess.Should().BeFalse();
        echoed.HasProblem.Should().BeTrue();
        echoed.Problem.Should().NotBeNull();
        echoed.Problem!.StatusCode.Should().Be(503);
        echoed.Problem!.Title.Should().Be("ServiceUnavailable");
        echoed.Problem!.Detail.Should().Be("Database connection failed");
        echoed.Collection.Should().BeEmpty();
    }

    [Fact]
    public async Task CollectionResult_WithComplexObjects_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        
        var profiles = Enumerable.Range(1, 3).Select(i => new UserProfile
        {
            Id = Guid.NewGuid(),
            Email = $"user{i}@example.com",
            Name = $"User {i}",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-i),
            Attributes = new()
            {
                ["level"] = i * 10,
                ["active"] = i % 2 == 0
            }
        }).ToList();
        
        var collectionResult = CollectionResult<UserProfile>.Succeed(
            profiles,
            pageNumber: 1,
            pageSize: 3,
            totalItems: 100
        );

        // Act
        var echoed = await grain.EchoCollectionResultAsync(collectionResult);

        // Assert
        echoed.Should().NotBeNull();
        echoed.IsSuccess.Should().BeTrue();
        echoed.Collection.Should().NotBeNull();
        echoed.Collection.Should().HaveCount(3);
        echoed.TotalItems.Should().Be(100);
        echoed.TotalPages.Should().Be(34); // ceiling(100/3)
        
        for (int i = 0; i < 3; i++)
        {
            echoed.Collection[i].Id.Should().Be(profiles[i].Id);
            echoed.Collection[i].Email.Should().Be(profiles[i].Email);
            echoed.Collection[i].Name.Should().Be(profiles[i].Name);
            echoed.Collection[i].CreatedAt.Should().BeCloseTo(profiles[i].CreatedAt, TimeSpan.FromSeconds(1));
            echoed.Collection[i].Attributes.Should().NotBeNull();
            echoed.Collection[i].Attributes["level"].Should().Be((i + 1) * 10);
            echoed.Collection[i].Attributes["active"].Should().Be((i + 1) % 2 == 0);
        }
    }

    [Fact]
    public async Task CollectionResult_LargePagination_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        
        var items = Enumerable.Range(991, 10).Select(i => new TestItem
        {
            Id = i,
            Name = $"Item {i}",
            Tags = new[] { "large-dataset" }
        }).ToArray();
        
        var collectionResult = CollectionResult<TestItem>.Succeed(
            items,
            pageNumber: 100,
            pageSize: 10,
            totalItems: 10000
        );

        // Act
        var echoed = await grain.EchoCollectionResultAsync(collectionResult);

        // Assert
        echoed.Should().NotBeNull();
        echoed.IsSuccess.Should().BeTrue();
        echoed.Collection.Should().HaveCount(10);
        echoed.PageNumber.Should().Be(100);
        echoed.PageSize.Should().Be(10);
        echoed.TotalItems.Should().Be(10000);
        echoed.TotalPages.Should().Be(1000);
        // Pagination properties
        (echoed.PageNumber < echoed.TotalPages).Should().BeTrue(); // Has next page
        (echoed.PageNumber > 1).Should().BeTrue(); // Has previous page
        
        // Verify items start from 991
        echoed.Collection[0].Id.Should().Be(991);
        echoed.Collection[9].Id.Should().Be(1000);
    }
}