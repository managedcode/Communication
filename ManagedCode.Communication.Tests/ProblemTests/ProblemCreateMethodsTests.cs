using System;
using System.Collections.Generic;
using FluentAssertions;
using ManagedCode.Communication.Constants;
using Xunit;

namespace ManagedCode.Communication.Tests.ProblemTests;

public class ProblemCreateMethodsTests
{
    #region Create<TEnum> with Status Code Tests

    [Fact]
    public void Problem_Create_WithEnumAndStatusCode_ShouldCreateProblemWithErrorCode()
    {
        // Act
        var problem = ManagedCode.Communication.Problem.Create(TestError.InvalidInput, 400);

        // Assert
        problem.Should().NotBeNull();
        problem.Type.Should().Be(ProblemConstants.Types.HttpStatus(400));
        problem.Title.Should().Be("InvalidInput");
        problem.StatusCode.Should().Be(400);
        problem.Detail.Should().Be($"{ProblemConstants.Messages.GenericError}: InvalidInput");
        problem.ErrorCode.Should().Be("InvalidInput");
        problem.Extensions.Should().ContainKey(ProblemConstants.ExtensionKeys.ErrorType);
        problem.Extensions[ProblemConstants.ExtensionKeys.ErrorType].Should().Be("TestError");
    }

    [Fact]
    public void Problem_Create_WithEnumDetailAndStatusCode_ShouldCreateProblemWithCustomDetail()
    {
        // Arrange
        const string detail = "Custom error detail";

        // Act
        var problem = ManagedCode.Communication.Problem.Create(TestError.ValidationFailed, detail, 422);

        // Assert
        problem.Should().NotBeNull();
        problem.Type.Should().Be(ProblemConstants.Types.HttpStatus(422));
        problem.Title.Should().Be("ValidationFailed");
        problem.StatusCode.Should().Be(422);
        problem.Detail.Should().Be(detail);
        problem.ErrorCode.Should().Be("ValidationFailed");
        problem.Extensions.Should().ContainKey(ProblemConstants.ExtensionKeys.ErrorType);
        problem.Extensions[ProblemConstants.ExtensionKeys.ErrorType].Should().Be("TestError");
    }

    [Theory]
    [InlineData(400, "BadRequest")]
    [InlineData(404, "NotFound")]
    [InlineData(500, "InternalServerError")]
    public void Problem_Create_WithEnumAndVariousStatusCodes_ShouldSetCorrectType(int statusCode, string enumValue)
    {
        // Arrange
        var errorEnum = Enum.Parse<HttpStatusError>(enumValue);

        // Act
        var problem = ManagedCode.Communication.Problem.Create(errorEnum, statusCode);

        // Assert
        problem.StatusCode.Should().Be(statusCode);
        problem.Type.Should().Be(ProblemConstants.Types.HttpStatus(statusCode));
        problem.ErrorCode.Should().Be(enumValue);
    }

    #endregion

    #region Create from Exception Tests

    [Fact]
    public void Problem_Create_FromException_ShouldCreateProblemWithExceptionDetails()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception message");

        // Act
        var problem = ManagedCode.Communication.Problem.Create(exception);

