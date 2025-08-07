using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Communication.Tests.Orleans.Fixtures;
using ManagedCode.Communication.Tests.Orleans.Grains;
using Orleans;
using Xunit;

namespace ManagedCode.Communication.Tests.Orleans.Serialization;

/// <summary>
/// Tests for Problem serialization through Orleans grain calls
/// </summary>
public class ProblemSerializationTests : IClassFixture<OrleansClusterFixture>
{
    private readonly IGrainFactory _grainFactory;

    public ProblemSerializationTests(OrleansClusterFixture fixture)
    {
        _grainFactory = fixture.Cluster.GrainFactory;
    }

    [Fact]
    public async Task Problem_WithAllFields_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        
        var problem = Problem.Create(
            "Payment Processing Failed",
            "Insufficient funds in the account",
            402,
            "https://example.com/errors/payment-failed",
            "/api/payments/123");
        
        problem.Extensions["traceId"] = "trace-xyz";
        problem.Extensions["accountBalance"] = 50.25m;
        problem.Extensions["requiredAmount"] = 100.00m;
        problem.Extensions["errors"] = new Dictionary<string, List<string>>
        {
            ["payment"] = new List<string> { "Insufficient funds", "Daily limit exceeded" },
            ["account"] = new List<string> { "Account on hold" }
        };
        problem.Extensions["metadata"] = new Dictionary<string, string>
        {
            ["customerId"] = "cust-789",
            ["attemptNumber"] = "3"
        };

        // Act
        var echoed = await grain.EchoProblemAsync(problem);

        // Assert
        echoed.Should().NotBeNull();
        echoed.Type.Should().Be(problem.Type);
        echoed.Title.Should().Be(problem.Title);
        echoed.StatusCode.Should().Be(problem.StatusCode);
        echoed.Detail.Should().Be(problem.Detail);
        echoed.Instance.Should().Be(problem.Instance);
        
        echoed.Extensions.Should().NotBeNull();
        echoed.Extensions["traceId"].Should().Be("trace-xyz");
        echoed.Extensions["accountBalance"].Should().Be(50.25m);
        echoed.Extensions["requiredAmount"].Should().Be(100.00m);
        
        var errors = echoed.Extensions["errors"] as Dictionary<string, List<string>>;
        errors.Should().NotBeNull();
        errors!["payment"].Should().Contain("Insufficient funds");
        errors["payment"].Should().Contain("Daily limit exceeded");
        errors["account"].Should().Contain("Account on hold");
        
