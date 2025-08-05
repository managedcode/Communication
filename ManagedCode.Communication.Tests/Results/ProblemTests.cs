using System;
using System.Collections.Generic;
using System.Net;
using FluentAssertions;
using ManagedCode.Communication.Constants;
using Xunit;

namespace ManagedCode.Communication.Tests.Results;

public enum TestError
{
    InvalidInput,
    ResourceLocked
}

public enum OtherError
{
    SomethingElse
}

public class ProblemTests
{
    [Fact]
    public void Create_ShouldCreateProblemWithAllProperties()
    {
        // Arrange
        const string type = "https://httpstatuses.io/400";
        const string title = "Bad Request";
        const int statusCode = 400;
        const string detail = "The request was invalid";
        const string instance = "/api/users/123";

        // Act
        var problem = Problem.Create(type, title, statusCode, detail, instance);

        // Assert
        problem.Type
            .Should()
            .Be(type);
        problem.Title
            .Should()
            .Be(title);
        problem.StatusCode
            .Should()
            .Be(statusCode);
        problem.Detail
            .Should()
            .Be(detail);
        problem.Instance
            .Should()
            .Be(instance);
        problem.Extensions
            .Should()
            .NotBeNull();
    }

    [Fact]
    public void FromStatusCode_ShouldCreateProblemFromHttpStatusCode()
    {
        // Act
        var problem = Problem.FromStatusCode(HttpStatusCode.NotFound, "User not found");

        // Assert
        problem.Type
            .Should()
            .Be("https://httpstatuses.io/404");
        problem.Title
            .Should()
            .Be("NotFound");
        problem.StatusCode
            .Should()
            .Be(404);
        problem.Detail
            .Should()
            .Be("User not found");
    }

    [Fact]
    public void FromEnum_ShouldCreateProblemFromEnum()
    {
        // Act
        var problem = Problem.FromEnum(TestError.InvalidInput, "The input provided is not valid", 422);

        // Assert
        problem.Type
            .Should()
            .Be("https://httpstatuses.io/422");
        problem.Title
            .Should()
            .Be("InvalidInput");
        problem.StatusCode
            .Should()
            .Be(422);
        problem.Detail
            .Should()
            .Be("The input provided is not valid");
        problem.ErrorCode
            .Should()
            .Be("InvalidInput");
        problem.Extensions[ProblemExtensionKeys.ErrorType]
            .Should()
            .Be("TestError");
    }

    [Fact]
    public void FromException_ShouldCreateProblemFromException()
    {
        // Arrange
        var exception = new InvalidOperationException("This operation is not allowed");
        exception.Data["CustomKey"] = "CustomValue";

        // Act
        var problem = Problem.FromException(exception);

        // Assert
        problem.Type
            .Should()
            .Be("https://httpstatuses.io/500");
        problem.Title
            .Should()
            .Be("InvalidOperationException");
        problem.Detail
            .Should()
            .Be("This operation is not allowed");
        problem.StatusCode
            .Should()
            .Be(500);
        problem.ErrorCode
            .Should()
            .Be("System.InvalidOperationException");
        problem.Extensions[$"{ProblemExtensionKeys.ExceptionDataPrefix}CustomKey"]
            .Should()
            .Be("CustomValue");
    }

    [Fact]
    public void Validation_ShouldCreateValidationProblem()
    {
        // Act
        var problem = Problem.Validation(("email", "Email is required"), ("email", "Email format is invalid"), ("age", "Age must be greater than 0"));

        // Assert
        problem.Type
            .Should()
            .Be("https://tools.ietf.org/html/rfc7231#section-6.5.1");
        problem.Title
            .Should()
            .Be("Validation Failed");
        problem.StatusCode
            .Should()
            .Be(400);
        problem.Detail
            .Should()
            .Be("One or more validation errors occurred.");

        var validationErrors = problem.GetValidationErrors();
        validationErrors.Should()
            .NotBeNull();
        validationErrors!["email"]
            .Should()
            .HaveCount(2);
        validationErrors["email"]
            .Should()
            .Contain("Email is required");
        validationErrors["email"]
            .Should()
            .Contain("Email format is invalid");
        validationErrors["age"]
            .Should()
            .Contain("Age must be greater than 0");
    }

    [Fact]
    public void ErrorCode_PropertyShouldWorkCorrectly()
    {
        // Arrange
        var problem = Problem.Create("type", "title", 400, "detail");

        // Act
        problem.ErrorCode = "TEST_ERROR";

        // Assert
        problem.ErrorCode
            .Should()
            .Be("TEST_ERROR");
        problem.Extensions[ProblemExtensionKeys.ErrorCode]
            .Should()
            .Be("TEST_ERROR");
    }

    [Fact]
    public void ErrorCode_SetToNull_ShouldRemoveFromExtensions()
    {
        // Arrange
        var problem = Problem.Create("type", "title", 400, "detail");
        problem.ErrorCode = "TEST_ERROR";

        // Act
        problem.ErrorCode = null;

        // Assert
        problem.ErrorCode
            .Should()
            .BeNull();
        problem.Extensions
            .Should()
            .NotContainKey(ProblemExtensionKeys.ErrorCode);
    }

