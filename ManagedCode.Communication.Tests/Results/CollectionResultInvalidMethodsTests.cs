using System;
using System.Collections.Generic;
using FluentAssertions;
using ManagedCode.Communication.CollectionResultT;
using Xunit;

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
        result.IsFailed.Should().BeTrue();
        result.Problem.Should().NotBeNull();
        result.Problem!.StatusCode.Should().Be(400);
        result.Problem.Title.Should().Be("Validation Failed");
        var errors = result.Problem.GetValidationErrors();
        errors.Should().ContainKey("message");
        errors!["message"].Should().Contain("Invalid");
    }

    #endregion

    #region Invalid<TEnum> Tests

    [Fact]
    public void CollectionResult_Invalid_WithEnum_ShouldCreateValidationResultWithErrorCode()
    {
        // Act
        var result = CollectionResult<int>.Invalid(TestError.InvalidInput);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Problem.Should().NotBeNull();
        result.Problem!.StatusCode.Should().Be(400);
        result.Problem.ErrorCode.Should().Be("InvalidInput");
        var errors = result.Problem.GetValidationErrors();
        errors.Should().ContainKey("message");
        errors!["message"].Should().Contain("Invalid");
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
        result.IsFailed.Should().BeTrue();
        result.Problem.Should().NotBeNull();
        result.Problem!.StatusCode.Should().Be(400);
        var errors = result.Problem.GetValidationErrors();
        errors.Should().ContainKey("message");
        errors!["message"].Should().Contain(message);
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
        result.IsFailed.Should().BeTrue();
        result.Problem.Should().NotBeNull();
        result.Problem!.StatusCode.Should().Be(400);
        result.Problem.ErrorCode.Should().Be("ValidationFailed");
        var errors = result.Problem.GetValidationErrors();
        errors.Should().ContainKey("message");
        errors!["message"].Should().Contain(message);
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
        result.IsFailed.Should().BeTrue();
        result.Problem.Should().NotBeNull();
        result.Problem!.StatusCode.Should().Be(400);
        var errors = result.Problem.GetValidationErrors();
        errors.Should().ContainKey(key);
        errors![key].Should().Contain(value);
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
        result.IsFailed.Should().BeTrue();
        result.Problem.Should().NotBeNull();
        result.Problem!.StatusCode.Should().Be(400);
        result.Problem.ErrorCode.Should().Be("DuplicateEntry");
        var errors = result.Problem.GetValidationErrors();
        errors.Should().ContainKey(key);
        errors![key].Should().Contain(value);
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
        result.IsFailed.Should().BeTrue();
        result.Problem.Should().NotBeNull();
        result.Problem!.StatusCode.Should().Be(400);
        var errors = result.Problem.GetValidationErrors();
        errors.Should().NotBeNull();
        errors!.Should().HaveCount(3);
        errors!["email"].Should().Contain("Email is required");
        errors["password"].Should().Contain("Password must be at least 8 characters");
        errors["age"].Should().Contain("Age must be between 18 and 100");
    }

    [Fact]
    public void CollectionResult_Invalid_WithEmptyDictionary_ShouldCreateValidationResultWithNoErrors()
    {
        // Arrange
        var validationErrors = new Dictionary<string, string>();

        // Act
        var result = CollectionResult<string>.Invalid(validationErrors);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Problem.Should().NotBeNull();
        result.Problem!.StatusCode.Should().Be(400);
        var errors = result.Problem.GetValidationErrors();
        if (errors != null)
            errors.Should().BeEmpty();
        else
            false.Should().BeTrue("errors should not be null");
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
        result.IsFailed.Should().BeTrue();
        result.Problem.Should().NotBeNull();
        result.Problem!.StatusCode.Should().Be(400);
        result.Problem.ErrorCode.Should().Be("ValidationFailed");
        var errors = result.Problem.GetValidationErrors();
        errors.Should().NotBeNull();
        errors!.Should().HaveCount(2);
        errors!["field1"].Should().Contain("Error 1");
        errors["field2"].Should().Contain("Error 2");
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
        results.Should().HaveCount(6);
        results.Should().OnlyContain(r => r.IsFailed);
        results.Should().OnlyContain(r => r.Problem != null);
        results.Should().OnlyContain(r => r.Problem!.StatusCode == 400);
    }

    [Fact]
    public void CollectionResult_Invalid_WithNullValues_ShouldHandleGracefully()
    {
        // Act & Assert - null message
        var result1 = CollectionResult<string>.Invalid((string)null!);
        result1.IsFailed.Should().BeTrue();
        var errors1 = result1.Problem!.GetValidationErrors();
        errors1.Should().NotBeNull();
        errors1!["message"].Should().Contain((string)null!);

        // Act & Assert - null key/value (should throw or handle gracefully)
        var action = () => CollectionResult<string>.Invalid(null!, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithMessage("*key*");
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
        result.IsFailed.Should().BeTrue();
        var errors = result.Problem!.GetValidationErrors();
        errors.Should().NotBeNull();
        errors!.Should().ContainKey(key);
        errors![key].Should().Contain(value);
    }

    [Fact]
    public void CollectionResult_Invalid_ComparedToFailValidation_ShouldBeEquivalent()
    {
        // Act
        var invalidResult = CollectionResult<string>.Invalid("email", "Invalid email");
        var failValidationResult = CollectionResult<string>.FailValidation(("email", "Invalid email"));

        // Assert
        invalidResult.IsFailed.Should().Be(failValidationResult.IsFailed);
        invalidResult.Problem!.StatusCode.Should().Be(failValidationResult.Problem!.StatusCode);
        invalidResult.Problem.Title.Should().Be(failValidationResult.Problem.Title);
        var invalidErrors = invalidResult.Problem.GetValidationErrors();
        var failValidationErrors = failValidationResult.Problem.GetValidationErrors();
        invalidErrors.Should().BeEquivalentTo(failValidationErrors);
    }

    #endregion

    #region Pagination with Invalid Tests

    [Fact]
    public void CollectionResult_Invalid_WithPagination_ShouldNotHaveData()
    {
        // Act
        var result = CollectionResult<string>.Invalid("page", "Invalid page number");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Collection.Should().BeEmpty();
        result.TotalItems.Should().Be(0);
        result.PageNumber.Should().Be(0);
        result.PageSize.Should().Be(0);
        result.TotalPages.Should().Be(0);
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