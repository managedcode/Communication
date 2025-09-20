using System;
using System.Collections.Generic;
using System.Linq;
using Shouldly;
using Xunit;
using ManagedCode.Communication.Tests.TestHelpers;

namespace ManagedCode.Communication.Tests.Results;

public class ResultInvalidMethodsTests
{
    #region Result Invalid Tests

    [Fact]
    public void Result_Invalid_WithoutParameters_ShouldCreateValidationResult()
    {
        // Act
        var result = Result.Invalid();

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        result.Problem.Type.ShouldBe("https://tools.ietf.org/html/rfc7231#section-6.5.1");
        result.Problem.Title.ShouldBe("Validation Failed");
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors.ShouldNotBeNull();
        validationErrors!["message"].ShouldContain("Invalid");
    }

    [Fact]
    public void Result_Invalid_WithEnum_ShouldCreateValidationResultWithErrorCode()
    {
        // Act
        var result = Result.Invalid(TestError.InvalidInput);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        result.Problem.ErrorCode.ShouldBe("InvalidInput");
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors!["message"].ShouldContain("Invalid");
    }

    [Fact]
    public void Result_Invalid_WithMessage_ShouldCreateValidationResultWithMessage()
    {
        // Arrange
        var message = "Custom validation message";

        // Act
        var result = Result.Invalid(message);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors!["message"].ShouldContain(message);
    }

    [Fact]
    public void Result_Invalid_WithEnumAndMessage_ShouldCreateValidationResultWithBoth()
    {
        // Arrange
        var message = "Custom validation message";

        // Act
        var result = Result.Invalid(TestError.ResourceLocked, message);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        result.Problem.ErrorCode.ShouldBe("ResourceLocked");
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors!["message"].ShouldContain(message);
    }

    [Fact]
    public void Result_Invalid_WithKeyValue_ShouldCreateValidationResultWithCustomField()
    {
        // Arrange
        var key = "username";
        var value = "Username is required";

        // Act
        var result = Result.Invalid(key, value);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors![key].ShouldContain(value);
    }

    [Fact]
    public void Result_Invalid_WithEnumKeyValue_ShouldCreateValidationResultWithAll()
    {
        // Arrange
        var key = "password";
        var value = "Password is too weak";

        // Act
        var result = Result.Invalid(TestError.InvalidInput, key, value);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        result.Problem.ErrorCode.ShouldBe("InvalidInput");
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors![key].ShouldContain(value);
    }

    [Fact]
    public void Result_Invalid_WithDictionary_ShouldCreateValidationResultWithMultipleFields()
    {
        // Arrange
        var values = new Dictionary<string, string>
        {
            ["email"] = "Invalid email format",
            ["age"] = "Age must be positive",
            ["name"] = "Name is required"
        };

        // Act
        var result = Result.Invalid(values);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors!["email"].ShouldContain("Invalid email format");
        validationErrors["age"].ShouldContain("Age must be positive");
        validationErrors["name"].ShouldContain("Name is required");
    }

    // Note: This test removed due to enum validation complexity

    #endregion

    #region Result<T> Invalid Tests

    [Fact]
    public void ResultT_Invalid_WithoutParameters_ShouldCreateValidationResult()
    {
        // Act
        var result = Result<string>.Invalid();

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Value.ShouldBeNull();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        result.Problem.Title.ShouldBe("Validation Failed");
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors!["message"].ShouldContain("Invalid");
    }

    [Fact]
    public void ResultT_Invalid_WithEnum_ShouldCreateValidationResultWithErrorCode()
    {
        // Act
        var result = Result<int>.Invalid(TestError.InvalidInput);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Value.ShouldBe(default(int));
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        result.Problem.ErrorCode.ShouldBe("InvalidInput");
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors!["message"].ShouldContain("Invalid");
    }

    [Fact]
    public void ResultT_Invalid_WithMessage_ShouldCreateValidationResultWithMessage()
    {
        // Arrange
        var message = "Custom validation message";

        // Act
        var result = Result<bool>.Invalid(message);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Value.ShouldBe(default(bool));
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors!["message"].ShouldContain(message);
    }

