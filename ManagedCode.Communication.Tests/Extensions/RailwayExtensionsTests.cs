using System.Threading.Tasks;
using ManagedCode.Communication.Extensions;
using ManagedCode.Communication.Results.Extensions;
using Shouldly;

namespace ManagedCode.Communication.Tests.Extensions;

public class RailwayExtensionsTests
{
    #region Result Bind Tests

    [Test]
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
        chainedResult.IsSuccess.ShouldBeTrue();
        executed.ShouldBeTrue();
    }

    [Test]
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
        chainedResult.IsSuccess.ShouldBeFalse();
        chainedResult.Problem!.Title.ShouldBe("Initial error");
        executed.ShouldBeFalse();
    }

    [Test]
    public void Bind_ResultToResultT_SuccessfulChain()
    {
        // Arrange
        var result = Result.Succeed();

        // Act
        var chainedResult = result.Bind(() => Result<string>.Succeed("value"));

        // Assert
        chainedResult.IsSuccess.ShouldBeTrue();
        chainedResult.Value.ShouldBe("value");
    }

    #endregion

    #region Result<T> Map Tests

    [Test]
    public void Map_SuccessfulResult_TransformsValue()
    {
        // Arrange
        var result = Result<int>.Succeed(42);

        // Act
        var mappedResult = result.Map(x => x.ToString());

        // Assert
        mappedResult.IsSuccess.ShouldBeTrue();
        mappedResult.Value.ShouldBe("42");
    }

    [Test]
    public void Map_FailedResult_DoesNotTransform()
    {
        // Arrange
        var result = Result<int>.Fail("Error");

        // Act
        var mappedResult = result.Map(x => x.ToString());

        // Assert
        mappedResult.IsSuccess.ShouldBeFalse();
        mappedResult.Problem!.Title.ShouldBe("Error");
    }

    #endregion

    #region Result<T> Bind Tests

    [Test]
    public void Bind_SuccessfulResultT_ExecutesBinder()
    {
        // Arrange
        var result = Result<int>.Succeed(10);

        // Act
        var chainedResult = result.Bind(x => Result<string>.Succeed($"Value: {x}"));

        // Assert
        chainedResult.IsSuccess.ShouldBeTrue();
        chainedResult.Value.ShouldBe("Value: 10");
    }

    [Test]
    public void Bind_FailedResultT_DoesNotExecuteBinder()
    {
        // Arrange
        var result = Result<int>.Fail("Input error");

        // Act
        var chainedResult = result.Bind(x => Result<string>.Succeed($"Value: {x}"));

        // Assert
        chainedResult.IsSuccess.ShouldBeFalse();
        chainedResult.Problem!.Title.ShouldBe("Input error");
    }

    [Test]
    public void Bind_ResultTToResult_SuccessfulChain()
    {
        // Arrange
        var result = Result<string>.Succeed("test");

        // Act
        var chainedResult = result.Bind(value =>
            value.Length > 0 ? Result.Succeed() : Result.Fail("Empty string"));

        // Assert
        chainedResult.IsSuccess.ShouldBeTrue();
    }

    #endregion

    #region Tap Tests

    [Test]
    public void Tap_SuccessfulResult_ExecutesAction()
    {
        // Arrange
        var result = Result.Succeed();
        var executed = false;

        // Act
        var tappedResult = result.Tap(() => executed = true);

        // Assert
        tappedResult.ShouldBe(result);
        executed.ShouldBeTrue();
    }

    [Test]
    public void Tap_FailedResult_DoesNotExecuteAction()
    {
        // Arrange
        var result = Result.Fail("Error");
        var executed = false;

        // Act
        var tappedResult = result.Tap(() => executed = true);

        // Assert
        tappedResult.ShouldBe(result);
        executed.ShouldBeFalse();
    }

    [Test]
    public void Tap_SuccessfulResultT_ExecutesActionWithValue()
    {
        // Arrange
        var result = Result<int>.Succeed(42);
        var capturedValue = 0;

        // Act
        var tappedResult = result.Tap(value => capturedValue = value);

        // Assert
        tappedResult.ShouldBe(result);
        capturedValue.ShouldBe(42);
    }

    #endregion

    #region Ensure Tests

    [Test]
    public void Ensure_SuccessfulResultValidPredicate_RemainsSuccessful()
    {
        // Arrange
        var result = Result<int>.Succeed(42);
        var problem = Problem.Create("Validation failed", "Value too small", 400);

        // Act
        var ensuredResult = result.Ensure(x => x > 10, problem);

        // Assert
        ensuredResult.IsSuccess.ShouldBeTrue();
        ensuredResult.Value.ShouldBe(42);
    }

    [Test]
    public void Ensure_SuccessfulResultInvalidPredicate_BecomesFailed()
    {
        // Arrange
        var result = Result<int>.Succeed(5);
        var problem = Problem.Create("Validation failed", "Value too small", 400);

        // Act
        var ensuredResult = result.Ensure(x => x > 10, problem);

        // Assert
        ensuredResult.IsSuccess.ShouldBeFalse();
        ensuredResult.Problem.ShouldBe(problem);
    }

    [Test]
    public void Ensure_FailedResult_RemainsFailedWithOriginalProblem()
    {
        // Arrange
        var originalProblem = Problem.Create("Original error", "Something went wrong", 500);
        var result = Result<int>.Fail(originalProblem);
        var validationProblem = Problem.Create("Validation failed", "Value too small", 400);

        // Act
        var ensuredResult = result.Ensure(x => x > 10, validationProblem);

        // Assert
        ensuredResult.IsSuccess.ShouldBeFalse();
        ensuredResult.Problem.ShouldBe(originalProblem);
    }

    #endregion

    #region Else Tests

    [Test]
    public void Else_SuccessfulResult_ReturnsOriginalResult()
    {
        // Arrange
        var result = Result.Succeed();

        // Act
        var elseResult = result.Else(() => Result.Fail("Alternative"));

        // Assert
        elseResult.ShouldBe(result);
        elseResult.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public void Else_FailedResult_ReturnsAlternative()
    {
        // Arrange
        var result = Result.Fail("Original error");

        // Act
        var elseResult = result.Else(() => Result.Succeed());

        // Assert
        elseResult.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public void Else_FailedResultT_ReturnsAlternativeValue()
    {
        // Arrange
        var result = Result<string>.Fail("Error");

        // Act
        var elseResult = result.Else(() => Result<string>.Succeed("Alternative"));

        // Assert
        elseResult.IsSuccess.ShouldBeTrue();
        elseResult.Value.ShouldBe("Alternative");
    }

    #endregion

    #region Finally Tests

    [Test]
    public void Finally_SuccessfulResult_ExecutesAction()
    {
        // Arrange
        var result = Result.Succeed();
        var executed = false;

        // Act
        var finalResult = result.Finally(r => executed = true);

        // Assert
        finalResult.ShouldBe(result);
        executed.ShouldBeTrue();
    }

    [Test]
    public void Finally_FailedResult_ExecutesAction()
    {
        // Arrange
        var result = Result.Fail("Error");
        var executed = false;

        // Act
        var finalResult = result.Finally(r => executed = true);

        // Assert
        finalResult.ShouldBe(result);
        executed.ShouldBeTrue();
    }

    #endregion

    #region Pattern Matching Tests

    [Test]
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
        output.ShouldBe("Success");
    }

    [Test]
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
        output.ShouldBe("Failure: Error");
    }

    [Test]
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
        output.ShouldBe("Value: 42");
    }

    [Test]
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
        successCalled.ShouldBeTrue();
        failureCalled.ShouldBeFalse();
    }

    #endregion

    #region Async Tests

    [Test]
    public async Task BindAsync_SuccessfulResult_ExecutesNext()
    {
        // Arrange
        var resultTask = Task.FromResult(Result.Succeed());

        // Act
        var chainedResult = await resultTask.BindAsync(() => Task.FromResult(Result.Succeed()));

        // Assert
        chainedResult.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task BindAsync_FailedResult_DoesNotExecuteNext()
    {
        // Arrange
        var resultTask = Task.FromResult(Result.Fail("Error"));

        // Act
        var chainedResult = await resultTask.BindAsync(() => Task.FromResult(Result.Succeed()));

        // Assert
        chainedResult.IsSuccess.ShouldBeFalse();
        chainedResult.Problem!.Title.ShouldBe("Error");
    }

    [Test]
    public async Task MapAsync_SuccessfulResult_TransformsValue()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Succeed(42));

        // Act
        var mappedResult = await resultTask.MapAsync(value => Task.FromResult(value.ToString()));

        // Assert
        mappedResult.IsSuccess.ShouldBeTrue();
        mappedResult.Value.ShouldBe("42");
    }

    [Test]
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
        tappedResult.IsSuccess.ShouldBeTrue();
        capturedValue.ShouldBe(42);
    }

    #endregion

    #region Complex Chaining Tests

    [Test]
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
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("Value: 20");
        sideEffectCalled.ShouldBeTrue();
    }

    [Test]
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
        result.IsSuccess.ShouldBeFalse();
        result.Problem!.Title.ShouldBe("Positive check");
        sideEffectCalled.ShouldBeFalse();
    }

    #endregion
}
