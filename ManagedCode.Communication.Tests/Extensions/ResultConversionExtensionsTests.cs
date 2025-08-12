using System;
using FluentAssertions;
using ManagedCode.Communication.Extensions;
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
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("test string");
        result.Problem.Should().BeNull();
    }

    [Fact]
    public void AsResult_WithIntValue_CreatesSuccessfulResult()
    {
        // Arrange
        var value = 42;

        // Act
        var result = value.AsResult();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        result.Problem.Should().BeNull();
    }

    [Fact]
    public void AsResult_WithNullValue_CreatesSuccessfulResultWithNull()
    {
        // Arrange
        string? value = null;

        // Act
        var result = value.AsResult();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Problem.Should().BeNull();
    }

    [Fact]
    public void AsResult_WithComplexObject_CreatesSuccessfulResult()
    {
        // Arrange
        var value = new TestClass { Id = 1, Name = "Test" };

        // Act
        var result = value.AsResult();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
        result.Value!.Id.Should().Be(1);
        result.Value.Name.Should().Be("Test");
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
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Problem.Should().NotBeNull();
        result.Problem!.Title.Should().Be("InvalidOperationException");
        result.Problem.Detail.Should().Be("Something went wrong");
        result.Problem.StatusCode.Should().Be(500);
    }

    [Fact]
    public void AsResult_WithArgumentException_CreatesFailedResultWithCorrectProblem()
    {
        // Arrange
        var exception = new ArgumentException("Invalid argument provided", "paramName");

        // Act
        var result = exception.AsResult<int>();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Problem.Should().NotBeNull();
        result.Problem!.Title.Should().Be("ArgumentException");
        result.Problem.Detail.Should().Be("Invalid argument provided (Parameter 'paramName')");
        result.Problem.StatusCode.Should().Be(500);
        result.Problem.ErrorCode.Should().Be("System.ArgumentException");
    }

    [Fact]
    public void AsResult_WithCustomException_CreatesFailedResultWithCustomMessage()
    {
        // Arrange
        var exception = new TestException("Custom error occurred");

        // Act
        var result = exception.AsResult<bool>();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Problem!.Title.Should().Be("TestException");
        result.Problem.Detail.Should().Be("Custom error occurred");
        result.Problem.ErrorCode.Should().Be("ManagedCode.Communication.Tests.Extensions.ResultConversionExtensionsTests+TestException");
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
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(7); // "INITIAL".Length
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
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("fallback");
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
        stringResult.Should().BeOfType<Result<string>>();
        intResult.Should().BeOfType<Result<int>>();
        boolResult.Should().BeOfType<Result<bool>>();
    }

    [Fact]
    public void AsResult_ExplicitTypeSpecification_WorksCorrectly()
    {
        // Arrange
        var exception = new InvalidCastException("Cast failed");

        // Act
        var result = exception.AsResult<decimal>();

        // Assert
        result.Should().BeOfType<Result<decimal>>();
        result.IsSuccess.Should().BeFalse();
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