        // Assert
        problem.Should().NotBeNull();
        problem.Type.Should().Be(ProblemConstants.Types.HttpStatus(500));
        problem.Title.Should().Be("InvalidOperationException");
        problem.Detail.Should().Be("Test exception message");
        problem.StatusCode.Should().Be(500);
        problem.ErrorCode.Should().Be(typeof(InvalidOperationException).FullName);
        problem.Extensions.Should().ContainKey(ProblemConstants.ExtensionKeys.OriginalExceptionType);
        problem.Extensions[ProblemConstants.ExtensionKeys.OriginalExceptionType].Should().Be(typeof(InvalidOperationException).FullName);
    }

    [Fact]
    public void Problem_Create_FromExceptionWithData_ShouldIncludeExceptionData()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        exception.Data["UserId"] = 123;
        exception.Data["RequestId"] = "ABC-123";
        exception.Data["Timestamp"] = DateTime.UtcNow;

        // Act
        var problem = ManagedCode.Communication.Problem.Create(exception);

        // Assert
        problem.Should().NotBeNull();
        problem.Extensions.Should().ContainKey($"{ProblemConstants.ExtensionKeys.ExceptionDataPrefix}UserId");
        problem.Extensions[$"{ProblemConstants.ExtensionKeys.ExceptionDataPrefix}UserId"].Should().Be(123);
        problem.Extensions.Should().ContainKey($"{ProblemConstants.ExtensionKeys.ExceptionDataPrefix}RequestId");
        problem.Extensions[$"{ProblemConstants.ExtensionKeys.ExceptionDataPrefix}RequestId"].Should().Be("ABC-123");
        problem.Extensions.Should().ContainKey($"{ProblemConstants.ExtensionKeys.ExceptionDataPrefix}Timestamp");
    }

    [Fact]
    public void Problem_Create_FromExceptionWithData_ShouldHandleValidKeysOnly()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        exception.Data["ValidKey"] = "Should be included";
        exception.Data["AnotherKey"] = 42;

        // Act
        var problem = ManagedCode.Communication.Problem.Create(exception);

        // Assert
        problem.Should().NotBeNull();
        problem.Extensions.Should().ContainKey($"{ProblemConstants.ExtensionKeys.ExceptionDataPrefix}ValidKey");
        problem.Extensions[$"{ProblemConstants.ExtensionKeys.ExceptionDataPrefix}ValidKey"].Should().Be("Should be included");
        problem.Extensions.Should().ContainKey($"{ProblemConstants.ExtensionKeys.ExceptionDataPrefix}AnotherKey");
        problem.Extensions[$"{ProblemConstants.ExtensionKeys.ExceptionDataPrefix}AnotherKey"].Should().Be(42);
    }

    [Fact]
    public void Problem_Create_FromInnerException_ShouldUseOuterException()
    {
        // Arrange
        var innerException = new ArgumentNullException("param", "Parameter is null");
        var outerException = new InvalidOperationException("Outer exception", innerException);

        // Act
        var problem = ManagedCode.Communication.Problem.Create(outerException);

        // Assert
        problem.Title.Should().Be("InvalidOperationException");
        problem.Detail.Should().Be("Outer exception");
        problem.ErrorCode.Should().Be(typeof(InvalidOperationException).FullName);
    }

    #endregion

    #region Complex Enum Scenarios

    [Fact]
    public void Problem_Create_WithFlagsEnum_ShouldHandleCorrectly()
    {
        // Arrange
        var flags = FlagsError.Read | FlagsError.Write;

        // Act
        var problem = ManagedCode.Communication.Problem.Create(flags, 403);

        // Assert
        problem.ErrorCode.Should().Be("Read, Write");
        problem.Title.Should().Be("Read, Write");
        problem.Extensions[ProblemConstants.ExtensionKeys.ErrorType].Should().Be("FlagsError");
    }

    [Fact]
    public void Problem_Create_WithNumericEnum_ShouldUseEnumName()
    {
        // Act
        var problem = ManagedCode.Communication.Problem.Create(NumericError.Error100, 400);

        // Assert
        problem.ErrorCode.Should().Be("Error100");
        problem.Title.Should().Be("Error100");
    }

    #endregion

    #region AddValidationError Tests

    [Fact]
    public void Problem_AddValidationError_ToEmptyProblem_ShouldCreateErrorsDictionary()
    {
        // Arrange
        var problem = new ManagedCode.Communication.Problem();

        // Act
        problem.AddValidationError("email", "Email is required");

        // Assert
        problem.Extensions.Should().ContainKey(ProblemConstants.ExtensionKeys.Errors);
        var errors = problem.GetValidationErrors();
        errors.Should().NotBeNull();
        errors!["email"].Should().Contain("Email is required");
    }

    [Fact]
    public void Problem_AddValidationError_ToExistingField_ShouldAppendError()
    {
        // Arrange
        var problem = ManagedCode.Communication.Problem.Validation(("password", "Too short"));

        // Act
        problem.AddValidationError("password", "Must contain numbers");
        problem.AddValidationError("password", "Must contain special characters");

        // Assert
        var errors = problem.GetValidationErrors();
        errors!["password"].Should().HaveCount(3);
        errors["password"].Should().Contain("Too short");
        errors["password"].Should().Contain("Must contain numbers");
        errors["password"].Should().Contain("Must contain special characters");
    }

    [Fact]
    public void Problem_AddValidationError_MultipleFields_ShouldCreateSeparateLists()
    {
        // Arrange
        var problem = new ManagedCode.Communication.Problem();

        // Act
        problem.AddValidationError("name", "Name is required");
        problem.AddValidationError("email", "Invalid email format");
        problem.AddValidationError("age", "Must be 18 or older");
        problem.AddValidationError("email", "Email already exists");

        // Assert
        var errors = problem.GetValidationErrors();
        errors.Should().HaveCount(3);
        errors!["name"].Should().HaveCount(1);
        errors["email"].Should().HaveCount(2);
        errors["age"].Should().HaveCount(1);
    }

    [Fact]
    public void Problem_AddValidationError_WithNonDictionaryExtension_ShouldReplaceWithDictionary()
    {
        // Arrange
        var problem = new ManagedCode.Communication.Problem();
        problem.Extensions[ProblemConstants.ExtensionKeys.Errors] = "not a dictionary";

        // Act
        problem.AddValidationError("field", "error message");

        // Assert
        var errors = problem.GetValidationErrors();
        errors.Should().NotBeNull();
        errors!["field"].Should().Contain("error message");
    }

    #endregion

    #region GetOrCreateValidationErrors Tests

    [Fact]
    public void Problem_GetValidationErrors_WithNoErrors_ShouldReturnNull()
    {
        // Arrange
        var problem = new ManagedCode.Communication.Problem();

        // Act
        var errors = problem.GetValidationErrors();

        // Assert
        errors.Should().BeNull();
    }

    [Fact]
    public void Problem_GetValidationErrors_WithInvalidType_ShouldReturnNull()
    {
        // Arrange
        var problem = new ManagedCode.Communication.Problem();
        problem.Extensions[ProblemConstants.ExtensionKeys.Errors] = "not a dictionary";

        // Act
        var errors = problem.GetValidationErrors();

        // Assert
        errors.Should().BeNull();
    }

    [Fact]
    public void Problem_GetValidationErrors_WithValidErrors_ShouldReturnDictionary()
    {
        // Arrange
        var problem = ManagedCode.Communication.Problem.Validation(
            ("field1", "error1"),
            ("field2", "error2")
        );

        // Act
        var errors = problem.GetValidationErrors();

        // Assert
        errors.Should().NotBeNull();
        errors.Should().HaveCount(2);
        errors!["field1"].Should().Contain("error1");
        errors["field2"].Should().Contain("error2");
    }

    #endregion

    #region Test Helpers

    private enum TestError
    {
        InvalidInput,
        ValidationFailed,
        SystemError
    }

    private enum HttpStatusError
    {
        BadRequest,
        NotFound,
        InternalServerError
    }

    [Flags]
    private enum FlagsError
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4
    }

    private enum NumericError
    {
        Error100 = 100,
        Error200 = 200,
        Error500 = 500
    }

    #endregion
}