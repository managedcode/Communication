using System;
using System.Globalization;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Communication.Extensions;
using ManagedCode.Communication.Results.Extensions;
using Xunit;

namespace ManagedCode.Communication.Tests;

public class ResultExtensionsTests
{
    #region Result Extensions Tests

    [Fact]
    public void Bind_WithSuccessResult_ShouldExecuteNext()
    {
        // Arrange
        var result = Result.Succeed();
        var executed = false;

        // Act
        var bound = result.Bind(() =>
        {
            executed = true;
            return Result.Succeed();
        });

        // Assert
        executed.Should()
            .BeTrue();
        bound.IsSuccess
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Bind_WithFailedResult_ShouldNotExecuteNext()
    {
        // Arrange
        var result = Result.Fail("Error");
        var executed = false;

        // Act
        var bound = result.Bind(() =>
        {
            executed = true;
            return Result.Succeed();
        });

        // Assert
        executed.Should()
            .BeFalse();
        bound.IsFailed
            .Should()
            .BeTrue();
        bound.Problem
            ?.Title
            .Should()
            .Be("Error");
    }

    [Fact]
    public void BindToGeneric_WithSuccessResult_ShouldTransformToResultT()
    {
        // Arrange
        var result = Result.Succeed();

        // Act
        var bound = result.Bind(() => Result<int>.Succeed(42));

        // Assert
        bound.IsSuccess
            .Should()
            .BeTrue();
        bound.Value
            .Should()
            .Be(42);
    }

    [Fact]
    public void Tap_WithSuccessResult_ShouldExecuteAction()
    {
        // Arrange
        var result = Result.Succeed();
        var executed = false;

        // Act
        var tapped = result.Tap(() => executed = true);

        // Assert
        executed.Should()
            .BeTrue();
        tapped.Should()
            .Be(result);
    }

    [Fact]
    public void Finally_ShouldAlwaysExecute()
    {
        // Arrange
        var successResult = Result.Succeed();
        var failedResult = Result.Fail("Error");
        var successExecuted = false;
        var failedExecuted = false;

        // Act
        successResult.Finally(r => successExecuted = true);
        failedResult.Finally(r => failedExecuted = true);

        // Assert
        successExecuted.Should()
            .BeTrue();
        failedExecuted.Should()
            .BeTrue();
    }

    [Fact]
    public void Else_WithFailedResult_ShouldReturnAlternative()
    {
        // Arrange
        var result = Result.Fail("Original error");

        // Act
        var alternative = result.Else(() => Result.Succeed());

        // Assert
        alternative.IsSuccess
            .Should()
            .BeTrue();
    }

    #endregion

    #region Result<T> Extensions Tests

    [Fact]
    public void Map_WithSuccessResult_ShouldTransformValue()
    {
        // Arrange
        var result = Result<int>.Succeed(10);

        // Act
        var mapped = result.Map(x => x * 2);

        // Assert
        mapped.IsSuccess
            .Should()
            .BeTrue();
        mapped.Value
            .Should()
            .Be(20);
    }

    [Fact]
    public void Map_WithFailedResult_ShouldPropagateFailure()
    {
        // Arrange
        var result = Result<int>.Fail("Error");

        // Act
        var mapped = result.Map(x => x * 2);

        // Assert
        mapped.IsFailed
            .Should()
            .BeTrue();
        mapped.Problem
            ?.Title
            .Should()
            .Be("Error");
    }

    [Fact]
    public void BindGeneric_WithSuccessResult_ShouldChainOperations()
    {
        // Arrange
        var result = Result<int>.Succeed(10);

        // Act
        var bound = result.Bind(x => x > 5 ? Result<string>.Succeed($"Value is {x}") : Result<string>.Fail("Value too small"));

        // Assert
        bound.IsSuccess
            .Should()
            .BeTrue();
        bound.Value
            .Should()
            .Be("Value is 10");
    }

    [Fact]
    public void Ensure_WithFailingPredicate_ShouldFail()
    {
        // Arrange
        var result = Result<int>.Succeed(3);
        var problem = Problem.Create("Value too small", "Value must be greater than 5");

        // Act
        var ensured = result.Ensure(x => x > 5, problem);

        // Assert
        ensured.IsFailed
            .Should()
            .BeTrue();
        ensured.Problem
            ?.Title
            .Should()
            .Be("Value too small");
    }

    [Fact]
    public void TapGeneric_WithSuccessResult_ShouldExecuteActionWithValue()
    {
        // Arrange
        var result = Result<string>.Succeed("test");
        string? capturedValue = null;

        // Act
        var tapped = result.Tap(value => capturedValue = value);

        // Assert
        capturedValue.Should()
            .Be("test");
        tapped.Should()
            .Be(result);
    }

    #endregion

    #region Async Extensions Tests

    [Fact]
    public async Task BindAsync_WithSuccessResult_ShouldExecuteNextAsync()
    {
        // Arrange
        var resultTask = Task.FromResult(Result.Succeed());

        // Act
        var bound = await resultTask.BindAsync(async () =>
        {
            await Task.Delay(1);
            return Result.Succeed();
        });

        // Assert
        bound.IsSuccess
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task BindAsyncGeneric_WithSuccessResult_ShouldChainAsyncOperations()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Succeed(10));

        // Act
        var bound = await resultTask.BindAsync(async x =>
        {
            await Task.Delay(1);
            return Result<string>.Succeed($"Value: {x}");
        });

        // Assert
        bound.IsSuccess
            .Should()
            .BeTrue();
        bound.Value
            .Should()
            .Be("Value: 10");
    }

