using System;
using System.Collections.Generic;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.Results;

public class ProblemExceptionTests
{
    [Fact]
    public void Constructor_WithProblem_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var problem = Problem.Create("Not Found", "Resource not found", 404, "https://httpstatuses.io/404", "/api/users/123");

        // Act
        var exception = new ProblemException(problem);

        // Assert
        exception.Problem.ShouldBe(problem);
        exception.StatusCode.ShouldBe(404);
        exception.Type.ShouldBe("https://httpstatuses.io/404");
        exception.Title.ShouldBe("Not Found");
        exception.Detail.ShouldBe("Resource not found");
        exception.Message.ShouldContain("Not Found");
        exception.Message.ShouldContain("Resource not found");
        exception.Message.ShouldContain("404");
    }

    [Fact]
    public void Constructor_WithBasicDetails_ShouldCreateProblemAndSetProperties()
    {
        // Act
        var exception = new ProblemException("Server Error", "Database connection failed", 503);

        // Assert
        exception.StatusCode.ShouldBe(503);
        exception.Title.ShouldBe("Server Error");
        exception.Detail.ShouldBe("Database connection failed");
        exception.Problem.ShouldNotBeNull();
        exception.Problem.StatusCode.ShouldBe(503);
        exception.Problem.Title.ShouldBe("Server Error");
        exception.Problem.Detail.ShouldBe("Database connection failed");
    }

    [Fact]
    public void Constructor_WithTitleOnly_ShouldUseDefaultStatusCodeAndDuplicateDetail()
    {
        // Act
        var exception = new ProblemException("Bad Request");

        // Assert
        exception.StatusCode.ShouldBe(500);
        exception.Title.ShouldBe("Bad Request");
        exception.Detail.ShouldBe("Bad Request");
    }

    [Fact]
    public void Constructor_WithInnerException_ShouldCreateProblemFromException()
    {
        // Arrange
        var innerException = new ArgumentNullException("paramName", "Parameter cannot be null");

        // Act
        var exception = new ProblemException(innerException);

        // Assert
        exception.Problem.ShouldNotBeNull();
        exception.Problem!.Title.ShouldBe("ArgumentNullException");
        exception.Problem.Detail!.ShouldContain("Parameter cannot be null");
        exception.Problem.ErrorCode.ShouldBe("System.ArgumentNullException");
    }

    [Fact]
    public void Constructor_WithProblemContainingErrorCode_ShouldPopulateData()
    {
        // Arrange
        var problem = Problem.FromEnum(TestError.InvalidInput, "Invalid data provided");

        // Act
        var exception = new ProblemException(problem);

        // Assert
        exception.ErrorCode.ShouldBe("InvalidInput");
        exception.Data.Contains($"{nameof(Problem)}.{nameof(problem.ErrorCode)}").ShouldBeTrue();
        exception.Data[$"{nameof(Problem)}.{nameof(problem.ErrorCode)}"].ShouldBe("InvalidInput");
    }

    [Fact]
    public void Constructor_WithValidationProblem_ShouldPopulateValidationErrors()
    {
        // Arrange
        var problem = Problem.Validation(("email", "Email is required"), ("age", "Age must be positive"));

        // Act
        var exception = new ProblemException(problem);

        // Assert
        exception.IsValidationProblem.ShouldBeTrue();
        exception.ValidationErrors.ShouldNotBeNull();
        exception.ValidationErrors!["email"].ShouldContain("Email is required");
        exception.ValidationErrors["age"].ShouldContain("Age must be positive");
        
        exception.Data.Contains($"{nameof(Problem)}.ValidationError.email").ShouldBeTrue();
        exception.Data[$"{nameof(Problem)}.ValidationError.email"].ShouldBe("Email is required");
        exception.Data.Contains($"{nameof(Problem)}.ValidationError.age").ShouldBeTrue();
        exception.Data[$"{nameof(Problem)}.ValidationError.age"].ShouldBe("Age must be positive");
        
        exception.Message.ShouldContain("Validation failed");
        exception.Message.ShouldContain("email: Email is required");
        exception.Message.ShouldContain("age: Age must be positive");
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
        exception.Data.Contains($"{nameof(Problem)}.{nameof(problem.Extensions)}.customKey").ShouldBeTrue();
        exception.Data[$"{nameof(Problem)}.{nameof(problem.Extensions)}.customKey"].ShouldBe("customValue");
        exception.Data.Contains($"{nameof(Problem)}.{nameof(problem.Extensions)}.retryAfter").ShouldBeTrue();
        exception.Data[$"{nameof(Problem)}.{nameof(problem.Extensions)}.retryAfter"].ShouldBe(60);
    }

    [Fact]
    public void Constructor_WithProblemHavingNullProperties_ShouldHandleGracefully()
    {
        // Arrange
        var problem = Problem.Create("", "", 0);

        // Act
        var exception = new ProblemException(problem);

        // Assert
        exception.Type.ShouldBeNull();
        exception.Title.ShouldBeNull();
        exception.Detail.ShouldBeNull();
        exception.StatusCode.ShouldBe(0);
        exception.Message.ShouldBe("An error occurred");
    }

    [Fact]
    public void FromProblem_ShouldCreateProblemException()
    {
        // Arrange
        var problem = Problem.Create("https://httpstatuses.io/409", "Conflict", 409, "Resource conflict");

        // Act
        var exception = ProblemException.FromProblem(problem);

        // Assert
        exception.ShouldNotBeNull();
        exception.Problem.ShouldBe(problem);
        exception.StatusCode.ShouldBe(409);
    }

    [Fact]
    public void IsValidationProblem_WithNonValidationProblem_ShouldReturnFalse()
    {
        // Arrange
        var problem = Problem.FromStatusCode(System.Net.HttpStatusCode.InternalServerError);
        var exception = new ProblemException(problem);

        // Act & Assert
        exception.IsValidationProblem.ShouldBeFalse();
    }

    [Fact]
    public void ValidationErrors_WithNonValidationProblem_ShouldReturnNull()
    {
        // Arrange
        var problem = Problem.FromStatusCode(System.Net.HttpStatusCode.NotFound);
        var exception = new ProblemException(problem);

        // Act & Assert
        exception.ValidationErrors.ShouldBeNull();
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
        exception.Data[$"{nameof(Problem)}.ValidationError.email"].ShouldBe("Email is required; Email format is invalid; Email domain is not allowed");
    }

    [Fact]
    public void Message_WithErrorCode_ShouldIncludeErrorCodeInBrackets()
    {
        // Arrange
        var problem = Problem.FromEnum(TestError.ResourceLocked, "Resource is currently locked", 423);

        // Act
        var exception = new ProblemException(problem);

        // Assert
        exception.Message.ShouldContain("[ResourceLocked]");
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
        exception.Data.Contains($"{nameof(Problem)}.{nameof(problem.Type)}").ShouldBeTrue();
        exception.Data.Contains($"{nameof(Problem)}.{nameof(problem.Title)}").ShouldBeTrue();
        exception.Data.Contains($"{nameof(Problem)}.{nameof(problem.StatusCode)}").ShouldBeTrue();
        exception.Data.Contains($"{nameof(Problem)}.{nameof(problem.Detail)}").ShouldBeTrue();
        exception.Data.Contains($"{nameof(Problem)}.{nameof(problem.Instance)}").ShouldBeTrue();
        exception.Data.Contains($"{nameof(Problem)}.{nameof(problem.ErrorCode)}").ShouldBeTrue();
    }

    [Fact]
    public void Constructor_WithEmptyErrorCode_ShouldNotPopulateErrorCodeDataEntry()
    {
        // Arrange
        var problem = Problem.Create("Server Error", "Failed", 500);
        problem.ErrorCode = string.Empty;

        // Act
        var exception = new ProblemException(problem);

        // Assert
        exception.Data.Contains($"{nameof(Problem)}.{nameof(problem.ErrorCode)}").ShouldBeFalse();
    }
}
