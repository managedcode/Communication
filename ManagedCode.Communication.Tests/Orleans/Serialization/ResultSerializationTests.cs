using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Communication.Tests.Orleans.Fixtures;
using ManagedCode.Communication.Tests.Orleans.Grains;
using ManagedCode.Communication.Tests.Orleans.Models;
using Orleans;
using Xunit;

namespace ManagedCode.Communication.Tests.Orleans.Serialization;

/// <summary>
/// Tests for Result serialization through Orleans grain calls
/// </summary>
public class ResultSerializationTests : IClassFixture<OrleansClusterFixture>
{
    private readonly IGrainFactory _grainFactory;

    public ResultSerializationTests(OrleansClusterFixture fixture)
    {
        _grainFactory = fixture.Cluster.GrainFactory;
    }

    [Fact]
    public async Task Result_Success_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        var result = Result.Succeed();

        // Act
        var echoed = await grain.EchoResultAsync(result);

        // Assert
        echoed.Should().NotBeNull();
        echoed.IsSuccess.Should().BeTrue();
        echoed.HasProblem.Should().BeFalse();
        echoed.Problem.Should().BeNull();
    }

    [Fact]
    public async Task Result_WithValidationProblem_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        
        var problem = Problem.Validation(
            ("email", "Invalid email format"),
            ("password", "Password too weak"),
            ("username", "Username already taken")
        );
        problem.Extensions["requestId"] = "req-123";
        problem.Extensions["timestamp"] = DateTimeOffset.UtcNow.ToString("O");
        
        var result = Result.Fail(problem);

        // Act
        var echoed = await grain.EchoResultAsync(result);

        // Assert
        echoed.Should().NotBeNull();
        echoed.IsSuccess.Should().BeFalse();
        echoed.HasProblem.Should().BeTrue();
        echoed.Problem.Should().NotBeNull();
        echoed.Problem!.Type.Should().Be(problem.Type);
        echoed.Problem!.Title.Should().Be(problem.Title);
        echoed.Problem!.StatusCode.Should().Be(problem.StatusCode);
        echoed.Problem!.Detail.Should().Be(problem.Detail);
        
        var errors = echoed.Problem!.GetValidationErrors();
        errors.Should().NotBeNull();
        errors.Should().HaveCount(3);
        errors!["email"].Should().Contain("Invalid email format");
        errors["password"].Should().Contain("Password too weak");
        errors["username"].Should().Contain("Username already taken");
        
        echoed.Problem!.Extensions["requestId"].Should().Be("req-123");
        echoed.Problem!.Extensions["timestamp"].Should().NotBeNull();
    }

    [Fact]
    public async Task ResultT_WithComplexValue_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        
        var response = new PaymentResponse
        {
            TransactionId = "txn-123",
            Status = "completed",
            ProcessedAt = DateTimeOffset.UtcNow,
            Details = new Dictionary<string, object>
            {
                ["gateway"] = "stripe",
                ["fee"] = 2.99m,
                ["net"] = 97.01m
            }
        };
        
        var result = Result<PaymentResponse>.Succeed(response);

        // Act
        var echoed = await grain.EchoResultAsync(result);

        // Assert
        echoed.Should().NotBeNull();
        echoed.IsSuccess.Should().BeTrue();
        echoed.HasProblem.Should().BeFalse();
        echoed.Value.Should().NotBeNull();
        echoed.Value!.TransactionId.Should().Be("txn-123");
        echoed.Value.Status.Should().Be("completed");
        echoed.Value.ProcessedAt.Should().BeCloseTo(response.ProcessedAt, TimeSpan.FromSeconds(1));
        echoed.Value.Details.Should().NotBeNull();
        echoed.Value.Details["gateway"].Should().Be("stripe");
        echoed.Value.Details["fee"].Should().Be(2.99m);
        echoed.Value.Details["net"].Should().Be(97.01m);
    }

    [Fact]
    public async Task ResultT_WithNullValue_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        var result = Result<string?>.Succeed((string?)null);

        // Act
        var echoed = await grain.EchoResultAsync(result);

        // Assert
        echoed.Should().NotBeNull();
        echoed.IsSuccess.Should().BeTrue();
        echoed.Value.Should().BeNull();
    }

    [Fact]
    public async Task ResultT_WithDifferentProblemTypes_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        
        // Test different problem types
        var testCases = new[]
        {
            Result<UserProfile>.FailNotFound("User not found"),
            Result<UserProfile>.FailUnauthorized("Invalid credentials"),
            Result<UserProfile>.FailForbidden("Access denied"),
            Result<UserProfile>.Fail("User already exists", "User already exists", System.Net.HttpStatusCode.Conflict),
            Result<UserProfile>.Fail("Invalid data", "Invalid data", System.Net.HttpStatusCode.UnprocessableEntity),
            Result<UserProfile>.Fail("Service temporarily unavailable", "Service temporarily unavailable", System.Net.HttpStatusCode.ServiceUnavailable),
            Result<UserProfile>.Fail("Custom error", "Something went wrong", (System.Net.HttpStatusCode)418) // I'm a teapot
        };

        foreach (var testCase in testCases)
        {
            // Act
            var echoed = await grain.EchoResultAsync(testCase);

            // Assert
            echoed.Should().NotBeNull();
            echoed.IsSuccess.Should().BeFalse();
            echoed.Problem.Should().NotBeNull();
            echoed.Problem!.StatusCode.Should().Be(testCase.Problem!.StatusCode);
            echoed.Problem!.Title.Should().Be(testCase.Problem!.Title);
            echoed.Problem!.Detail.Should().Be(testCase.Problem!.Detail);
        }
    }

    [Fact]
    public async Task ResultT_WithComplexNestedObject_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Name = "John Doe",
            CreatedAt = DateTimeOffset.UtcNow,
            Attributes = new Dictionary<string, object>
            {
                ["age"] = 30,
                ["verified"] = true,
                ["preferences"] = new Dictionary<string, string>
                {
                    ["theme"] = "dark",
                    ["language"] = "en"
                },
                ["scores"] = new[] { 85, 92, 78 }
            }
        };
        
        var result = Result<UserProfile>.Succeed(profile);

        // Act
        var echoed = await grain.EchoResultAsync(result);

        // Assert
        echoed.Should().NotBeNull();
        echoed.IsSuccess.Should().BeTrue();
        echoed.Value.Should().NotBeNull();
        echoed.Value!.Id.Should().Be(profile.Id);
        echoed.Value.Email.Should().Be(profile.Email);
        echoed.Value.Name.Should().Be(profile.Name);
        echoed.Value.CreatedAt.Should().BeCloseTo(profile.CreatedAt, TimeSpan.FromSeconds(1));
        
        echoed.Value.Attributes.Should().NotBeNull();
        echoed.Value.Attributes["age"].Should().Be(30);
        echoed.Value.Attributes["verified"].Should().Be(true);
        echoed.Value.Attributes["preferences"].Should().NotBeNull();
        
        var preferences = echoed.Value.Attributes["preferences"] as Dictionary<string, string>;
        preferences.Should().NotBeNull();
        preferences!["theme"].Should().Be("dark");
        preferences["language"].Should().Be("en");
        
        var scores = echoed.Value.Attributes["scores"] as int[];
        scores.Should().NotBeNull();
        scores.Should().BeEquivalentTo(new[] { 85, 92, 78 });
    }
}