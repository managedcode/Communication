using System;
using System.Net;
using FluentAssertions;
using Xunit;

namespace ManagedCode.Communication.Tests.Results;

public class ProblemCreationExtensionsTests
{
    [Fact]
    public void ToProblem_FromException_ShouldCreateProblemWithDefaultStatusCode()
    {
        // Arrange
        var exception = new InvalidOperationException("Operation not allowed");

        // Act
        var problem = exception.ToProblem();

        // Assert
        problem.Should().NotBeNull();
        problem.Type.Should().Be("https://httpstatuses.io/500");
        problem.Title.Should().Be("InvalidOperationException");
        problem.Detail.Should().Be("Operation not allowed");
        problem.StatusCode.Should().Be(500);
        problem.ErrorCode.Should().Be("System.InvalidOperationException");
    }

    [Fact]
    public void ToProblem_FromException_WithCustomStatusCode_ShouldUseProvidedStatusCode()
    {
        // Arrange
        var exception = new ArgumentException("Invalid argument");

        // Act
        var problem = exception.ToProblem(400);

        // Assert
        problem.StatusCode.Should().Be(400);
        problem.Type.Should().Be("https://httpstatuses.io/400");
    }

    [Fact]
    public void ToProblem_FromException_WithHttpStatusCode_ShouldUseProvidedStatusCode()
    {
        // Arrange
        var exception = new UnauthorizedAccessException("Access denied");

        // Act
        var problem = exception.ToProblem(HttpStatusCode.Forbidden);

        // Assert
        problem.StatusCode.Should().Be(403);
        problem.Type.Should().Be("https://httpstatuses.io/403");
        problem.Title.Should().Be("UnauthorizedAccessException");
        problem.Detail.Should().Be("Access denied");
    }

    [Fact]
    public void ToProblem_FromEnum_WithDefaultParameters_ShouldCreateProblem()
    {
        // Act
        var problem = TestError.InvalidInput.ToProblem();

        // Assert
        problem.Should().NotBeNull();
        problem.Type.Should().Be("https://httpstatuses.io/400");
        problem.Title.Should().Be("InvalidInput");
        problem.Detail.Should().Be("An error occurred: InvalidInput");
        problem.StatusCode.Should().Be(400);
        problem.ErrorCode.Should().Be("InvalidInput");
    }

    [Fact]
    public void ToProblem_FromEnum_WithCustomDetail_ShouldUseProvidedDetail()
    {
        // Act
        var problem = TestError.ResourceLocked.ToProblem("The resource is locked by another user");

        // Assert
        problem.Detail.Should().Be("The resource is locked by another user");
        problem.Title.Should().Be("ResourceLocked");
    }

    [Fact]
    public void ToProblem_FromEnum_WithCustomStatusCode_ShouldUseProvidedStatusCode()
    {
        // Act
        var problem = TestError.ResourceLocked.ToProblem("Resource locked", 423);

        // Assert
        problem.StatusCode.Should().Be(423);
        problem.Type.Should().Be("https://httpstatuses.io/423");
        problem.Detail.Should().Be("Resource locked");
    }

    [Fact]
    public void ToProblem_FromEnum_WithHttpStatusCode_ShouldUseProvidedStatusCode()
    {
        // Act
        var problem = TestError.InvalidInput.ToProblem("Invalid input data", HttpStatusCode.UnprocessableEntity);

        // Assert
        problem.StatusCode.Should().Be(422);
        problem.Type.Should().Be("https://httpstatuses.io/422");
        problem.Detail.Should().Be("Invalid input data");
        problem.ErrorCode.Should().Be("InvalidInput");
    }

    [Fact]
    public void ToException_FromProblem_ShouldCreateProblemException()
    {
        // Arrange
        var problem = Problem.Create("https://httpstatuses.io/409", "Conflict", 409, "Resource conflict detected");

        // Act
        var exception = problem.ToException();

        // Assert
        exception.Should().BeOfType<ProblemException>();
        var problemException = (ProblemException)exception;
        problemException.Problem.Should().Be(problem);
        problemException.StatusCode.Should().Be(409);
        problemException.Title.Should().Be("Conflict");
        problemException.Detail.Should().Be("Resource conflict detected");
    }

    [Fact]
    public void ToProblem_FromExceptionWithData_ShouldIncludeDataInExtensions()
    {
        // Arrange
        var exception = new InvalidOperationException("Operation failed");
        exception.Data["UserId"] = 123;
        exception.Data["CorrelationId"] = "abc-123";

        // Act
        var problem = exception.ToProblem();

        // Assert
        problem.Extensions.Should().ContainKey("exception.UserId");
        problem.Extensions["exception.UserId"].Should().Be(123);
        problem.Extensions.Should().ContainKey("exception.CorrelationId");
        problem.Extensions["exception.CorrelationId"].Should().Be("abc-123");
    }

    [Fact]
    public void ToException_PreservesAllProblemDetails()
    {
        // Arrange
        var problem = Problem.Validation(("field1", "Error 1"), ("field2", "Error 2"));
        problem.Extensions["customData"] = "customValue";

        // Act
        var exception = problem.ToException();

        // Assert
        var problemException = (ProblemException)exception;
        problemException.IsValidationProblem.Should().BeTrue();
        problemException.ValidationErrors.Should().NotBeNull();
        problemException.ValidationErrors!["field1"].Should().Contain("Error 1");
        problemException.ValidationErrors["field2"].Should().Contain("Error 2");
        problemException.Data.Contains($"{nameof(Problem)}.{nameof(problem.Extensions)}.customData").Should().BeTrue();
    }
}