using System;
using System.Collections.Generic;
using FluentAssertions;
using ManagedCode.Communication.Extensions;
using Xunit;

namespace ManagedCode.Communication.Tests.Results;

public class RailwayOrientedProgrammingTests
{
    [Fact]
    public void Map_ChainedOperations_ShouldWork()
    {
        // Arrange
        var result = Result<int>.Succeed(5);

        // Act
        var finalResult = result.Map(x => x * 2) // 5 -> 10
            .Map(x => x + 3) // 10 -> 13
            .Map(x => x.ToString()); // 13 -> "13"

        // Assert
        finalResult.IsSuccess
            .Should()
            .BeTrue();
        finalResult.Value
            .Should()
            .Be("13");
    }

    [Fact]
    public void Map_WithFailure_ShouldShortCircuit()
    {
        // Arrange
        var result = Result<int>.Fail("Initial failure", "Initial failure");

        // Act
        var finalResult = result.Map(x => x * 2)
            .Map(x => x + 3)
            .Map(x => x.ToString());

        // Assert
        finalResult.IsSuccess
            .Should()
            .BeFalse();
        finalResult.Problem!.Detail
            .Should()
            .Be("Initial failure");
    }

    [Fact]
    public void Bind_ChainedOperations_ShouldWork()
    {
        // Arrange
        var result = Result<int>.Succeed(5);

        // Act
        var finalResult = result.Bind(x => x > 0 ? Result<int>.Succeed(x * 2) : Result<int>.Fail("Value must be positive", "Value must be positive"))
            .Bind(x => x < 20 ? Result<string>.Succeed(x.ToString()) : Result<string>.Fail("Value too large", "Value too large"));

        // Assert
        finalResult.IsSuccess
            .Should()
            .BeTrue();
        finalResult.Value
            .Should()
            .Be("10");
    }

    [Fact]
    public void Bind_WithFailure_ShouldShortCircuit()
    {
        // Arrange
        var result = Result<int>.Succeed(5);

        // Act
        var finalResult = result.Bind(x => Result<int>.Fail("Bind failure", "Bind failure"))
            .Bind(x => Result<string>.Succeed(x.ToString()));

        // Assert
        finalResult.IsSuccess
            .Should()
            .BeFalse();
        finalResult.Problem!.Detail
            .Should()
            .Be("Bind failure");
    }

    [Fact]
    public void Tap_ShouldExecuteSideEffectWithoutChangingResult()
    {
        // Arrange
        var result = Result<int>.Succeed(5);
        var sideEffectValue = 0;

        // Act
        var tapResult = result.Tap(x => sideEffectValue = x * 2);

        // Assert
        tapResult.Should()
            .Be(result); // Same reference
        tapResult.Value
            .Should()
            .Be(5); // Value unchanged
        sideEffectValue.Should()
            .Be(10); // Side effect executed
    }

    [Fact]
    public void Tap_WithFailure_ShouldNotExecuteSideEffect()
    {
        // Arrange
        var result = Result<int>.Fail("Failed", "Failed");
        var sideEffectExecuted = false;

        // Act
        var tapResult = result.Tap(x => sideEffectExecuted = true);

        // Assert
        tapResult.Should()
            .Be(result);
        sideEffectExecuted.Should()
            .BeFalse();
    }

    [Fact]
    public void Match_WithSuccess_ShouldExecuteSuccessFunction()
    {
        // Arrange
        var result = Result<int>.Succeed(42);

        // Act
        var output = result.Match(onSuccess: x => $"Success: {x}", onFailure: p => $"Failed: {p.Detail}");

        // Assert
        output.Should()
            .Be("Success: 42");
    }

    [Fact]
    public void Match_WithFailure_ShouldExecuteFailureFunction()
    {
        // Arrange
        var result = Result<int>.Fail("Something went wrong", "Something went wrong");

        // Act
        var output = result.Match(onSuccess: x => $"Success: {x}", onFailure: p => $"Failed: {p.Detail}");

        // Assert
        output.Should()
            .Be("Failed: Something went wrong");
    }

    [Fact]
    public void ComplexChain_ShouldWorkCorrectly()
    {
        // Arrange
        var input = "123";
        var log = new List<string>();

        // Act
        var result = Result<string>.Succeed(input)
            .Tap(x => log.Add($"Starting with: {x}"))
            .Bind(x => int.TryParse(x, out var number) ? Result<int>.Succeed(number) : Result<int>.Fail("Not a valid number", "Not a valid number"))
            .Tap(x => log.Add($"Parsed to: {x}"))
            .Map(x => x * 2)
            .Tap(x => log.Add($"Doubled to: {x}"))
            .Bind(x => x > 200 ? Result<string>.Succeed($"Large number: {x}") : Result<string>.Succeed($"Small number: {x}"))
            .Tap(x => log.Add($"Final result: {x}"));

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();
        result.Value
            .Should()
            .Be("Large number: 246");
        log.Should()
            .Equal("Starting with: 123", "Parsed to: 123", "Doubled to: 246", "Final result: Large number: 246");
    }

    [Fact]
    public void ComplexChain_WithFailure_ShouldShortCircuit()
    {
        // Arrange
        var input = "abc"; // Invalid number
        var log = new List<string>();

        // Act
        var result = Result<string>.Succeed(input)
            .Tap(x => log.Add($"Starting with: {x}"))
            .Bind(x => int.TryParse(x, out var number) ? Result<int>.Succeed(number) : Result<int>.Fail("Not a valid number", "Not a valid number"))
            .Tap(x => log.Add($"Parsed to: {x}")) // Should not execute
            .Map(x => x * 2)
            .Tap(x => log.Add($"Doubled to: {x}")) // Should not execute
            .Bind(x => Result<string>.Succeed($"Number: {x}"));

        // Assert
        result.IsSuccess
            .Should()
            .BeFalse();
        result.Problem!.Detail
            .Should()
            .Be("Not a valid number");
        log.Should()
            .Equal("Starting with: abc"); // Only first tap executed
    }

    [Fact]
    public void Try_WithSuccessfulOperation_ShouldReturnSuccess()
    {
        // Act
        var result = Result.Try(() => int.Parse("42"));

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();
        result.Value
            .Should()
            .Be(42);
    }

    [Fact]
    public void Try_WithFailingOperation_ShouldReturnFailure()
    {
        // Act
        var result = Result.Try(() => int.Parse("not-a-number"));

        // Assert
        result.IsSuccess
            .Should()
            .BeFalse();
        result.Problem
            .Should()
            .NotBeNull();
        result.Problem!.Detail
            .Should()
            .Contain("not-a-number");
    }

    [Fact]
    public void ResultTry_WithSuccessfulAction_ShouldReturnSuccess()
    {
        // Arrange
        var executed = false;

        // Act
        var result = Result.Try(() => executed = true);

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();
        executed.Should()
            .BeTrue();
    }

    [Fact]
    public void ResultTry_WithFailingAction_ShouldReturnFailure()
    {
        // Act
        var result = Result.Try(() => throw new InvalidOperationException("Test failure"));

        // Assert
        result.IsSuccess
            .Should()
            .BeFalse();
        result.Problem
            .Should()
            .NotBeNull();
        result.Problem!.Detail
            .Should()
            .Be("Test failure");
    }
}