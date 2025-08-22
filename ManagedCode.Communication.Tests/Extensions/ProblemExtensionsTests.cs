using System.Collections.Generic;
using FluentAssertions;
using ManagedCode.Communication.AspNetCore;
using ManagedCode.Communication.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ManagedCode.Communication.Tests.Extensions;

public class ProblemExtensionsTests
{
    [Fact]
    public void ToProblemDetails_ShouldConvertProblemToProblemDetails()
    {
        // Arrange
        var problem = Problem.Create("Bad Request", "Invalid input", 400, "https://httpstatuses.io/400", "/api/users");
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
        var problem = Problem.Create("Error", "Something went wrong", 0, "https://example.com/error");

        // Act
        var problemDetails = problem.ToProblemDetails();

        // Assert
        problemDetails.Status.Should().BeNull();
    }

    [Fact]
    public void FromProblemDetails_ShouldConvertProblemDetailsToProblem()
    {
        // Arrange
        var problemDetails = ProblemDetailsBuilder.Create(
            "https://httpstatuses.io/404",
            "Not Found",
            404,
            "Resource not found",
            "/api/items/123"
        );
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
        var problem = Problem.Create("title", "detail", 400, "type");

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
        var problemDetails = ProblemDetailsBuilder.Create(
            "type",
            "title",
            500,
            "detail"
        );

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
        var problem = Problem.Create("Server Error", "Internal error occurred", 500, "https://httpstatuses.io/500");

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
        var problem = Problem.Create("Forbidden", "Access denied", 403, "https://httpstatuses.io/403");

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
        var originalProblem = Problem.Create("Conflict", "Resource conflict", 409, "https://httpstatuses.io/409", "/api/resource/123");
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
    }

    [Fact]
    public void ToFailedResult_WithComplexExtensions_ShouldPreserveAllData()
    {
        // Arrange
        var problemDetails = ProblemDetailsBuilder.CreateWithValidationErrors(
            "Unprocessable Entity",
            "Validation failed",
            422,
            ("email", new[] { "Invalid format", "Already exists" }),
            ("password", new[] { "Too short" })
        );

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

    [Fact]
    public void AddInvalidMessage_ShouldAddValidationError()
    {
        // Arrange
        var problem = new Problem();

        // Act
        problem.AddInvalidMessage("email", "Email is required");
        problem.AddInvalidMessage("email", "Email format is invalid");

        // Assert
        problem.InvalidField("email")
            .Should()
            .BeTrue();
        var emailErrors = problem.InvalidFieldError("email");
        emailErrors.Should()
            .Contain("Email is required");
        emailErrors.Should()
            .Contain("Email format is invalid");
    }

    [Fact]
    public void AddInvalidMessage_WithGeneralMessage_ShouldAddToGeneralErrors()
    {
        // Arrange
        var problem = new Problem();

        // Act
        problem.AddInvalidMessage("General error occurred");

        // Assert
        problem.InvalidField("_general")
            .Should()
            .BeTrue();
        var generalErrors = problem.InvalidFieldError("_general");
        generalErrors.Should()
            .Be("General error occurred");
    }
}