        var metadata = echoed.Extensions["metadata"] as Dictionary<string, string>;
        metadata.Should().NotBeNull();
        metadata!["customerId"].Should().Be("cust-789");
        metadata["attemptNumber"].Should().Be("3");
    }

    [Fact]
    public async Task Problem_ValidationErrors_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        
        var problem = Problem.Validation(
            ("firstName", "First name is required"),
            ("lastName", "Last name is required"),
            ("email", "Email format is invalid"),
            ("age", "Age must be between 18 and 120"),
            ("password", "Password must be at least 8 characters")
        );

        // Act
        var echoed = await grain.EchoProblemAsync(problem);

        // Assert
        echoed.Should().NotBeNull();
        echoed.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.1");
        echoed.Title.Should().Be("Validation Failed");
        echoed.StatusCode.Should().Be(400);
        echoed.Detail.Should().Be("One or more validation errors occurred.");
        
        var errors = echoed.GetValidationErrors();
        errors.Should().NotBeNull();
        errors.Should().HaveCount(5);
        errors!["firstName"].Should().Contain("First name is required");
        errors["lastName"].Should().Contain("Last name is required");
        errors["email"].Should().Contain("Email format is invalid");
        errors["age"].Should().Contain("Age must be between 18 and 120");
        errors["password"].Should().Contain("Password must be at least 8 characters");
    }

    [Fact]
    public async Task Problem_StandardTypes_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        
        var testCases = new[]
        {
            Problem.FromStatusCode(HttpStatusCode.BadRequest, "Invalid input"),
            Problem.FromStatusCode(HttpStatusCode.Unauthorized, "Authentication required"),
            Problem.FromStatusCode(HttpStatusCode.Forbidden, "Access denied"),
            Problem.FromStatusCode(HttpStatusCode.NotFound, "Resource not found"),
            Problem.FromStatusCode(HttpStatusCode.Conflict, "Resource conflict"),
            Problem.FromStatusCode(HttpStatusCode.UnprocessableEntity, "Cannot process entity"),
            Problem.FromStatusCode(HttpStatusCode.InternalServerError, "Server error occurred"),
            Problem.FromStatusCode(HttpStatusCode.ServiceUnavailable, "Service temporarily down")
        };

        foreach (var problem in testCases)
        {
            // Act
            var echoed = await grain.EchoProblemAsync(problem);

            // Assert
            echoed.Should().NotBeNull();
            echoed.Type.Should().Be(problem.Type);
            echoed.Title.Should().Be(problem.Title);
            echoed.StatusCode.Should().Be(problem.StatusCode);
            echoed.Detail.Should().Be(problem.Detail);
            echoed.Instance.Should().Be(problem.Instance);
        }
    }

    [Fact]
    public async Task Problem_WithCustomExtensions_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        
        var problem = Problem.FromStatusCode(HttpStatusCode.BadRequest, "Validation failed");
        problem.Extensions["correlationId"] = Guid.NewGuid().ToString();
        problem.Extensions["timestamp"] = DateTimeOffset.UtcNow;
        problem.Extensions["retryAfter"] = 60;
        problem.Extensions["supportContact"] = "support@example.com";
        problem.Extensions["errorCodes"] = new[] { "ERR001", "ERR002", "ERR003" };
        problem.Extensions["nested"] = new Dictionary<string, object>
        {
            ["level1"] = new Dictionary<string, object>
            {
                ["level2"] = "deep value"
            }
        };

        // Act
        var echoed = await grain.EchoProblemAsync(problem);

        // Assert
        echoed.Should().NotBeNull();
        echoed.Extensions.Should().NotBeNull();
        echoed.Extensions.Should().HaveCount(6);
        
        echoed.Extensions["correlationId"].Should().NotBeNull();
        echoed.Extensions["timestamp"].Should().NotBeNull();
        echoed.Extensions["retryAfter"].Should().Be(60);
        echoed.Extensions["supportContact"].Should().Be("support@example.com");
        
        var errorCodes = echoed.Extensions["errorCodes"] as string[];
        errorCodes.Should().NotBeNull();
        errorCodes.Should().BeEquivalentTo(new[] { "ERR001", "ERR002", "ERR003" });
        
        var nested = echoed.Extensions["nested"] as Dictionary<string, object>;
        nested.Should().NotBeNull();
        var level1 = nested!["level1"] as Dictionary<string, object>;
        level1.Should().NotBeNull();
        level1!["level2"].Should().Be("deep value");
    }

    [Fact]
    public async Task Problem_MinimalFields_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        
        var problem = Problem.Create("Internal Error", "An error occurred", 500);

        // Act
        var echoed = await grain.EchoProblemAsync(problem);

        // Assert
        echoed.Should().NotBeNull();
        echoed.StatusCode.Should().Be(500);
        echoed.Title.Should().Be("Internal Error");
        echoed.Type.Should().Be("https://httpstatuses.io/500");
        echoed.Detail.Should().Be("An error occurred");
        echoed.Instance.Should().BeNull();
        echoed.Extensions.Should().NotBeNull();
        echoed.Extensions.Should().BeEmpty();
    }

    [Fact]
    public async Task Problem_WithErrorCode_ShouldSerializeCorrectly()
    {
        // Arrange
        var grain = _grainFactory.GetGrain<ITestSerializationGrain>(Guid.NewGuid());
        
        var problem = Problem.FromStatusCode(HttpStatusCode.BadRequest, "Invalid request");
        problem.ErrorCode = "APP_ERROR_001";

        // Act
        var echoed = await grain.EchoProblemAsync(problem);

        // Assert
        echoed.Should().NotBeNull();
        echoed.ErrorCode.Should().Be("APP_ERROR_001");
        echoed.StatusCode.Should().Be(400);
        echoed.Title.Should().Be("BadRequest");
        echoed.Detail.Should().Be("Invalid request");
    }
}