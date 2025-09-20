using System;
using System.Collections.Generic;
using Shouldly;
using ManagedCode.Communication.CollectionResultT;
using Xunit;
using ManagedCode.Communication.Tests.TestHelpers;

namespace ManagedCode.Communication.Tests.Results;

public class CollectionResultInvalidMethodsTests
{
    #region Invalid() Tests

    [Fact]
    public void CollectionResult_Invalid_NoParameters_ShouldCreateValidationResult()
    {
        // Act
        var result = CollectionResult<string>.Invalid();

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        result.Problem.Title.ShouldBe("Validation Failed");
        var errors = result.AssertValidationErrors();
        errors.ShouldContainKey("message");
        errors!["message"].ShouldContain("Invalid");
    }

    #endregion

    #region Invalid<TEnum> Tests

    [Fact]
    public void CollectionResult_Invalid_WithEnum_ShouldCreateValidationResultWithErrorCode()
    {
        // Act
        var result = CollectionResult<int>.Invalid(TestError.InvalidInput);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        result.Problem.ErrorCode.ShouldBe("InvalidInput");
        var errors = result.AssertValidationErrors();
        errors.ShouldContainKey("message");
        errors!["message"].ShouldContain("Invalid");
    }

    #endregion

    #region Invalid(string) Tests

    [Fact]
    public void CollectionResult_Invalid_WithMessage_ShouldCreateValidationResultWithMessage()
    {
        // Arrange
        const string message = "Custom validation error";

        // Act
        var result = CollectionResult<User>.Invalid(message);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        var errors = result.AssertValidationErrors();
        errors.ShouldContainKey("message");
        errors!["message"].ShouldContain(message);
    }

    #endregion

    #region Invalid<TEnum>(TEnum, string) Tests

    [Fact]
    public void CollectionResult_Invalid_WithEnumAndMessage_ShouldCreateValidationResultWithBoth()
    {
        // Arrange
        const string message = "Validation failed";

        // Act
        var result = CollectionResult<string>.Invalid(TestError.ValidationFailed, message);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        result.Problem.ErrorCode.ShouldBe("ValidationFailed");
        var errors = result.AssertValidationErrors();
        errors.ShouldContainKey("message");
        errors!["message"].ShouldContain(message);
    }

    #endregion

    #region Invalid(string, string) Tests

    [Fact]
    public void CollectionResult_Invalid_WithKeyValue_ShouldCreateValidationResultWithKeyValue()
    {
        // Arrange
        const string key = "email";
        const string value = "Invalid email format";

        // Act
        var result = CollectionResult<User>.Invalid(key, value);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        var errors = result.AssertValidationErrors();
        errors.ShouldContainKey(key);
        errors![key].ShouldContain(value);
    }

    #endregion

    #region Invalid<TEnum>(TEnum, string, string) Tests

    [Fact]
    public void CollectionResult_Invalid_WithEnumAndKeyValue_ShouldCreateValidationResultWithAll()
    {
        // Arrange
        const string key = "username";
        const string value = "Username already taken";

        // Act
        var result = CollectionResult<string>.Invalid(TestError.DuplicateEntry, key, value);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        result.Problem.ErrorCode.ShouldBe("DuplicateEntry");
        var errors = result.AssertValidationErrors();
        errors.ShouldContainKey(key);
        errors![key].ShouldContain(value);
    }

    #endregion

    #region Invalid(Dictionary<string, string>) Tests

    [Fact]
    public void CollectionResult_Invalid_WithDictionary_ShouldCreateValidationResultWithAllErrors()
    {
        // Arrange
        var validationErrors = new Dictionary<string, string>
        {
            { "email", "Email is required" },
            { "password", "Password must be at least 8 characters" },
            { "age", "Age must be between 18 and 100" }
        };

        // Act
        var result = CollectionResult<User>.Invalid(validationErrors);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        var errors = result.AssertValidationErrors();
        errors.ShouldNotBeNull();
        errors!.ShouldHaveCount(3);
        errors!["email"].ShouldContain("Email is required");
        errors["password"].ShouldContain("Password must be at least 8 characters");
        errors["age"].ShouldContain("Age must be between 18 and 100");
    }

    [Fact]
    public void CollectionResult_Invalid_WithEmptyDictionary_ShouldCreateValidationResultWithNoErrors()
    {
        // Arrange
        var validationErrors = new Dictionary<string, string>();

        // Act
        var result = CollectionResult<string>.Invalid(validationErrors);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        var errors = result.AssertValidationErrors();
        if (errors != null)
            errors.ShouldBeEmpty();
        else
            false.ShouldBeTrue("errors should not be null");
    }

