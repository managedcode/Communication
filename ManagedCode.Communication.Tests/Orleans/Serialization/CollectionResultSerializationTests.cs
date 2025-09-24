using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Tests.Orleans.Fixtures;
using ManagedCode.Communication.Tests.Orleans.Grains;
using ManagedCode.Communication.Tests.Orleans.Models;
using Orleans;
using Xunit;
using ManagedCode.Communication.Tests.TestHelpers;

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
        echoed.IsSuccess.ShouldBeTrue();
        echoed.Collection.ShouldNotBeNull();
        echoed.Collection.ShouldHaveCount(5);
        echoed.PageNumber.ShouldBe(2);
        echoed.PageSize.ShouldBe(5);
        echoed.TotalItems.ShouldBe(50);
        echoed.TotalPages.ShouldBe(10);
        
        for (int i = 0; i < 5; i++)
        {
            echoed.Collection[i].Id.ShouldBe(i + 1);
            echoed.Collection[i].Name.ShouldBe($"Item {i + 1}");
            echoed.Collection[i].Tags.ShouldNotBeNull();
            echoed.Collection[i].Tags.ShouldContain($"tag{i + 1}");
            echoed.Collection[i].Tags.ShouldContain("common");
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
        echoed.IsSuccess.ShouldBeTrue();
        echoed.Collection.ShouldNotBeNull();
        echoed.Collection.ShouldBeEmpty();
        echoed.PageNumber.ShouldBe(1);
        echoed.PageSize.ShouldBe(10);
        echoed.TotalItems.ShouldBe(0);
        echoed.TotalPages.ShouldBe(0);
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
        echoed.IsSuccess.ShouldBeFalse();
        echoed.HasProblem.ShouldBeTrue();
        echoed.Problem.ShouldNotBeNull();
        echoed.Problem!.StatusCode.ShouldBe(503);
        echoed.Problem!.Title.ShouldBe("ServiceUnavailable");
        echoed.Problem!.Detail.ShouldBe("Database connection failed");
        echoed.Collection.ShouldBeEmpty();
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
            CreatedAt = DateTime.UtcNow.AddDays(-i),
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
        echoed.IsSuccess.ShouldBeTrue();
        echoed.Collection.ShouldNotBeNull();
        echoed.Collection.ShouldHaveCount(3);
        echoed.TotalItems.ShouldBe(100);
        echoed.TotalPages.ShouldBe(34); // ceiling(100/3)
        
        for (int i = 0; i < 3; i++)
        {
            echoed.Collection[i].Id.ShouldBe(profiles[i].Id);
            echoed.Collection[i].Email.ShouldBe(profiles[i].Email);
            echoed.Collection[i].Name.ShouldBe(profiles[i].Name);
            echoed.Collection[i].CreatedAt.ShouldBeCloseTo(profiles[i].CreatedAt, TimeSpan.FromSeconds(1));
            echoed.Collection[i].Attributes.ShouldNotBeNull();
            echoed.Collection[i].Attributes["level"].ShouldBe((i + 1) * 10);
            echoed.Collection[i].Attributes["active"].ShouldBe((i + 1) % 2 == 0);
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
        echoed.IsSuccess.ShouldBeTrue();
        echoed.Collection.ShouldHaveCount(10);
        echoed.PageNumber.ShouldBe(100);
        echoed.PageSize.ShouldBe(10);
        echoed.TotalItems.ShouldBe(10000);
        echoed.TotalPages.ShouldBe(1000);
        // Pagination properties
        (echoed.PageNumber < echoed.TotalPages).ShouldBeTrue(); // Has next page
        (echoed.PageNumber > 1).ShouldBeTrue(); // Has previous page
        
        // Verify items start from 991
        echoed.Collection[0].Id.ShouldBe(991);
        echoed.Collection[9].Id.ShouldBe(1000);
    }
}
