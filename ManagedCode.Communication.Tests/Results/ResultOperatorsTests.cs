using System;
using System.Net;
using System.Threading.Tasks;
using Shouldly;
using ManagedCode.Communication.CollectionResultT;
using Xunit;
using ManagedCode.Communication.Tests.TestHelpers;

namespace ManagedCode.Communication.Tests.Results;

public class ResultOperatorsTests
{
    [Fact]
    public void Result_ImplicitOperator_FromException_ShouldCreateFailedResult()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        Result result = exception;

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.Detail.ShouldBe("Test exception");
        result.Problem.Title.ShouldBe("InvalidOperationException");
    }

    [Fact]
    public void Result_ImplicitOperator_FromProblem_ShouldCreateFailedResult()
    {
        // Arrange
        var problem = Problem.Create("Bad Request", "Invalid input", 400, "https://httpstatuses.io/400");

        // Act
        Result result = problem;

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldBe(problem);
    }

    [Fact]
    public void ResultT_ImplicitOperator_FromValue_ShouldCreateSuccessResult()
    {
        // Arrange
        const string value = "test value";

        // Act
        Result<string> result = value;

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(value);
        result.Problem.ShouldBeNull();
    }

    [Fact]
    public void ResultT_ImplicitOperator_FromException_ShouldCreateFailedResult()
    {
        // Arrange
        var exception = new ArgumentNullException("param");

        // Act
        Result<string> result = exception;

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Value.ShouldBeNull();
        result.Problem.ShouldNotBeNull();
        result.Problem!.Title.ShouldBe("ArgumentNullException");
    }


    [Fact]
    public void Result_From_WithSuccessfulFunc_ShouldReturnSuccessResult()
    {
        // Arrange
        var executed = false;
        Func<Result> func = () =>
        {
            executed = true;
            return Result.Succeed();
        };

        // Act
        var result = Result.From(func);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        executed.ShouldBeTrue();
    }


    [Fact]
    public async Task Result_TryAsync_WithExceptionThrowingAction_ShouldReturnFailedResult()
    {
        // Act
        var result = await Result.TryAsync(async () =>
        {
            await Task.Delay(1);
            throw new InvalidOperationException("Async exception");
        }, HttpStatusCode.BadRequest);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.StatusCode.ShouldBe(400);
        result.Problem.Detail.ShouldBe("Async exception");
    }

    [Fact]
    public async Task Result_TryAsync_WithSuccessfulAction_ShouldReturnSuccessResult()
    {
        // Arrange
        var executed = false;

        // Act
        var result = await Result.TryAsync(async () =>
        {
            await Task.Delay(1);
            executed = true;
        });

        // Assert
        result.IsSuccess.ShouldBeTrue();
        executed.ShouldBeTrue();
    }

    [Fact]
    public void Result_Fail_WithEnum_ShouldCreateFailedResultWithErrorCode()
    {
        // Act
        var result = Result.Fail(TestError.InvalidInput, "Custom error message");

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldNotBeNull();
        result.Problem!.ErrorCode.ShouldBe("InvalidInput");
        result.Problem.Detail.ShouldBe("Custom error message");
        result.Problem.Title.ShouldBe("InvalidInput");
        result.Problem.Extensions["errorType"].ShouldBe("TestError");
    }

    [Fact]
    public void ResultT_From_WithFunc_ShouldExecuteAndWrapResult()
    {
        // Act
        Func<int> func = () => 42;
        var result = Result<int>.From(func);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void ResultT_From_WithExceptionThrowingFunc_ShouldReturnFailedResult()
    {
        // Act
        Func<int> func = () => throw new DivideByZeroException("Cannot divide by zero");
        var result = Result<int>.From(func);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Value.ShouldBe(default(int));
        result.Problem.ShouldNotBeNull();
        result.Problem!.Detail.ShouldBe("Cannot divide by zero");
    }


    [Fact]
    public void CollectionResult_SucceedFromArray_ShouldCreateSuccessResult()
    {
        // Arrange
        var items = new[] { "item1", "item2", "item3" };

        // Act
        var result = CollectionResult<string>.Succeed(items);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Collection.ShouldBeEquivalentTo(items);
        result.PageNumber.ShouldBe(1);
        result.PageSize.ShouldBe(items.Length);
        result.TotalItems.ShouldBe(items.Length);
    }

    [Fact]
    public void CollectionResult_ImplicitOperator_ToBool_ShouldReturnIsSuccess()
    {
        // Arrange
        var successResult = CollectionResult<int>.Succeed(new[] { 1, 2, 3 });
        var failResult = CollectionResult<int>.Fail("Failed", "Failed");

        // Act & Assert
        ((bool)successResult).ShouldBeTrue();
        ((bool)failResult).ShouldBeFalse();
    }

    [Fact]
    public void CollectionResult_ImplicitOperator_FromException_ShouldCreateFailedResult()
    {
        // Arrange
        var exception = new InvalidOperationException("Collection error");

        // Act
        CollectionResult<string> result = exception;

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Collection.ShouldBeEmpty();
        result.Problem.ShouldNotBeNull();
        result.Problem!.Detail.ShouldBe("Collection error");
    }

    [Fact]
    public void CollectionResult_ImplicitOperator_FromProblem_ShouldCreateFailedResult()
    {
        // Arrange
        var problem = Problem.Create("Server Error", "Database error", 500, "https://httpstatuses.io/500");

        // Act
        CollectionResult<int> result = problem;

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Collection.ShouldBeEmpty();
        result.Problem.ShouldBe(problem);
    }

    [Fact]
    public void ResultT_Fail_WithEnum_ShouldCreateFailedResultWithErrorCode()
    {
        // Act
        var result = Result<string>.Fail(TestError.ResourceLocked, "Resource is locked");

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Value.ShouldBeNull();
        result.Problem.ShouldNotBeNull();
        result.Problem!.ErrorCode.ShouldBe("ResourceLocked");
        result.Problem.StatusCode.ShouldBe(400);
        result.Problem.Detail.ShouldBe("Resource is locked");
    }

    [Fact]
    public void CollectionResult_Fail_WithEnum_ShouldCreateFailedResultWithErrorCode()
    {
        // Act
        var result = CollectionResult<int>.Fail(TestError.InvalidInput, "Invalid collection query");

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Collection.ShouldBeEmpty();
        result.Problem.ShouldNotBeNull();
        result.Problem!.ErrorCode.ShouldBe("InvalidInput");
        result.Problem.Detail.ShouldBe("Invalid collection query");
    }
}