    #endregion

    #region Invalid<TEnum>(TEnum, Dictionary<string, string>) Tests

    [Fact]
    public void CollectionResult_Invalid_WithEnumAndDictionary_ShouldCreateValidationResultWithErrorCodeAndErrors()
    {
        // Arrange
        var validationErrors = new Dictionary<string, string>
        {
            { "field1", "Error 1" },
            { "field2", "Error 2" }
        };

        // Act
        var result = CollectionResult<int>.Invalid(TestError.ValidationFailed, validationErrors);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        result.Problem.ErrorCode.ShouldBe("ValidationFailed");
        var errors = result.AssertValidationErrors();
        errors.ShouldNotBeNull();
        errors!.ShouldHaveCount(2);
        errors!["field1"].ShouldContain("Error 1");
        errors["field2"].ShouldContain("Error 2");
    }

    #endregion

    #region Complex Scenarios Tests

    [Fact]
    public void CollectionResult_Invalid_ChainedValidations_ShouldWorkCorrectly()
    {
        // Arrange
        var results = new List<CollectionResult<string>>();

        // Act
        results.Add(CollectionResult<string>.Invalid());
        results.Add(CollectionResult<string>.Invalid("Validation error"));
        results.Add(CollectionResult<string>.Invalid("email", "Invalid format"));
        results.Add(CollectionResult<string>.Invalid(TestError.InvalidInput));
        results.Add(CollectionResult<string>.Invalid(TestError.ValidationFailed, "Custom error"));
        results.Add(CollectionResult<string>.Invalid(new Dictionary<string, string> { { "key", "value" } }));

        // Assert
        results.ShouldHaveCount(6);
        results.ShouldAllBe(r => r.IsFailed);
        results.ShouldAllBe(r => r.Problem != null);
        results.ShouldAllBe(r => r.Problem!.StatusCode == 400);
    }

    [Fact]
    public void CollectionResult_Invalid_WithNullValues_ShouldHandleGracefully()
    {
        // Act & Assert - null message
        var result1 = CollectionResult<string>.Invalid((string)null!);
        result1.IsFailed.ShouldBeTrue();
        var errors1 = result1.AssertValidationErrors();
        errors1.ShouldContainKey("message");
        errors1["message"].ShouldContain((string)null!);

        // Act & Assert - null key/value (should throw or handle gracefully)
        var exception = Should.Throw<ArgumentNullException>(() => CollectionResult<string>.Invalid(null!, null!));
        exception.ParamName.ShouldBe("key");
    }

    [Fact]
    public void CollectionResult_Invalid_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        const string key = "field-name.with[special]";
        const string value = "Error with 'quotes' and \"double quotes\"";

        // Act
        var result = CollectionResult<string>.Invalid(key, value);

        // Assert
        result.IsFailed.ShouldBeTrue();
        var errors = result.AssertValidationErrors();
        errors.ShouldNotBeNull();
        errors!.ShouldContainKey(key);
        errors![key].ShouldContain(value);
    }

    [Fact]
    public void CollectionResult_Invalid_ComparedToFailValidation_ShouldBeEquivalent()
    {
        // Act
        var invalidResult = CollectionResult<string>.Invalid("email", "Invalid email");
        var failValidationResult = CollectionResult<string>.FailValidation(("email", "Invalid email"));

        // Assert
        invalidResult.IsFailed.ShouldBe(failValidationResult.IsFailed);
        invalidResult.Problem!.StatusCode.ShouldBe(failValidationResult.Problem!.StatusCode);
        invalidResult.Problem.Title.ShouldBe(failValidationResult.Problem.Title);
        var invalidErrors = invalidResult.AssertValidationErrors();
        var failValidationErrors = failValidationResult.AssertValidationErrors();
        invalidErrors.ShouldBeEquivalentTo(failValidationErrors);
    }

    #endregion

    #region Pagination with Invalid Tests

    [Fact]
    public void CollectionResult_Invalid_WithPagination_ShouldNotHaveData()
    {
        // Act
        var result = CollectionResult<string>.Invalid("page", "Invalid page number");

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Collection.ShouldBeEmpty();
        result.TotalItems.ShouldBe(0);
        result.PageNumber.ShouldBe(0);
        result.PageSize.ShouldBe(0);
        result.TotalPages.ShouldBe(0);
    }

    #endregion

    #region Test Helpers

    private enum TestError
    {
        InvalidInput,
        ValidationFailed,
        DuplicateEntry
    }

    private class User
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public int Age { get; set; }
    }

    #endregion
}