    [Fact]
    public void HasErrorCode_WithMatchingEnum_ShouldReturnTrue()
    {
        // Arrange
        var problem = Problem.FromEnum(TestError.InvalidInput);

        // Act & Assert
        problem.HasErrorCode(TestError.InvalidInput)
            .Should()
            .BeTrue();
        problem.HasErrorCode(TestError.ResourceLocked)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void GetErrorCodeAs_WithMatchingEnum_ShouldReturnEnumValue()
    {
        // Arrange
        var problem = Problem.FromEnum(TestError.InvalidInput);

        // Act
        var errorCode = problem.GetErrorCodeAs<TestError>();

        // Assert
        errorCode.Should()
            .Be(TestError.InvalidInput);
    }

    [Fact]
    public void GetErrorCodeAs_WithNonMatchingEnum_ShouldReturnNull()
    {
        // Arrange
        var problem = Problem.FromEnum(TestError.InvalidInput);

        // Act
        var errorCode = problem.GetErrorCodeAs<OtherError>();

        // Assert
        errorCode.Should()
            .BeNull();
    }

    [Fact]
    public void GetValidationErrors_WithValidationProblem_ShouldReturnErrors()
    {
        // Arrange
        var problem = Problem.Validation(("email", "Email is required"), ("age", "Age must be greater than 0"));

        // Act
        var errors = problem.GetValidationErrors();

        // Assert
        errors.Should()
            .NotBeNull();
        errors.Should()
            .HaveCount(2);
        errors!["email"]
            .Should()
            .Contain("Email is required");
        errors["age"]
            .Should()
            .Contain("Age must be greater than 0");
    }

    [Fact]
    public void GetValidationErrors_WithNonValidationProblem_ShouldReturnNull()
    {
        // Arrange
        var problem = Problem.FromStatusCode(HttpStatusCode.NotFound);

        // Act
        var errors = problem.GetValidationErrors();

        // Assert
        errors.Should()
            .BeNull();
    }

    [Fact]
    public void Constructor_ShouldInitializeExtensions()
    {
        // Act
        var problem = Problem.Create("type", "title", 400, "detail");

        // Assert
        problem.Extensions
            .Should()
            .NotBeNull();
        problem.Extensions
            .Should()
            .BeEmpty();
    }

    [Fact]
    public void Constructor_WithParameters_ShouldSetProperties()
    {
        // Act
        var problem = Problem.Create("type", "title", 400, "detail", "instance");

        // Assert
        problem.Type
            .Should()
            .Be("type");
        problem.Title
            .Should()
            .Be("title");
        problem.StatusCode
            .Should()
            .Be(400);
        problem.Detail
            .Should()
            .Be("detail");
        problem.Instance
            .Should()
            .Be("instance");
        problem.Extensions
            .Should()
            .NotBeNull();
    }

    [Fact]
    public void WithExtensions_ShouldCreateNewProblemWithAdditionalExtensions()
    {
        // Arrange
        var originalProblem = Problem.Create("type", "title", 400, "detail");
        originalProblem.Extensions["existing"] = "value";
        
        var additionalExtensions = new Dictionary<string, object?>
        {
            ["new"] = "newValue",
            ["another"] = 123
        };

        // Act
        var newProblem = originalProblem.WithExtensions(additionalExtensions);

        // Assert
        newProblem.Type.Should().Be(originalProblem.Type);
        newProblem.Title.Should().Be(originalProblem.Title);
        newProblem.StatusCode.Should().Be(originalProblem.StatusCode);
        newProblem.Detail.Should().Be(originalProblem.Detail);
        newProblem.Instance.Should().Be(originalProblem.Instance);
        
        newProblem.Extensions.Should().ContainKey("existing");
        newProblem.Extensions["existing"].Should().Be("value");
        newProblem.Extensions.Should().ContainKey("new");
        newProblem.Extensions["new"].Should().Be("newValue");
        newProblem.Extensions.Should().ContainKey("another");
        newProblem.Extensions["another"].Should().Be(123);
    }

    [Fact]
    public void WithExtensions_ShouldOverwriteExistingExtensions()
    {
        // Arrange
        var originalProblem = Problem.Create("type", "title", 400, "detail");
        originalProblem.Extensions["key"] = "originalValue";
        
        var additionalExtensions = new Dictionary<string, object?>
        {
            ["key"] = "newValue"
        };

        // Act
        var newProblem = originalProblem.WithExtensions(additionalExtensions);

        // Assert
        newProblem.Extensions["key"].Should().Be("newValue");
    }

    [Fact]
    public void FromEnum_WithDefaultStatusCode_ShouldUse400()
    {
        // Act
        var problem = Problem.FromEnum(TestError.InvalidInput);

        // Assert
        problem.StatusCode.Should().Be(400);
        problem.Detail.Should().Be("An error occurred: InvalidInput");
    }

    [Fact]
    public void HasErrorCode_WithDifferentEnumType_ShouldReturnFalse()
    {
        // Arrange
        var problem = Problem.FromEnum(TestError.InvalidInput);

        // Act & Assert
        problem.HasErrorCode(OtherError.SomethingElse).Should().BeFalse();
    }

    [Fact]
    public void GetErrorCodeAs_WithNoErrorCode_ShouldReturnNull()
    {
        // Arrange
        var problem = Problem.Create("type", "title", 400, "detail");

        // Act
        var errorCode = problem.GetErrorCodeAs<TestError>();

        // Assert
        errorCode.Should().BeNull();
    }

    [Fact]
    public void ImplicitOperator_ToProblemException_ShouldCreateException()
    {
        // Arrange
        var problem = Problem.Create("https://httpstatuses.io/404", "Not Found", 404, "Resource not found");

        // Act
        ProblemException exception = problem;

        // Assert
        exception.Should().NotBeNull();
        exception.Problem.Should().Be(problem);
        exception.StatusCode.Should().Be(404);
        exception.Title.Should().Be("Not Found");
        exception.Detail.Should().Be("Resource not found");
    }
}