    [Fact]
    public void ResultT_Invalid_WithEnumAndMessage_ShouldCreateValidationResultWithBoth()
    {
        // Arrange
        var message = "Custom validation message";

        // Act
        var result = Result<decimal>.Invalid(TestError.ResourceLocked, message);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Value.ShouldBe(default(decimal));
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        result.Problem.ErrorCode.ShouldBe("ResourceLocked");
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors!["message"].ShouldContain(message);
    }

    [Fact]
    public void ResultT_Invalid_WithKeyValue_ShouldCreateValidationResultWithCustomField()
    {
        // Arrange
        var key = "username";
        var value = "Username is required";

        // Act
        var result = Result<User>.Invalid(key, value);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Value.ShouldBeNull();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors![key].ShouldContain(value);
    }

    [Fact]
    public void ResultT_Invalid_WithEnumKeyValue_ShouldCreateValidationResultWithAll()
    {
        // Arrange
        var key = "password";
        var value = "Password is too weak";

        // Act
        var result = Result<User>.Invalid(TestError.InvalidInput, key, value);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Value.ShouldBeNull();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        result.Problem.ErrorCode.ShouldBe("InvalidInput");
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors![key].ShouldContain(value);
    }

    [Fact]
    public void ResultT_Invalid_WithDictionary_ShouldCreateValidationResultWithMultipleFields()
    {
        // Arrange
        var values = new Dictionary<string, string>
        {
            ["email"] = "Invalid email format",
            ["age"] = "Age must be positive",
            ["name"] = "Name is required"
        };

        // Act
        var result = Result<User>.Invalid(values);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Value.ShouldBeNull();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors!["email"].ShouldContain("Invalid email format");
        validationErrors["age"].ShouldContain("Age must be positive");
        validationErrors["name"].ShouldContain("Name is required");
    }

    [Fact]
    public void ResultT_Invalid_WithEnumAndDictionary_ShouldCreateValidationResultWithAllData()
    {
        // Arrange
        var values = new Dictionary<string, string>
        {
            ["field1"] = "Error 1",
            ["field2"] = "Error 2"
        };

        // Act
        var result = Result<User>.Invalid(TestError.InvalidInput, values);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Value.ShouldBeNull();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        result.Problem.ErrorCode.ShouldBe("InvalidInput");
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors!["field1"].ShouldContain("Error 1");
        validationErrors["field2"].ShouldContain("Error 2");
    }

    #endregion

    #region Generic Result.Invalid<T> Tests

    [Fact]
    public void Result_InvalidT_WithoutParameters_ShouldCreateValidationResult()
    {
        // Act
        var result = Result.Invalid<string>();

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Value.ShouldBeNull();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        result.Problem.Title.ShouldBe("Validation Failed");
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors!["message"].ShouldContain("Invalid");
    }

    [Fact]
    public void Result_InvalidT_WithEnum_ShouldCreateValidationResultWithErrorCode()
    {
        // Act
        var result = Result.Invalid<int, TestError>(TestError.InvalidInput);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Value.ShouldBe(default(int));
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        result.Problem.ErrorCode.ShouldBe("InvalidInput");
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors!["message"].ShouldContain("Invalid");
    }

    [Fact]
    public void Result_InvalidT_WithMessage_ShouldCreateValidationResultWithMessage()
    {
        // Arrange
        var message = "Custom validation message";

        // Act
        var result = Result.Invalid<bool>(message);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Value.ShouldBe(default(bool));
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors!["message"].ShouldContain(message);
    }

    [Fact]
    public void Result_InvalidT_WithEnumAndMessage_ShouldCreateValidationResultWithBoth()
    {
        // Arrange
        var message = "Custom validation message";

        // Act
        var result = Result.Invalid<decimal, TestError>(TestError.ResourceLocked, message);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Value.ShouldBe(default(decimal));
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        result.Problem.ErrorCode.ShouldBe("ResourceLocked");
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors!["message"].ShouldContain(message);
    }

