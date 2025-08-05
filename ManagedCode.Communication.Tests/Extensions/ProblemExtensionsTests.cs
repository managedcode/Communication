using System.Collections.Generic;
using FluentAssertions;
using ManagedCode.Communication.Extensions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ManagedCode.Communication.Tests.Extensions;

public class ProblemExtensionsTests
{
    [Fact]
    public void ToProblemDetails_ShouldConvertProblemToProblemDetails()
    {
        // Arrange
        var problem = Problem.Create("https://httpstatuses.io/400", "Bad Request", 400, "Invalid input", "/api/users");
        problem.Extensions["traceId"] = "12345";
        problem.Extensions["timestamp"] = "2024-01-01T00:00:00Z";

        // Act
        var problemDetails = problem.ToProblemDetails();

        // Assert
        problemDetails.Should().NotBeNull();
        problemDetails.Type.Should().Be("https://httpstatuses.io/400");
        problemDetails.Title.Should().Be("Bad Request");
        problemDetails.Status.Should().Be(400);
        problemDetails.Detail.Should().Be("Invalid input");
        problemDetails.Instance.Should().Be("/api/users");
        problemDetails.Extensions.Should().ContainKey("traceId");
        problemDetails.Extensions["traceId"].Should().Be("12345");
        problemDetails.Extensions.Should().ContainKey("timestamp");
        problemDetails.Extensions["timestamp"].Should().Be("2024-01-01T00:00:00Z");
    }

    [Fact]
    public void ToProblemDetails_WithZeroStatusCode_ShouldSetStatusToNull()
    {
        // Arrange
        var problem = new Problem
        {
            Type = "https://example.com/error",
            Title = "Error",
            StatusCode = 0,
            Detail = "Something went wrong"
        };

        // Act
        var problemDetails = problem.ToProblemDetails();

        // Assert
        problemDetails.Status.Should().BeNull();
    }

    [Fact]
    public void FromProblemDetails_ShouldConvertProblemDetailsToProblem()
    {
        // Arrange
        var problemDetails = new ProblemDetails
        {
            Type = "https://httpstatuses.io/404",
            Title = "Not Found",
            Status = 404,
            Detail = "Resource not found",
            Instance = "/api/items/123"
        };
        problemDetails.Extensions["correlationId"] = "abc-123";
        problemDetails.Extensions["userId"] = 42;

        // Act
        var problem = ProblemExtensions.FromProblemDetails(problemDetails);

        // Assert
        problem.Should().NotBeNull();
        problem.Type.Should().Be("https://httpstatuses.io/404");
        problem.Title.Should().Be("Not Found");
        problem.StatusCode.Should().Be(404);
        problem.Detail.Should().Be("Resource not found");
        problem.Instance.Should().Be("/api/items/123");
        problem.Extensions.Should().ContainKey("correlationId");
        problem.Extensions["correlationId"].Should().Be("abc-123");
        problem.Extensions.Should().ContainKey("userId");
        problem.Extensions["userId"].Should().Be(42);
    }

    [Fact]
    public void FromProblemDetails_WithNullStatus_ShouldSetStatusCodeToZero()
    {
        // Arrange
        var problemDetails = new ProblemDetails
        {
            Type = "https://example.com/error",
            Title = "Error",
            Status = null,
            Detail = "Something went wrong"
        };

        // Act
        var problem = ProblemExtensions.FromProblemDetails(problemDetails);

        // Assert
        problem.StatusCode.Should().Be(0);
    }

    [Fact]
    public void AsProblemDetails_ShouldConvertProblemToProblemDetails()
    {
        // Arrange
        var problem = Problem.Create("type", "title", 400, "detail");

        // Act
        var problemDetails = problem.AsProblemDetails();

        // Assert
        problemDetails.Should().NotBeNull();
        problemDetails.Type.Should().Be("type");
        problemDetails.Title.Should().Be("title");
        problemDetails.Status.Should().Be(400);
        problemDetails.Detail.Should().Be("detail");
    }

    [Fact]
    public void AsProblem_ShouldConvertProblemDetailsToProblem()
    {
        // Arrange
        var problemDetails = new ProblemDetails
        {
            Type = "type",
            Title = "title",
            Status = 500,
            Detail = "detail"
        };

        // Act
        var problem = problemDetails.AsProblem();

        // Assert
        problem.Should().NotBeNull();
        problem.Type.Should().Be("type");
        problem.Title.Should().Be("title");
        problem.StatusCode.Should().Be(500);
        problem.Detail.Should().Be("detail");
    }

