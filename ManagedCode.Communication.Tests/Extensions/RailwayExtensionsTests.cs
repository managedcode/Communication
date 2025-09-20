using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Communication.Extensions;
using ManagedCode.Communication.Results.Extensions;
using Xunit;

namespace ManagedCode.Communication.Tests.Extensions;

public class RailwayExtensionsTests
{
    #region Result Bind Tests

    [Fact]
    public void Bind_SuccessfulResult_ExecutesNext()
    {
        // Arrange
        var result = Result.Succeed();
        var executed = false;

        // Act
        var chainedResult = result.Bind(() =>
        {
            executed = true;
            return Result.Succeed();
        });

        // Assert
        chainedResult.IsSuccess.Should().BeTrue();
        executed.Should().BeTrue();
    }

    [Fact]
    public void Bind_FailedResult_DoesNotExecuteNext()
    {
        // Arrange
        var result = Result.Fail("Initial error");
        var executed = false;

        // Act
        var chainedResult = result.Bind(() =>
        {
            executed = true;
            return Result.Succeed();
        });

        // Assert
        chainedResult.IsSuccess.Should().BeFalse();
        chainedResult.Problem!.Title.Should().Be("Initial error");
        executed.Should().BeFalse();
    }

    [Fact]
    public void Bind_ResultToResultT_SuccessfulChain()
    {
        // Arrange
        var result = Result.Succeed();

        // Act
        var chainedResult = result.Bind(() => Result<string>.Succeed("value"));

        // Assert
        chainedResult.IsSuccess.Should().BeTrue();
        chainedResult.Value.Should().Be("value");
    }

    #endregion

    #region Result<T> Map Tests

    [Fact]
    public void Map_SuccessfulResult_TransformsValue()
    {
        // Arrange
        var result = Result<int>.Succeed(42);

        // Act
        var mappedResult = result.Map(x => x.ToString());

        // Assert
        mappedResult.IsSuccess.Should().BeTrue();
        mappedResult.Value.Should().Be("42");
    }

    [Fact]
    public void Map_FailedResult_DoesNotTransform()
    {
        // Arrange
        var result = Result<int>.Fail("Error");

        // Act
        var mappedResult = result.Map(x => x.ToString());

        // Assert
        mappedResult.IsSuccess.Should().BeFalse();
        mappedResult.Problem!.Title.Should().Be("Error");
    }

    #endregion

    #region Result<T> Bind Tests

    [Fact]
    public void Bind_SuccessfulResultT_ExecutesBinder()
    {
        // Arrange
        var result = Result<int>.Succeed(10);

        // Act
        var chainedResult = result.Bind(x => Result<string>.Succeed($"Value: {x}"));

        // Assert
        chainedResult.IsSuccess.Should().BeTrue();
        chainedResult.Value.Should().Be("Value: 10");
    }

    [Fact]
    public void Bind_FailedResultT_DoesNotExecuteBinder()
    {
        // Arrange
        var result = Result<int>.Fail("Input error");

        // Act
        var chainedResult = result.Bind(x => Result<string>.Succeed($"Value: {x}"));

        // Assert
        chainedResult.IsSuccess.Should().BeFalse();
        chainedResult.Problem!.Title.Should().Be("Input error");
    }

