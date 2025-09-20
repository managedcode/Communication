using System;
using System.Collections.Generic;
using Shouldly;
using ManagedCode.Communication.Extensions;
using ManagedCode.Communication.Results.Extensions;
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
            .ShouldBeTrue();
        finalResult.Value
            .ShouldBe("13");
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
            .ShouldBeFalse();
        finalResult.Problem!.Detail
            .ShouldBe("Initial failure");
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
            .ShouldBeTrue();
        finalResult.Value
            .ShouldBe("10");
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
            .ShouldBeFalse();
        finalResult.Problem!.Detail
            .ShouldBe("Bind failure");
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
        tapResult.ShouldBe(result); // Same reference
        tapResult.Value
            .ShouldBe(5); // Value unchanged
        sideEffectValue.ShouldBe(10); // Side effect executed
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
        tapResult.ShouldBe(result);
        sideEffectExecuted.ShouldBeFalse();
    }

    [Fact]
    public void Match_WithSuccess_ShouldExecuteSuccessFunction()
    {
        // Arrange
        var result = Result<int>.Succeed(42);

        // Act
        var output = result.Match(onSuccess: x => $"Success: {x}", onFailure: p => $"Failed: {p.Detail}");

        // Assert
        output.ShouldBe("Success: 42");
    }

    [Fact]
    public void Match_WithFailure_ShouldExecuteFailureFunction()
    {
        // Arrange
        var result = Result<int>.Fail("Something went wrong", "Something went wrong");

        // Act
        var output = result.Match(onSuccess: x => $"Success: {x}", onFailure: p => $"Failed: {p.Detail}");

        // Assert
        output.ShouldBe("Failed: Something went wrong");
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
            .ShouldBeTrue();
        result.Value
            .ShouldBe("Large number: 246");
        log.ShouldBe(new[]
        {
            "Starting with: 123",
            "Parsed to: 123",
            "Doubled to: 246",
            "Final result: Large number: 246"
        });
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
            .ShouldBeFalse();
        result.Problem!.Detail
            .ShouldBe("Not a valid number");
        log.ShouldBe(new[] { "Starting with: abc" }); // Only first tap executed
    }

    [Fact]
    public void Try_WithSuccessfulOperation_ShouldReturnSuccess()
    {
        // Act
        var result = Result.Try(() => int.Parse("42"));

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
        result.Value
            .ShouldBe(42);
    }

    [Fact]
    public void Try_WithFailingOperation_ShouldReturnFailure()
    {
        // Act
        var result = Result.Try(() => int.Parse("not-a-number"));

        // Assert
        result.IsSuccess
            .ShouldBeFalse();
        result.Problem
            .ShouldNotBeNull();
        result.Problem!.Detail!
            .ShouldContain("not-a-number");
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
            .ShouldBeTrue();
        executed.ShouldBeTrue();
    }

    [Fact]
    public void ResultTry_WithFailingAction_ShouldReturnFailure()
    {
        // Act
        var result = Result.Try(() => throw new InvalidOperationException("Test failure"));

        // Assert
        result.IsSuccess
            .ShouldBeFalse();
        result.Problem
            .ShouldNotBeNull();
        result.Problem!.Detail
            .ShouldBe("Test failure");
    }
}
