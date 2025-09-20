using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Shouldly;
using ManagedCode.Communication.Tests.Orleans.Fixtures;
using ManagedCode.Communication.Tests.Orleans.Grains;
using Orleans;
using Xunit;
using ManagedCode.Communication.Tests.TestHelpers;

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
        echoed.ShouldNotBeNull();
        echoed.Type.ShouldBe(problem.Type);
        echoed.Title.ShouldBe(problem.Title);
        echoed.StatusCode.ShouldBe(problem.StatusCode);
        echoed.Detail.ShouldBe(problem.Detail);
        echoed.Instance.ShouldBe(problem.Instance);
        
        echoed.Extensions.ShouldNotBeNull();
        echoed.Extensions["traceId"].ShouldBe("trace-xyz");
        echoed.Extensions["accountBalance"].ShouldBe(50.25m);
        echoed.Extensions["requiredAmount"].ShouldBe(100.00m);
        
        var errors = echoed.Extensions["errors"] as Dictionary<string, List<string>>;
        errors.ShouldNotBeNull();
        errors!["payment"].ShouldContain("Insufficient funds");
        errors["payment"].ShouldContain("Daily limit exceeded");
        errors["account"].ShouldContain("Account on hold");
        
        var metadata = echoed.Extensions["metadata"] as Dictionary<string, string>;
        metadata.ShouldNotBeNull();
        metadata!["customerId"].ShouldBe("cust-789");
        metadata["attemptNumber"].ShouldBe("3");
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
        echoed.ShouldNotBeNull();
        echoed.Type.ShouldBe("https://tools.ietf.org/html/rfc7231#section-6.5.1");
        echoed.Title.ShouldBe("Validation Failed");
        echoed.StatusCode.ShouldBe(400);
        echoed.Detail.ShouldBe("One or more validation errors occurred.");
        
        var errors = echoed.GetValidationErrors();
        errors.ShouldNotBeNull();
        errors.ShouldHaveCount(5);
        errors!["firstName"].ShouldContain("First name is required");
        errors["lastName"].ShouldContain("Last name is required");
        errors["email"].ShouldContain("Email format is invalid");
        errors["age"].ShouldContain("Age must be between 18 and 120");
        errors["password"].ShouldContain("Password must be at least 8 characters");
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
            echoed.ShouldNotBeNull();
            echoed.Type.ShouldBe(problem.Type);
            echoed.Title.ShouldBe(problem.Title);
            echoed.StatusCode.ShouldBe(problem.StatusCode);
            echoed.Detail.ShouldBe(problem.Detail);
            echoed.Instance.ShouldBe(problem.Instance);
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
        echoed.ShouldNotBeNull();
        echoed.Extensions.ShouldNotBeNull();
        echoed.Extensions.ShouldHaveCount(6);
        
        echoed.Extensions["correlationId"].ShouldNotBeNull();
        echoed.Extensions["timestamp"].ShouldNotBeNull();
        echoed.Extensions["retryAfter"].ShouldBe(60);
        echoed.Extensions["supportContact"].ShouldBe("support@example.com");
        
        var errorCodes = echoed.Extensions["errorCodes"] as string[];
        errorCodes.ShouldNotBeNull();
        errorCodes.ShouldBeEquivalentTo(new[] { "ERR001", "ERR002", "ERR003" });
        
        var nested = echoed.Extensions["nested"] as Dictionary<string, object>;
        nested.ShouldNotBeNull();
        var level1 = nested!["level1"] as Dictionary<string, object>;
        level1.ShouldNotBeNull();
        level1!["level2"].ShouldBe("deep value");
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
        echoed.ShouldNotBeNull();
        echoed.StatusCode.ShouldBe(500);
        echoed.Title.ShouldBe("Internal Error");
        echoed.Type.ShouldBe("https://httpstatuses.io/500");
        echoed.Detail.ShouldBe("An error occurred");
        echoed.Instance.ShouldBeNull();
        echoed.Extensions.ShouldNotBeNull();
        echoed.Extensions.ShouldBeEmpty();
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
        echoed.ShouldNotBeNull();
        echoed.ErrorCode.ShouldBe("APP_ERROR_001");
        echoed.StatusCode.ShouldBe(400);
        echoed.Title.ShouldBe("BadRequest");
        echoed.Detail.ShouldBe("Invalid request");
    }
}