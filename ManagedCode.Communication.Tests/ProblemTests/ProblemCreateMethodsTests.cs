using System;
using System.Collections.Generic;
using Shouldly;
using ManagedCode.Communication.Constants;
using Xunit;
using ManagedCode.Communication.Tests.TestHelpers;

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
        problem.ShouldNotBeNull();
        problem.Type.ShouldBe(ProblemConstants.Types.HttpStatus(400));
        problem.Title.ShouldBe("InvalidInput");
        problem.StatusCode.ShouldBe(400);
        problem.Detail.ShouldBe($"{ProblemConstants.Messages.GenericError}: InvalidInput");
        problem.ErrorCode.ShouldBe("InvalidInput");
        problem.Extensions.ShouldContainKey(ProblemConstants.ExtensionKeys.ErrorType);
        problem.Extensions[ProblemConstants.ExtensionKeys.ErrorType].ShouldBe("TestError");
    }

    [Fact]
    public void Problem_Create_WithEnumDetailAndStatusCode_ShouldCreateProblemWithCustomDetail()
    {
        // Arrange
        const string detail = "Custom error detail";

        // Act
        var problem = ManagedCode.Communication.Problem.Create(TestError.ValidationFailed, detail, 422);

        // Assert
        problem.ShouldNotBeNull();
        problem.Type.ShouldBe(ProblemConstants.Types.HttpStatus(422));
        problem.Title.ShouldBe("ValidationFailed");
        problem.StatusCode.ShouldBe(422);
        problem.Detail.ShouldBe(detail);
        problem.ErrorCode.ShouldBe("ValidationFailed");
        problem.Extensions.ShouldContainKey(ProblemConstants.ExtensionKeys.ErrorType);
        problem.Extensions[ProblemConstants.ExtensionKeys.ErrorType].ShouldBe("TestError");
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
        problem.StatusCode.ShouldBe(statusCode);
        problem.Type.ShouldBe(ProblemConstants.Types.HttpStatus(statusCode));
        problem.ErrorCode.ShouldBe(enumValue);
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
        problem.ShouldNotBeNull();
        problem.Type.ShouldBe(ProblemConstants.Types.HttpStatus(500));
        problem.Title.ShouldBe("InvalidOperationException");
        problem.Detail.ShouldBe("Test exception message");
        problem.StatusCode.ShouldBe(500);
        problem.ErrorCode.ShouldBe(typeof(InvalidOperationException).FullName);
        problem.Extensions.ShouldContainKey(ProblemConstants.ExtensionKeys.OriginalExceptionType);
        problem.Extensions[ProblemConstants.ExtensionKeys.OriginalExceptionType].ShouldBe(typeof(InvalidOperationException).FullName);
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
        problem.ShouldNotBeNull();
        problem.Extensions.ShouldContainKey($"{ProblemConstants.ExtensionKeys.ExceptionDataPrefix}UserId");
        problem.Extensions[$"{ProblemConstants.ExtensionKeys.ExceptionDataPrefix}UserId"].ShouldBe(123);
        problem.Extensions.ShouldContainKey($"{ProblemConstants.ExtensionKeys.ExceptionDataPrefix}RequestId");
        problem.Extensions[$"{ProblemConstants.ExtensionKeys.ExceptionDataPrefix}RequestId"].ShouldBe("ABC-123");
        problem.Extensions.ShouldContainKey($"{ProblemConstants.ExtensionKeys.ExceptionDataPrefix}Timestamp");
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
        problem.ShouldNotBeNull();
        problem.Extensions.ShouldContainKey($"{ProblemConstants.ExtensionKeys.ExceptionDataPrefix}ValidKey");
        problem.Extensions[$"{ProblemConstants.ExtensionKeys.ExceptionDataPrefix}ValidKey"].ShouldBe("Should be included");
        problem.Extensions.ShouldContainKey($"{ProblemConstants.ExtensionKeys.ExceptionDataPrefix}AnotherKey");
        problem.Extensions[$"{ProblemConstants.ExtensionKeys.ExceptionDataPrefix}AnotherKey"].ShouldBe(42);
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
        problem.Title.ShouldBe("InvalidOperationException");
        problem.Detail.ShouldBe("Outer exception");
        problem.ErrorCode.ShouldBe(typeof(InvalidOperationException).FullName);
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
        problem.ErrorCode.ShouldBe("Read, Write");
        problem.Title.ShouldBe("Read, Write");
        problem.Extensions[ProblemConstants.ExtensionKeys.ErrorType].ShouldBe("FlagsError");
    }

    [Fact]
    public void Problem_Create_WithNumericEnum_ShouldUseEnumName()
    {
        // Act
        var problem = ManagedCode.Communication.Problem.Create(NumericError.Error100, 400);

        // Assert
        problem.ErrorCode.ShouldBe("Error100");
        problem.Title.ShouldBe("Error100");
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
        problem.Extensions.ShouldContainKey(ProblemConstants.ExtensionKeys.Errors);
        var errors = problem.GetValidationErrors();
        errors.ShouldNotBeNull();
        errors!["email"].ShouldContain("Email is required");
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
        errors!["password"].ShouldHaveCount(3);
        errors["password"].ShouldContain("Too short");
        errors["password"].ShouldContain("Must contain numbers");
        errors["password"].ShouldContain("Must contain special characters");
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
        errors.ShouldNotBeNull();
        var errorDictionary = errors!;
        errorDictionary.ShouldHaveCount(3);
        errorDictionary["name"].ShouldHaveCount(1);
        errorDictionary["email"].ShouldHaveCount(2);
        errorDictionary["age"].ShouldHaveCount(1);
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
        errors.ShouldNotBeNull();
        errors!["field"].ShouldContain("error message");
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
        errors.ShouldBeNull();
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
        errors.ShouldBeNull();
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
        errors.ShouldNotBeNull();
        errors.ShouldHaveCount(2);
        errors!["field1"].ShouldContain("error1");
        errors["field2"].ShouldContain("error2");
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