    [Fact]
    public void ToFailedResult_FromProblemDetails_ShouldCreateFailedResult()
    {
        // Arrange
        var problemDetails = new ProblemDetails
        {
            Type = "https://httpstatuses.io/400",
            Title = "Validation Error",
            Status = 400,
            Detail = "Invalid input data"
        };

        // Act
        var result = problemDetails.ToFailedResult();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Problem.Should().NotBeNull();
        result.Problem!.Type.Should().Be("https://httpstatuses.io/400");
        result.Problem.Title.Should().Be("Validation Error");
        result.Problem.StatusCode.Should().Be(400);
        result.Problem.Detail.Should().Be("Invalid input data");
    }

    [Fact]
    public void ToFailedResultT_FromProblemDetails_ShouldCreateFailedResultT()
    {
        // Arrange
        var problemDetails = new ProblemDetails
        {
            Type = "https://httpstatuses.io/404",
            Title = "Not Found",
            Status = 404,
            Detail = "User not found"
        };

        // Act
        var result = problemDetails.ToFailedResult<string>();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Problem.Should().NotBeNull();
        result.Problem!.Type.Should().Be("https://httpstatuses.io/404");
        result.Problem.Title.Should().Be("Not Found");
        result.Problem.StatusCode.Should().Be(404);
        result.Problem.Detail.Should().Be("User not found");
    }

    [Fact]
    public void ToFailedResult_FromProblem_ShouldCreateFailedResult()
    {
        // Arrange
        var problem = Problem.Create("https://httpstatuses.io/500", "Server Error", 500, "Internal error occurred");

        // Act
        var result = problem.ToFailedResult();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Problem.Should().Be(problem);
    }

    [Fact]
    public void ToFailedResultT_FromProblem_ShouldCreateFailedResultT()
    {
        // Arrange
        var problem = Problem.Create("https://httpstatuses.io/403", "Forbidden", 403, "Access denied");

        // Act
        var result = problem.ToFailedResult<int>();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().Be(default(int));
        result.Problem.Should().Be(problem);
    }

    [Fact]
    public void RoundTrip_ProblemToProblemDetailsAndBack_ShouldPreserveAllData()
    {
        // Arrange
        var originalProblem = Problem.Create("https://httpstatuses.io/409", "Conflict", 409, "Resource conflict", "/api/resource/123");
        originalProblem.Extensions["error_code"] = "RESOURCE_LOCKED";
        originalProblem.Extensions["retry_after"] = 30;
        originalProblem.Extensions["nested"] = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        var problemDetails = originalProblem.ToProblemDetails();
        var convertedProblem = problemDetails.AsProblem();

        // Assert
        convertedProblem.Type.Should().Be(originalProblem.Type);
        convertedProblem.Title.Should().Be(originalProblem.Title);
        convertedProblem.StatusCode.Should().Be(originalProblem.StatusCode);
        convertedProblem.Detail.Should().Be(originalProblem.Detail);
        convertedProblem.Instance.Should().Be(originalProblem.Instance);
        convertedProblem.Extensions.Should().BeEquivalentTo(originalProblem.Extensions);
    }

    [Fact]
    public void RoundTrip_ProblemDetailsWithNullValues_ShouldHandleGracefully()
    {
        // Arrange
        var problemDetails = new ProblemDetails
        {
            Type = null,
            Title = null,
            Status = null,
            Detail = null,
            Instance = null
        };

        // Act
        var problem = problemDetails.AsProblem();
        var convertedProblemDetails = problem.AsProblemDetails();

        // Assert
        problem.Type.Should().BeNull();
        problem.Title.Should().BeNull();
        problem.StatusCode.Should().Be(0);
        problem.Detail.Should().BeNull();
        problem.Instance.Should().BeNull();
        
        convertedProblemDetails.Type.Should().BeNull();
        convertedProblemDetails.Title.Should().BeNull();
        convertedProblemDetails.Status.Should().BeNull();
        convertedProblemDetails.Detail.Should().BeNull();
        convertedProblemDetails.Instance.Should().BeNull();
    }

    [Fact]
    public void ToFailedResult_WithComplexExtensions_ShouldPreserveAllData()
    {
        // Arrange
        var problemDetails = new ProblemDetails
        {
            Type = "https://httpstatuses.io/422",
            Title = "Unprocessable Entity",
            Status = 422,
            Detail = "Validation failed"
        };
        problemDetails.Extensions["errors"] = new Dictionary<string, List<string>>
        {
            ["email"] = new List<string> { "Invalid format", "Already exists" },
            ["password"] = new List<string> { "Too short" }
        };

        // Act
        var result = problemDetails.ToFailedResult();

        // Assert
        result.Problem!.Extensions.Should().ContainKey("errors");
        var errors = result.Problem.Extensions["errors"] as Dictionary<string, List<string>>;
        errors.Should().NotBeNull();
        errors!["email"].Should().Contain("Invalid format");
        errors["email"].Should().Contain("Already exists");
        errors["password"].Should().Contain("Too short");
    }
}