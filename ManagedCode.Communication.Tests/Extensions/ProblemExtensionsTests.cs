using System.Collections.Generic;
using Shouldly;
using ManagedCode.Communication.AspNetCore;
using ManagedCode.Communication.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using ManagedCode.Communication.Tests.TestHelpers;

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
        problemDetails.ShouldNotBeNull();
        problemDetails.Type.ShouldBe("https://httpstatuses.io/400");
        problemDetails.Title.ShouldBe("Bad Request");
        problemDetails.Status.ShouldBe(400);
        problemDetails.Detail.ShouldBe("Invalid input");
        problemDetails.Instance.ShouldBe("/api/users");
        problemDetails.Extensions.ShouldContainKey("traceId");
        problemDetails.Extensions["traceId"].ShouldBe("12345");
        problemDetails.Extensions.ShouldContainKey("timestamp");
        problemDetails.Extensions["timestamp"].ShouldBe("2024-01-01T00:00:00Z");
    }

    [Fact]
    public void ToProblemDetails_WithZeroStatusCode_ShouldSetStatusToNull()
    {
        // Arrange
        var problem = Problem.Create("Error", "Something went wrong", 0, "https://example.com/error");

        // Act
        var problemDetails = problem.ToProblemDetails();

        // Assert
        problemDetails.Status.ShouldBeNull();
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
        problem.ShouldNotBeNull();
        problem.Type.ShouldBe("https://httpstatuses.io/404");
        problem.Title.ShouldBe("Not Found");
        problem.StatusCode.ShouldBe(404);
        problem.Detail.ShouldBe("Resource not found");
        problem.Instance.ShouldBe("/api/items/123");
        problem.Extensions.ShouldContainKey("correlationId");
        problem.Extensions["correlationId"].ShouldBe("abc-123");
        problem.Extensions.ShouldContainKey("userId");
        problem.Extensions["userId"].ShouldBe(42);
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
        problem.StatusCode.ShouldBe(0);
    }

    [Fact]
    public void AsProblemDetails_ShouldConvertProblemToProblemDetails()
    {
        // Arrange
        var problem = Problem.Create("title", "detail", 400, "type");

        // Act
        var problemDetails = problem.AsProblemDetails();

        // Assert
        problemDetails.ShouldNotBeNull();
        problemDetails.Type.ShouldBe("type");
        problemDetails.Title.ShouldBe("title");
        problemDetails.Status.ShouldBe(400);
        problemDetails.Detail.ShouldBe("detail");
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
        problem.ShouldNotBeNull();
        problem.Type.ShouldBe("type");
        problem.Title.ShouldBe("title");
        problem.StatusCode.ShouldBe(500);
        problem.Detail.ShouldBe("detail");
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
        result.IsFailed.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem!.Type.ShouldBe("https://httpstatuses.io/400");
        result.Problem.Title.ShouldBe("Validation Error");
        result.Problem.StatusCode.ShouldBe(400);
        result.Problem.Detail.ShouldBe("Invalid input data");
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
        result.IsFailed.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.Problem.ShouldNotBeNull();
        result.Problem!.Type.ShouldBe("https://httpstatuses.io/404");
        result.Problem.Title.ShouldBe("Not Found");
        result.Problem.StatusCode.ShouldBe(404);
        result.Problem.Detail.ShouldBe("User not found");
    }

    [Fact]
    public void ToFailedResult_FromProblem_ShouldCreateFailedResult()
    {
        // Arrange
        var problem = Problem.Create("Server Error", "Internal error occurred", 500, "https://httpstatuses.io/500");

        // Act
        var result = problem.ToFailedResult();

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.Problem.ShouldBe(problem);
    }

    [Fact]
    public void ToFailedResultT_FromProblem_ShouldCreateFailedResultT()
    {
        // Arrange
        var problem = Problem.Create("Forbidden", "Access denied", 403, "https://httpstatuses.io/403");

        // Act
        var result = problem.ToFailedResult<int>();

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.Value.ShouldBe(default(int));
        result.Problem.ShouldBe(problem);
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
        convertedProblem.Type.ShouldBe(originalProblem.Type);
        convertedProblem.Title.ShouldBe(originalProblem.Title);
        convertedProblem.StatusCode.ShouldBe(originalProblem.StatusCode);
        convertedProblem.Detail.ShouldBe(originalProblem.Detail);
        convertedProblem.Instance.ShouldBe(originalProblem.Instance);
        convertedProblem.Extensions.ShouldBeEquivalentTo(originalProblem.Extensions);
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
        problem.Type.ShouldBeNull();
        problem.Title.ShouldBeNull();
        problem.StatusCode.ShouldBe(0);
        problem.Detail.ShouldBeNull();
        problem.Instance.ShouldBeNull();
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
        result.Problem!.Extensions.ShouldContainKey("errors");
        var errors = result.Problem.Extensions["errors"] as Dictionary<string, List<string>>;
        errors.ShouldNotBeNull();
        errors!["email"].ShouldContain("Invalid format");
        errors["email"].ShouldContain("Already exists");
        errors["password"].ShouldContain("Too short");
    }

    [Fact]
    public void AddInvalidMessage_ShouldAddValidationError()
    {
        // Arrange
        var problem = new Problem();

        // Act
        problem.AddValidationError("email", "Email is required");
        problem.AddValidationError("email", "Email format is invalid");

        // Assert
        problem.InvalidField("email")
            .ShouldBeTrue();
        var emailErrors = problem.InvalidFieldError("email");
        emailErrors.ShouldContain("Email is required");
        emailErrors.ShouldContain("Email format is invalid");
    }

    [Fact]
    public void AddInvalidMessage_WithGeneralMessage_ShouldAddToGeneralErrors()
    {
        // Arrange
        var problem = new Problem();

        // Act
        problem.AddValidationError("General error occurred");

        // Assert
        problem.InvalidField("_general")
            .ShouldBeTrue();
        var generalErrors = problem.InvalidFieldError("_general");
        generalErrors.ShouldBe("General error occurred");
    }
}