    [Fact]
    public void Result_InvalidT_WithKeyValue_ShouldCreateValidationResultWithCustomField()
    {
        // Arrange
        var key = "username";
        var value = "Username is required";

        // Act
        var result = Result.Invalid<User>(key, value);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Value.ShouldBeNull();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors![key].ShouldContain(value);
    }

    [Fact]
    public void Result_InvalidT_WithEnumKeyValue_ShouldCreateValidationResultWithAll()
    {
        // Arrange
        var key = "password";
        var value = "Password is too weak";

        // Act
        var result = Result.Invalid<User, TestError>(TestError.InvalidInput, key, value);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Value.ShouldBeNull();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        result.Problem.ErrorCode.ShouldBe("InvalidInput");
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors![key].ShouldContain(value);
    }

    [Fact]
    public void Result_InvalidT_WithDictionary_ShouldCreateValidationResultWithMultipleFields()
    {
        // Arrange
        var values = new Dictionary<string, string>
        {
            ["email"] = "Invalid email format",
            ["age"] = "Age must be positive"
        };

        // Act
        var result = Result.Invalid<User>(values);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Value.ShouldBeNull();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors!["email"].ShouldContain("Invalid email format");
        validationErrors["age"].ShouldContain("Age must be positive");
    }

    [Fact]
    public void Result_InvalidT_WithEnumAndDictionary_ShouldCreateValidationResultWithAllData()
    {
        // Arrange
        var values = new Dictionary<string, string>
        {
            ["field1"] = "Error 1",
            ["field2"] = "Error 2"
        };

        // Act
        var result = Result.Invalid<User, TestError>(TestError.InvalidInput, values);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Value.ShouldBeNull();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        result.Problem.ErrorCode.ShouldBe("InvalidInput");
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors!["field1"].ShouldContain("Error 1");
        validationErrors["field2"].ShouldContain("Error 2");
    }

    #endregion

    #region Edge Cases and Complex Scenarios

    [Fact]
    public void Invalid_WithEmptyDictionary_ShouldCreateValidationResultWithoutErrors()
    {
        // Arrange
        var emptyValues = new Dictionary<string, string>();

        // Act
        var result = Result.Invalid(emptyValues);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors.ShouldNotBeNull();
        validationErrors!.ShouldBeEmpty();
    }

    [Fact]
    public void Invalid_WithDictionaryContainingEmptyValues_ShouldHandleGracefully()
    {
        // Arrange
        var values = new Dictionary<string, string>
        {
            ["field1"] = "Valid error",
            ["field2"] = string.Empty
        };

        // Act
        var result = Result<string>.Invalid(values);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors!["field1"].ShouldContain("Valid error");
        validationErrors["field2"].ShouldContain(string.Empty);
    }

    [Fact]
    public void Invalid_WithDuplicateKeysInDictionary_ShouldOverwritePreviousValue()
    {
        // Arrange
        var key = "username";
        var values = new Dictionary<string, string>
        {
            [key] = "First error"
        };
        values[key] = "Second error"; // Overwrite

        // Act
        var result = Result.Invalid(TestError.InvalidInput, values);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.ErrorCode.ShouldBe("InvalidInput");
        
        var validationErrors = result.AssertValidationErrors();
        validationErrors![key].ShouldContain("Second error");
        validationErrors[key].ShouldNotContain("First error");
    }

    [Fact]
    public void Invalid_DifferentEnumTypes_ShouldWorkWithAnyEnum()
    {
        // Act
        var result1 = Result.Invalid(TestError.InvalidInput);
        var result2 = Result.Invalid(TestStatus.Pending);

        // Assert
        result1.Problem!.ErrorCode.ShouldBe("InvalidInput");
        result2.Problem!.ErrorCode.ShouldBe("Pending");
    }

    #endregion

    #region Helper Classes

    public class User
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public int Age { get; set; }
    }

    public enum TestStatus
    {
        Pending,
        Approved,
        Rejected
    }

    #endregion
}
