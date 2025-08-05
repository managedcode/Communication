using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace ManagedCode.Communication.Tests.Results;

public class ProblemExceptionTests
{
    [Fact]
    public void Constructor_WithProblem_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var problem = Problem.Create("https://httpstatuses.io/404", "Not Found", 404, "Resource not found", "/api/users/123");

        // Act
        var exception = new ProblemException(problem);

        // Assert
        exception.Problem.Should().Be(problem);
        exception.StatusCode.Should().Be(404);
        exception.Type.Should().Be("https://httpstatuses.io/404");
        exception.Title.Should().Be("Not Found");
        exception.Detail.Should().Be("Resource not found");
        exception.Message.Should().Contain("Not Found");
        exception.Message.Should().Contain("Resource not found");
        exception.Message.Should().Contain("404");
    }

    [Fact]
    public void Constructor_WithBasicDetails_ShouldCreateProblemAndSetProperties()
    {
        // Act
        var exception = new ProblemException("Server Error", "Database connection failed", 503);

        // Assert
        exception.StatusCode.Should().Be(503);
        exception.Title.Should().Be("Server Error");
        exception.Detail.Should().Be("Database connection failed");
        exception.Problem.Should().NotBeNull();
        exception.Problem.StatusCode.Should().Be(503);
        exception.Problem.Title.Should().Be("Server Error");
        exception.Problem.Detail.Should().Be("Database connection failed");
    }

    [Fact]
    public void Constructor_WithTitleOnly_ShouldUseDefaultStatusCodeAndDuplicateDetail()
    {
        // Act
        var exception = new ProblemException("Bad Request");

        // Assert
        exception.StatusCode.Should().Be(500);
        exception.Title.Should().Be("Bad Request");
        exception.Detail.Should().Be("Bad Request");
    }

    [Fact]
    public void Constructor_WithInnerException_ShouldCreateProblemFromException()
    {
        // Arrange
        var innerException = new ArgumentNullException("paramName", "Parameter cannot be null");

        // Act
        var exception = new ProblemException(innerException);

        // Assert
        exception.Problem.Should().NotBeNull();
        exception.Problem.Title.Should().Be("ArgumentNullException");
        exception.Problem.Detail.Should().Contain("Parameter cannot be null");
        exception.Problem.ErrorCode.Should().Be("System.ArgumentNullException");
    }

    [Fact]
    public void Constructor_WithProblemContainingErrorCode_ShouldPopulateData()
    {
        // Arrange
        var problem = Problem.FromEnum(TestError.InvalidInput, "Invalid data provided");

        // Act
        var exception = new ProblemException(problem);

        // Assert
        exception.ErrorCode.Should().Be("InvalidInput");
        exception.Data.Contains($"{nameof(Problem)}.{nameof(problem.ErrorCode)}").Should().BeTrue();
        exception.Data[$"{nameof(Problem)}.{nameof(problem.ErrorCode)}"].Should().Be("InvalidInput");
    }

    [Fact]
    public void Constructor_WithValidationProblem_ShouldPopulateValidationErrors()
    {
        // Arrange
        var problem = Problem.Validation(("email", "Email is required"), ("age", "Age must be positive"));

        // Act
        var exception = new ProblemException(problem);

        // Assert
        exception.IsValidationProblem.Should().BeTrue();
        exception.ValidationErrors.Should().NotBeNull();
        exception.ValidationErrors!["email"].Should().Contain("Email is required");
        exception.ValidationErrors["age"].Should().Contain("Age must be positive");
        
        exception.Data.Contains($"{nameof(Problem)}.ValidationError.email").Should().BeTrue();
        exception.Data[$"{nameof(Problem)}.ValidationError.email"].Should().Be("Email is required");
        exception.Data.Contains($"{nameof(Problem)}.ValidationError.age").Should().BeTrue();
        exception.Data[$"{nameof(Problem)}.ValidationError.age"].Should().Be("Age must be positive");
        
        exception.Message.Should().Contain("Validation failed");
        exception.Message.Should().Contain("email: Email is required");
        exception.Message.Should().Contain("age: Age must be positive");
    }

    [Fact]
    public void Constructor_WithProblemExtensions_ShouldPopulateData()
    {
        // Arrange
        var problem = Problem.Create("type", "title", 400, "detail");
        problem.Extensions["customKey"] = "customValue";
        problem.Extensions["retryAfter"] = 60;

        // Act
        var exception = new ProblemException(problem);

        // Assert
        exception.Data.Contains($"{nameof(Problem)}.{nameof(problem.Extensions)}.customKey").Should().BeTrue();
        exception.Data[$"{nameof(Problem)}.{nameof(problem.Extensions)}.customKey"].Should().Be("customValue");
        exception.Data.Contains($"{nameof(Problem)}.{nameof(problem.Extensions)}.retryAfter").Should().BeTrue();
        exception.Data[$"{nameof(Problem)}.{nameof(problem.Extensions)}.retryAfter"].Should().Be(60);
    }

    [Fact]
    public void Constructor_WithProblemHavingNullProperties_ShouldHandleGracefully()
    {
        // Arrange
        var problem = new Problem
        {
            Type = null,
            Title = null,
            StatusCode = 0,
            Detail = null,
            Instance = null,
            Extensions = new Dictionary<string, object?>()
        };

        // Act
        var exception = new ProblemException(problem);

        // Assert
        exception.Type.Should().BeNull();
        exception.Title.Should().BeNull();
        exception.Detail.Should().BeNull();
        exception.StatusCode.Should().Be(0);
        exception.Message.Should().Be("An error occurred");
    }

    [Fact]
    public void FromProblem_ShouldCreateProblemException()
    {
        // Arrange
        var problem = Problem.Create("https://httpstatuses.io/409", "Conflict", 409, "Resource conflict");

        // Act
        var exception = ProblemException.FromProblem(problem);

        // Assert
        exception.Should().NotBeNull();
        exception.Problem.Should().Be(problem);
        exception.StatusCode.Should().Be(409);
    }

    [Fact]
    public void IsValidationProblem_WithNonValidationProblem_ShouldReturnFalse()
    {
        // Arrange
        var problem = Problem.FromStatusCode(System.Net.HttpStatusCode.InternalServerError);
        var exception = new ProblemException(problem);

        // Act & Assert
        exception.IsValidationProblem.Should().BeFalse();
    }

    [Fact]
    public void ValidationErrors_WithNonValidationProblem_ShouldReturnNull()
    {
        // Arrange
        var problem = Problem.FromStatusCode(System.Net.HttpStatusCode.NotFound);
        var exception = new ProblemException(problem);

        // Act & Assert
        exception.ValidationErrors.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithProblemContainingMultipleValidationErrorsPerField_ShouldJoinWithSemicolon()
    {
        // Arrange
        var problem = Problem.Validation(
            ("email", "Email is required"), 
            ("email", "Email format is invalid"),
            ("email", "Email domain is not allowed")
        );

        // Act
        var exception = new ProblemException(problem);

        // Assert
        exception.Data[$"{nameof(Problem)}.ValidationError.email"].Should().Be("Email is required; Email format is invalid; Email domain is not allowed");
    }

    [Fact]
    public void Message_WithErrorCode_ShouldIncludeErrorCodeInBrackets()
    {
        // Arrange
        var problem = Problem.FromEnum(TestError.ResourceLocked, "Resource is currently locked", 423);

        // Act
        var exception = new ProblemException(problem);

        // Assert
        exception.Message.Should().Contain("[ResourceLocked]");
    }

    [Fact]
    public void Constructor_AllDataFieldsShouldUseNameof()
    {
        // Arrange
        var problem = Problem.Create("type", "title", 400, "detail", "instance");
        problem.ErrorCode = "TEST001";

        // Act
        var exception = new ProblemException(problem);

        // Assert
        exception.Data.Contains($"{nameof(Problem)}.{nameof(problem.Type)}").Should().BeTrue();
        exception.Data.Contains($"{nameof(Problem)}.{nameof(problem.Title)}").Should().BeTrue();
        exception.Data.Contains($"{nameof(Problem)}.{nameof(problem.StatusCode)}").Should().BeTrue();
        exception.Data.Contains($"{nameof(Problem)}.{nameof(problem.Detail)}").Should().BeTrue();
        exception.Data.Contains($"{nameof(Problem)}.{nameof(problem.Instance)}").Should().BeTrue();
        exception.Data.Contains($"{nameof(Problem)}.{nameof(problem.ErrorCode)}").Should().BeTrue();
    }
}