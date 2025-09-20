using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;
using ManagedCode.Communication.Tests.Orleans.Fixtures;
using ManagedCode.Communication.Tests.Orleans.Grains;
using ManagedCode.Communication.Tests.Orleans.Models;
using Orleans;
using Xunit;
using ManagedCode.Communication.Tests.TestHelpers;

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
        echoed.IsSuccess.ShouldBeTrue();
        echoed.HasProblem.ShouldBeFalse();
        echoed.Problem.ShouldBeNull();
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
        echoed.IsSuccess.ShouldBeFalse();
        echoed.HasProblem.ShouldBeTrue();
        echoed.Problem.ShouldNotBeNull();
        echoed.Problem!.Type.ShouldBe(problem.Type);
        echoed.Problem!.Title.ShouldBe(problem.Title);
        echoed.Problem!.StatusCode.ShouldBe(problem.StatusCode);
        echoed.Problem!.Detail.ShouldBe(problem.Detail);
        
        var errors = echoed.Problem!.GetValidationErrors();
        errors.ShouldNotBeNull();
        errors.ShouldHaveCount(3);
        errors!["email"].ShouldContain("Invalid email format");
        errors["password"].ShouldContain("Password too weak");
        errors["username"].ShouldContain("Username already taken");
        
        echoed.Problem!.Extensions["requestId"].ShouldBe("req-123");
        echoed.Problem!.Extensions["timestamp"].ShouldNotBeNull();
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
        echoed.IsSuccess.ShouldBeTrue();
        echoed.HasProblem.ShouldBeFalse();
        echoed.Value.ShouldNotBeNull();
        echoed.Value!.TransactionId.ShouldBe("txn-123");
        echoed.Value.Status.ShouldBe("completed");
        echoed.Value.ProcessedAt.ShouldBeCloseTo(response.ProcessedAt, TimeSpan.FromSeconds(1));
        echoed.Value.Details.ShouldNotBeNull();
        echoed.Value.Details["gateway"].ShouldBe("stripe");
        echoed.Value.Details["fee"].ShouldBe(2.99m);
        echoed.Value.Details["net"].ShouldBe(97.01m);
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
        echoed.IsSuccess.ShouldBeTrue();
        echoed.Value.ShouldBeNull();
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
            echoed.IsSuccess.ShouldBeFalse();
            echoed.Problem.ShouldNotBeNull();
            echoed.Problem!.StatusCode.ShouldBe(testCase.Problem!.StatusCode);
            echoed.Problem!.Title.ShouldBe(testCase.Problem!.Title);
            echoed.Problem!.Detail.ShouldBe(testCase.Problem!.Detail);
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
        echoed.IsSuccess.ShouldBeTrue();
        echoed.Value.ShouldNotBeNull();
        echoed.Value!.Id.ShouldBe(profile.Id);
        echoed.Value.Email.ShouldBe(profile.Email);
        echoed.Value.Name.ShouldBe(profile.Name);
        echoed.Value.CreatedAt.ShouldBeCloseTo(profile.CreatedAt, TimeSpan.FromSeconds(1));
        
        echoed.Value.Attributes.ShouldNotBeNull();
        echoed.Value.Attributes["age"].ShouldBe(30);
        echoed.Value.Attributes["verified"].ShouldBe(true);
        echoed.Value.Attributes["preferences"].ShouldNotBeNull();
        
        var preferences = echoed.Value.Attributes["preferences"] as Dictionary<string, string>;
        preferences.ShouldNotBeNull();
        preferences!["theme"].ShouldBe("dark");
        preferences["language"].ShouldBe("en");
        
        var scores = echoed.Value.Attributes["scores"] as int[];
        scores.ShouldNotBeNull();
        scores.ShouldBeEquivalentTo(new[] { 85, 92, 78 });
    }
}
