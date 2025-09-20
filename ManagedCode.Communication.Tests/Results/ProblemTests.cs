using System;
using System.Collections.Generic;
using System.Net;
using Shouldly;
using ManagedCode.Communication.Constants;
using Xunit;
using ManagedCode.Communication.Tests.TestHelpers;

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
        var problem = Problem.Create(title, detail, statusCode, type, instance);

        // Assert
        problem.Type
            .ShouldBe(type);
        problem.Title
            .ShouldBe(title);
        problem.StatusCode
            .ShouldBe(statusCode);
        problem.Detail
            .ShouldBe(detail);
        problem.Instance
            .ShouldBe(instance);
        problem.Extensions
            .ShouldNotBeNull();
    }

    [Fact]
    public void FromStatusCode_ShouldCreateProblemFromHttpStatusCode()
    {
        // Act
        var problem = Problem.FromStatusCode(HttpStatusCode.NotFound, "User not found");

        // Assert
        problem.Type
            .ShouldBe("https://httpstatuses.io/404");
        problem.Title
            .ShouldBe("NotFound");
        problem.StatusCode
            .ShouldBe(404);
        problem.Detail
            .ShouldBe("User not found");
    }

    [Fact]
    public void FromEnum_ShouldCreateProblemFromEnum()
    {
        // Act
        var problem = Problem.FromEnum(TestError.InvalidInput, "The input provided is not valid", 422);

        // Assert
        problem.Type
            .ShouldBe("https://httpstatuses.io/422");
        problem.Title
            .ShouldBe("InvalidInput");
        problem.StatusCode
            .ShouldBe(422);
        problem.Detail
            .ShouldBe("The input provided is not valid");
        problem.ErrorCode
            .ShouldBe("InvalidInput");
        problem.Extensions[ProblemConstants.ExtensionKeys.ErrorType]
            .ShouldBe("TestError");
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
            .ShouldBe("https://httpstatuses.io/500");
        problem.Title
            .ShouldBe("InvalidOperationException");
        problem.Detail
            .ShouldBe("This operation is not allowed");
        problem.StatusCode
            .ShouldBe(500);
        problem.ErrorCode
            .ShouldBe("System.InvalidOperationException");
        problem.Extensions[$"{ProblemConstants.ExtensionKeys.ExceptionDataPrefix}CustomKey"]
            .ShouldBe("CustomValue");
    }

    [Fact]
    public void Validation_ShouldCreateValidationProblem()
    {
        // Act
        var problem = Problem.Validation(("email", "Email is required"), ("email", "Email format is invalid"), ("age", "Age must be greater than 0"));

        // Assert
        problem.Type
            .ShouldBe("https://tools.ietf.org/html/rfc7231#section-6.5.1");
        problem.Title
            .ShouldBe("Validation Failed");
        problem.StatusCode
            .ShouldBe(400);
        problem.Detail
            .ShouldBe("One or more validation errors occurred.");

        var validationErrors = problem.GetValidationErrors();
        validationErrors.ShouldNotBeNull();
        validationErrors!["email"]
            .ShouldHaveCount(2);
        validationErrors["email"]
            .ShouldContain("Email is required");
        validationErrors["email"]
            .ShouldContain("Email format is invalid");
        validationErrors["age"]
            .ShouldContain("Age must be greater than 0");
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
            .ShouldBe("TEST_ERROR");
        problem.Extensions[ProblemConstants.ExtensionKeys.ErrorCode]
            .ShouldBe("TEST_ERROR");
    }

    [Fact]
    public void ErrorCode_SetToNull_ShouldRemoveFromExtensions()
    {
        // Arrange
        var problem = Problem.Create("type", "title", 400, "detail");
        problem.ErrorCode = "TEST_ERROR";

        // Act
        problem.ErrorCode = string.Empty;

        // Assert
        problem.ErrorCode
            .ShouldBeEmpty();
        problem.Extensions
            .ShouldNotContainKey(ProblemConstants.ExtensionKeys.ErrorCode);
    }

    [Fact]
    public void HasErrorCode_WithMatchingEnum_ShouldReturnTrue()
    {
        // Arrange
        var problem = Problem.FromEnum(TestError.InvalidInput);

        // Act & Assert
        problem.HasErrorCode(TestError.InvalidInput)
            .ShouldBeTrue();
        problem.HasErrorCode(TestError.ResourceLocked)
            .ShouldBeFalse();
    }

    [Fact]
    public void GetErrorCodeAs_WithMatchingEnum_ShouldReturnEnumValue()
    {
        // Arrange
        var problem = Problem.FromEnum(TestError.InvalidInput);

        // Act
        var errorCode = problem.GetErrorCodeAs<TestError>();

        // Assert
        errorCode.ShouldBe(TestError.InvalidInput);
    }

    [Fact]
    public void GetErrorCodeAs_WithNonMatchingEnum_ShouldReturnNull()
    {
        // Arrange
        var problem = Problem.FromEnum(TestError.InvalidInput);

        // Act
        var errorCode = problem.GetErrorCodeAs<OtherError>();

        // Assert
        errorCode.ShouldBeNull();
    }

    [Fact]
    public void GetValidationErrors_WithValidationProblem_ShouldReturnErrors()
    {
        // Arrange
        var problem = Problem.Validation(("email", "Email is required"), ("age", "Age must be greater than 0"));

        // Act
        var errors = problem.GetValidationErrors();

        // Assert
        errors.ShouldNotBeNull();
        errors.ShouldHaveCount(2);
        errors!["email"]
            .ShouldContain("Email is required");
        errors["age"]
            .ShouldContain("Age must be greater than 0");
    }

    [Fact]
    public void GetValidationErrors_WithNonValidationProblem_ShouldReturnNull()
    {
        // Arrange
        var problem = Problem.FromStatusCode(HttpStatusCode.NotFound);

        // Act
        var errors = problem.GetValidationErrors();

        // Assert
        errors.ShouldBeNull();
    }

    [Fact]
    public void Constructor_ShouldInitializeExtensions()
    {
        // Act
        var problem = Problem.Create("title", "detail", 400, "type");

        // Assert
        problem.Extensions
            .ShouldNotBeNull();
        problem.Extensions
            .ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_WithParameters_ShouldSetProperties()
    {
        // Act
        var problem = Problem.Create("title", "detail", 400, "type", "instance");

        // Assert
        problem.Type
            .ShouldBe("type");
        problem.Title
            .ShouldBe("title");
        problem.StatusCode
            .ShouldBe(400);
        problem.Detail
            .ShouldBe("detail");
        problem.Instance
            .ShouldBe("instance");
        problem.Extensions
            .ShouldNotBeNull();
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
        newProblem.Type.ShouldBe(originalProblem.Type);
        newProblem.Title.ShouldBe(originalProblem.Title);
        newProblem.StatusCode.ShouldBe(originalProblem.StatusCode);
        newProblem.Detail.ShouldBe(originalProblem.Detail);
        newProblem.Instance.ShouldBe(originalProblem.Instance);
        
        newProblem.Extensions.ShouldContainKey("existing");
        newProblem.Extensions["existing"].ShouldBe("value");
        newProblem.Extensions.ShouldContainKey("new");
        newProblem.Extensions["new"].ShouldBe("newValue");
        newProblem.Extensions.ShouldContainKey("another");
        newProblem.Extensions["another"].ShouldBe(123);
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
        newProblem.Extensions["key"].ShouldBe("newValue");
    }

    [Fact]
    public void FromEnum_WithDefaultStatusCode_ShouldUse400()
    {
        // Act
        var problem = Problem.FromEnum(TestError.InvalidInput);

        // Assert
        problem.StatusCode.ShouldBe(400);
        problem.Detail.ShouldBe("An error occurred: InvalidInput");
    }

    [Fact]
    public void HasErrorCode_WithDifferentEnumType_ShouldReturnFalse()
    {
        // Arrange
        var problem = Problem.FromEnum(TestError.InvalidInput);

        // Act & Assert
        problem.HasErrorCode(OtherError.SomethingElse).ShouldBeFalse();
    }

    [Fact]
    public void GetErrorCodeAs_WithNoErrorCode_ShouldReturnNull()
    {
        // Arrange
        var problem = Problem.Create("type", "title", 400, "detail");

        // Act
        var errorCode = problem.GetErrorCodeAs<TestError>();

        // Assert
        errorCode.ShouldBeNull();
    }

    [Fact]
    public void ImplicitOperator_ToProblemException_ShouldCreateException()
    {
        // Arrange
        var problem = Problem.Create("Not Found", "Resource not found", 404, "https://httpstatuses.io/404");

        // Act
        ProblemException exception = problem;

        // Assert
        exception.ShouldNotBeNull();
        exception.Problem.ShouldBe(problem);
        exception.StatusCode.ShouldBe(404);
        exception.Title.ShouldBe("Not Found");
        exception.Detail.ShouldBe("Resource not found");
    }
}