    [Fact]
    public void Bind_ResultTToResult_SuccessfulChain()
    {
        // Arrange
        var result = Result<string>.Succeed("test");

        // Act
        var chainedResult = result.Bind(value => 
            value.Length > 0 ? Result.Succeed() : Result.Fail("Empty string"));

        // Assert
        chainedResult.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Tap Tests

    [Fact]
    public void Tap_SuccessfulResult_ExecutesAction()
    {
        // Arrange
        var result = Result.Succeed();
        var executed = false;

        // Act
        var tappedResult = result.Tap(() => executed = true);

        // Assert
        tappedResult.Should().Be(result);
        executed.Should().BeTrue();
    }

    [Fact]
    public void Tap_FailedResult_DoesNotExecuteAction()
    {
        // Arrange
        var result = Result.Fail("Error");
        var executed = false;

        // Act
        var tappedResult = result.Tap(() => executed = true);

        // Assert
        tappedResult.Should().Be(result);
        executed.Should().BeFalse();
    }

    [Fact]
    public void Tap_SuccessfulResultT_ExecutesActionWithValue()
    {
        // Arrange
        var result = Result<int>.Succeed(42);
        var capturedValue = 0;

        // Act
        var tappedResult = result.Tap(value => capturedValue = value);

        // Assert
        tappedResult.Should().Be(result);
        capturedValue.Should().Be(42);
    }

    #endregion

    #region Ensure Tests

    [Fact]
    public void Ensure_SuccessfulResultValidPredicate_RemainsSuccessful()
    {
        // Arrange
        var result = Result<int>.Succeed(42);
        var problem = Problem.Create("Validation failed", "Value too small", 400);

        // Act
        var ensuredResult = result.Ensure(x => x > 10, problem);

        // Assert
        ensuredResult.IsSuccess.Should().BeTrue();
        ensuredResult.Value.Should().Be(42);
    }

    [Fact]
    public void Ensure_SuccessfulResultInvalidPredicate_BecomesFailed()
    {
        // Arrange
        var result = Result<int>.Succeed(5);
        var problem = Problem.Create("Validation failed", "Value too small", 400);

        // Act
        var ensuredResult = result.Ensure(x => x > 10, problem);

        // Assert
        ensuredResult.IsSuccess.Should().BeFalse();
        ensuredResult.Problem.Should().Be(problem);
    }

    [Fact]
    public void Ensure_FailedResult_RemainsFailedWithOriginalProblem()
    {
        // Arrange
        var originalProblem = Problem.Create("Original error", "Something went wrong", 500);
        var result = Result<int>.Fail(originalProblem);
        var validationProblem = Problem.Create("Validation failed", "Value too small", 400);

        // Act
        var ensuredResult = result.Ensure(x => x > 10, validationProblem);

        // Assert
        ensuredResult.IsSuccess.Should().BeFalse();
        ensuredResult.Problem.Should().Be(originalProblem);
    }

    #endregion

    #region Else Tests

    [Fact]
    public void Else_SuccessfulResult_ReturnsOriginalResult()
    {
        // Arrange
        var result = Result.Succeed();

        // Act
        var elseResult = result.Else(() => Result.Fail("Alternative"));

        // Assert
        elseResult.Should().Be(result);
        elseResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Else_FailedResult_ReturnsAlternative()
    {
        // Arrange
        var result = Result.Fail("Original error");

        // Act
        var elseResult = result.Else(() => Result.Succeed());

        // Assert
        elseResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Else_FailedResultT_ReturnsAlternativeValue()
    {
        // Arrange
        var result = Result<string>.Fail("Error");

        // Act
        var elseResult = result.Else(() => Result<string>.Succeed("Alternative"));

        // Assert
        elseResult.IsSuccess.Should().BeTrue();
        elseResult.Value.Should().Be("Alternative");
    }

    #endregion

    #region Finally Tests

    [Fact]
    public void Finally_SuccessfulResult_ExecutesAction()
    {
        // Arrange
        var result = Result.Succeed();
        var executed = false;

        // Act
        var finalResult = result.Finally(r => executed = true);

        // Assert
        finalResult.Should().Be(result);
        executed.Should().BeTrue();
    }

    [Fact]
    public void Finally_FailedResult_ExecutesAction()
    {
        // Arrange
        var result = Result.Fail("Error");
        var executed = false;

        // Act
        var finalResult = result.Finally(r => executed = true);

        // Assert
        finalResult.Should().Be(result);
        executed.Should().BeTrue();
    }

    #endregion

    #region Pattern Matching Tests

    [Fact]
    public void Match_SuccessfulResult_ExecutesOnSuccess()
    {
        // Arrange
        var result = Result.Succeed();

        // Act
        var output = result.Match(
            onSuccess: () => "Success",
            onFailure: problem => "Failure"
        );

        // Assert
        output.Should().Be("Success");
    }

    [Fact]
    public void Match_FailedResult_ExecutesOnFailure()
    {
        // Arrange
        var result = Result.Fail("Error");

        // Act
        var output = result.Match(
            onSuccess: () => "Success",
            onFailure: problem => $"Failure: {problem.Title}"
        );

        // Assert
        output.Should().Be("Failure: Error");
    }

    [Fact]
    public void Match_SuccessfulResultT_ExecutesOnSuccessWithValue()
    {
        // Arrange
        var result = Result<int>.Succeed(42);

        // Act
        var output = result.Match(
            onSuccess: value => $"Value: {value}",
            onFailure: problem => "Failed"
        );

        // Assert
        output.Should().Be("Value: 42");
    }

    [Fact]
    public void Match_SideEffects_SuccessfulResult_CallsSuccessAction()
    {
        // Arrange
        var result = Result<string>.Succeed("test");
        var successCalled = false;
        var failureCalled = false;

        // Act
        result.Match(
            onSuccess: value => successCalled = true,
            onFailure: problem => failureCalled = true
        );

        // Assert
        successCalled.Should().BeTrue();
        failureCalled.Should().BeFalse();
    }

    #endregion

    #region Async Tests

    [Fact]
    public async Task BindAsync_SuccessfulResult_ExecutesNext()
    {
        // Arrange
        var resultTask = Task.FromResult(Result.Succeed());

        // Act
        var chainedResult = await resultTask.BindAsync(() => Task.FromResult(Result.Succeed()));

        // Assert
        chainedResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task BindAsync_FailedResult_DoesNotExecuteNext()
    {
        // Arrange
        var resultTask = Task.FromResult(Result.Fail("Error"));

        // Act
        var chainedResult = await resultTask.BindAsync(() => Task.FromResult(Result.Succeed()));

        // Assert
        chainedResult.IsSuccess.Should().BeFalse();
        chainedResult.Problem!.Title.Should().Be("Error");
    }

    [Fact]
    public async Task MapAsync_SuccessfulResult_TransformsValue()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Succeed(42));

        // Act
        var mappedResult = await resultTask.MapAsync(value => Task.FromResult(value.ToString()));

        // Assert
        mappedResult.IsSuccess.Should().BeTrue();
        mappedResult.Value.Should().Be("42");
    }

    [Fact]
    public async Task TapAsync_SuccessfulResult_ExecutesAction()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Succeed(42));
        var capturedValue = 0;

        // Act
        var tappedResult = await resultTask.TapAsync(value =>
        {
            capturedValue = value;
            return Task.CompletedTask;
        });

        // Assert
        tappedResult.IsSuccess.Should().BeTrue();
        capturedValue.Should().Be(42);
    }