    [Fact]
    public async Task MapAsync_WithSuccessResult_ShouldTransformValueAsync()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Succeed(5));

        // Act
        var mapped = await resultTask.MapAsync(async x =>
        {
            await Task.Delay(1);
            return x * 3;
        });

        // Assert
        mapped.IsSuccess
            .Should()
            .BeTrue();
        mapped.Value
            .Should()
            .Be(15);
    }

    [Fact]
    public async Task TapAsync_WithSuccessResult_ShouldExecuteAsyncAction()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<string>.Succeed("async"));
        var executed = false;

        // Act
        var tapped = await resultTask.TapAsync(async value =>
        {
            await Task.Delay(1);
            executed = true;
        });

        // Assert
        executed.Should()
            .BeTrue();
        tapped.IsSuccess
            .Should()
            .BeTrue();
        tapped.Value
            .Should()
            .Be("async");
    }

    #endregion

    #region Pattern Matching Tests

    [Fact]
    public void Match_WithResultT_ShouldReturnCorrectBranch()
    {
        // Arrange
        var successResult = Result<int>.Succeed(42);
        var failedResult = Result<int>.Fail("Error");

        // Act
        var successValue = successResult.Match(onSuccess: value => $"Success: {value}", onFailure: problem => $"Failed: {problem.Title}");

        var failedValue = failedResult.Match(onSuccess: value => $"Success: {value}", onFailure: problem => $"Failed: {problem.Title}");

        // Assert
        successValue.Should()
            .Be("Success: 42");
        failedValue.Should()
            .Be("Failed: Error");
    }

    [Fact]
    public void MatchAction_ShouldExecuteCorrectBranch()
    {
        // Arrange
        var result = Result.Succeed();
        var successExecuted = false;
        var failureExecuted = false;

        // Act
        result.Match(onSuccess: () => successExecuted = true, onFailure: _ => failureExecuted = true);

        // Assert
        successExecuted.Should()
            .BeTrue();
        failureExecuted.Should()
            .BeFalse();
    }

    #endregion

    #region Edge Cases and Complex Scenarios

    [Fact]
    public void ChainedOperations_ShouldShortCircuitOnFailure()
    {
        // Arrange
        var result = Result<int>.Succeed(10);
        var step2Executed = false;
        var step3Executed = false;

        // Act
        var final = result.Bind(x => Result<int>.Fail("Step 1 failed"))
            .Bind(x =>
            {
                step2Executed = true;
                return Result<int>.Succeed(x * 2);
            })
            .Map(x =>
            {
                step3Executed = true;
                return x + 5;
            });

        // Assert
        final.IsFailed
            .Should()
            .BeTrue();
        final.Problem
            ?.Title
            .Should()
            .Be("Step 1 failed");
        step2Executed.Should()
            .BeFalse();
        step3Executed.Should()
            .BeFalse();
    }

    [Fact]
    public void ComplexPipeline_ShouldProcessCorrectly()
    {
        // Arrange
        var input = "10";

        // Act
        var result = Result<string>.Succeed(input)
            .Map(int.Parse)
            .Ensure(x => x > 0, Problem.Create("Must be positive", "Must be positive"))
            .Map(x => x * 2)
            .Bind(x => x < 100 ? Result<double>.Succeed(x / 2.0) : Result<double>.Fail("Too large"))
            .Tap(x => Console.WriteLine($"Current value: {x}"))
            .Map(x => $"Final result: {x.ToString("F2", CultureInfo.InvariantCulture)}");

        // Assert
        result.IsSuccess
            .Should()
            .BeTrue();
        result.Value
            .Should()
            .Be("Final result: 10.00");
    }

    #endregion
}
