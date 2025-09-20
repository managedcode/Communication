using System;
using Shouldly;
using ManagedCode.Communication.Extensions;
using ManagedCode.Communication.Results.Extensions;
using Xunit;

namespace ManagedCode.Communication.Tests.Extensions;

public class ResultConversionExtensionsTests
{
    #region AsResult<T> for Values Tests

    [Fact]
    public void AsResult_WithStringValue_CreatesSuccessfulResult()
    {
        // Arrange
        var value = "test string";

        // Act
        var result = value.AsResult();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("test string");
        result.Problem.ShouldBeNull();
    }

    [Fact]
    public void AsResult_WithIntValue_CreatesSuccessfulResult()
    {
        // Arrange
        var value = 42;

        // Act
        var result = value.AsResult();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
        result.Problem.ShouldBeNull();
    }

    [Fact]
    public void AsResult_WithNullValue_CreatesSuccessfulResultWithNull()
    {
        // Arrange
        string? value = null;

        // Act
        var result = value.AsResult();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
        result.Problem.ShouldBeNull();
    }

    [Fact]
    public void AsResult_WithComplexObject_CreatesSuccessfulResult()
    {
        // Arrange
        var value = new TestClass { Id = 1, Name = "Test" };

        // Act
        var result = value.AsResult();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(value);
        result.Value!.Id.ShouldBe(1);
        result.Value.Name.ShouldBe("Test");
    }

    #endregion

    #region AsResult<T> for Exception Tests

    [Fact]
    public void AsResult_WithException_CreatesFailedResultWithProblem()
    {
        // Arrange
        var exception = new InvalidOperationException("Something went wrong");

        // Act
        var result = exception.AsResult<string>();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.Problem.ShouldNotBeNull();
        result.Problem!.Title.ShouldBe("InvalidOperationException");
        result.Problem.Detail.ShouldBe("Something went wrong");
        result.Problem.StatusCode.ShouldBe(500);
    }

    [Fact]
    public void AsResult_WithArgumentException_CreatesFailedResultWithCorrectProblem()
    {
        // Arrange
        var exception = new ArgumentException("Invalid argument provided", "paramName");

        // Act
        var result = exception.AsResult<int>();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Problem.ShouldNotBeNull();
        result.Problem!.Title.ShouldBe("ArgumentException");
        result.Problem.Detail.ShouldBe("Invalid argument provided (Parameter 'paramName')");
        result.Problem.StatusCode.ShouldBe(500);
        result.Problem.ErrorCode.ShouldBe("System.ArgumentException");
    }

    [Fact]
    public void AsResult_WithCustomException_CreatesFailedResultWithCustomMessage()
    {
        // Arrange
        var exception = new TestException("Custom error occurred");

        // Act
        var result = exception.AsResult<bool>();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Problem!.Title.ShouldBe("TestException");
        result.Problem.Detail.ShouldBe("Custom error occurred");
        result.Problem.ErrorCode.ShouldBe("ManagedCode.Communication.Tests.Extensions.ResultConversionExtensionsTests+TestException");
    }

    #endregion

    #region Integration with Result Methods Tests

    [Fact]
    public void AsResult_CanBeChainedWithRailwayMethods()
    {
        // Arrange
        var value = "initial";

        // Act
        var result = value.AsResult()
            .Map(x => x.ToUpper())
            .Bind(x => x.Length > 5 ? Result<int>.Succeed(x.Length) : Result<int>.Fail("Too short"));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(7); // "INITIAL".Length
    }

    [Fact]
    public void AsResult_ExceptionCanBeUsedInRailwayChain()
    {
        // Arrange
        var exception = new ArgumentException("Invalid input");

        // Act
        var result = exception.AsResult<string>()
            .Map(x => x.ToUpper()) // Should not execute
            .Else(() => Result<string>.Succeed("fallback"));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("fallback");
    }

    #endregion

    #region Type Inference Tests

    [Fact]
    public void AsResult_InfersTypeFromValue()
    {
        // Arrange & Act
        var stringResult = "test".AsResult();
        var intResult = 42.AsResult();
        var boolResult = true.AsResult();

        // Assert
        stringResult.ShouldBeOfType<Result<string>>();
        intResult.ShouldBeOfType<Result<int>>();
        boolResult.ShouldBeOfType<Result<bool>>();
    }

    [Fact]
    public void AsResult_ExplicitTypeSpecification_WorksCorrectly()
    {
        // Arrange
        var exception = new InvalidCastException("Cast failed");

        // Act
        var result = exception.AsResult<decimal>();

        // Assert
        result.ShouldBeOfType<Result<decimal>>();
        result.IsSuccess.ShouldBeFalse();
    }

    #endregion

    private class TestClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class TestException : Exception
    {
        public TestException(string message) : base(message)
        {
        }
    }
}