    #endregion

    #region Complex Chaining Tests

    [Fact]
    public void ComplexChain_SuccessPath_ExecutesAllSteps()
    {
        // Arrange
        var input = 10;
        var sideEffectCalled = false;

        // Act
        var result = Result<int>.Succeed(input)
            .Ensure(x => x > 0, Problem.Create("Positive check", "Must be positive", 400))
            .Map(x => x * 2)
            .Bind(x => x < 100 ? Result<string>.Succeed($"Value: {x}") : Result<string>.Fail("Too large"))
            .Tap(value => sideEffectCalled = true)
            .Finally(r => { /* cleanup logic */ });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("Value: 20");
        sideEffectCalled.Should().BeTrue();
    }

    [Fact]
    public void ComplexChain_FailurePath_StopsAtFirstFailure()
    {
        // Arrange
        var input = -5;
        var sideEffectCalled = false;

        // Act
        var result = Result<int>.Succeed(input)
            .Ensure(x => x > 0, Problem.Create("Positive check", "Must be positive", 400))
            .Map(x => x * 2) // Should not execute
            .Tap(value => sideEffectCalled = true) // Should not execute
            .Finally(r => { /* cleanup always runs */ });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Problem!.Title.Should().Be("Positive check");
        sideEffectCalled.Should().BeFalse();
    }

    #endregion
}
