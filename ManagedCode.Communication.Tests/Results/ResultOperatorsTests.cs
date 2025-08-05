using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Communication.CollectionResultT;
using Xunit;

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
        result.IsFailed.Should().BeTrue();
        result.Problem.Should().NotBeNull();
        result.Problem!.Detail.Should().Be("Test exception");
        result.Problem.Title.Should().Be("InvalidOperationException");
    }

    [Fact]
    public void Result_ImplicitOperator_FromProblem_ShouldCreateFailedResult()
    {
        // Arrange
        var problem = Problem.Create("https://httpstatuses.io/400", "Bad Request", 400, "Invalid input");

        // Act
        Result result = problem;

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Problem.Should().Be(problem);
    }

    [Fact]
    public void ResultT_ImplicitOperator_FromValue_ShouldCreateSuccessResult()
    {
        // Arrange
        const string value = "test value";

        // Act
        Result<string> result = value;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
        result.Problem.Should().BeNull();
    }

    [Fact]
    public void ResultT_ImplicitOperator_FromException_ShouldCreateFailedResult()
    {
        // Arrange
        var exception = new ArgumentNullException("param");

        // Act
        Result<string> result = exception;

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Problem.Should().NotBeNull();
        result.Problem!.Title.Should().Be("ArgumentNullException");
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
        result.IsSuccess.Should().BeTrue();
        executed.Should().BeTrue();
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
        result.IsFailed.Should().BeTrue();
        result.Problem.Should().NotBeNull();
        result.Problem!.StatusCode.Should().Be(400);
        result.Problem.Detail.Should().Be("Async exception");
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
        result.IsSuccess.Should().BeTrue();
        executed.Should().BeTrue();
    }

    [Fact]
    public void Result_Fail_WithEnum_ShouldCreateFailedResultWithErrorCode()
    {
        // Act
        var result = Result.Fail(TestError.InvalidInput, "Custom error message");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Problem.Should().NotBeNull();
        result.Problem!.ErrorCode.Should().Be("InvalidInput");
        result.Problem.Detail.Should().Be("Custom error message");
        result.Problem.Title.Should().Be("InvalidInput");
        result.Problem.Extensions["errorType"].Should().Be("TestError");
    }

    [Fact]
    public void ResultT_From_WithFunc_ShouldExecuteAndWrapResult()
    {
        // Act
        Func<int> func = () => 42;
        var result = Result<int>.From(func);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void ResultT_From_WithExceptionThrowingFunc_ShouldReturnFailedResult()
    {
        // Act
        Func<int> func = () => throw new DivideByZeroException("Cannot divide by zero");
        var result = Result<int>.From(func);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Value.Should().Be(default(int));
        result.Problem.Should().NotBeNull();
        result.Problem!.Detail.Should().Be("Cannot divide by zero");
    }


    [Fact]
    public void CollectionResult_SucceedFromArray_ShouldCreateSuccessResult()
    {
        // Arrange
        var items = new[] { "item1", "item2", "item3" };

        // Act
        var result = CollectionResult<string>.Succeed(items);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Collection.Should().BeEquivalentTo(items);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(items.Length);
        result.TotalItems.Should().Be(items.Length);
    }

    [Fact]
    public void CollectionResult_ImplicitOperator_ToBool_ShouldReturnIsSuccess()
    {
        // Arrange
        var successResult = CollectionResult<int>.Succeed(new[] { 1, 2, 3 });
        var failResult = CollectionResult<int>.Fail("Failed", "Failed");

        // Act & Assert
        ((bool)successResult).Should().BeTrue();
        ((bool)failResult).Should().BeFalse();
    }

    [Fact]
    public void CollectionResult_ImplicitOperator_FromException_ShouldCreateFailedResult()
    {
        // Arrange
        var exception = new InvalidOperationException("Collection error");

        // Act
        CollectionResult<string> result = exception;

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Collection.Should().BeEmpty();
        result.Problem.Should().NotBeNull();
        result.Problem!.Detail.Should().Be("Collection error");
    }

    [Fact]
    public void CollectionResult_ImplicitOperator_FromProblem_ShouldCreateFailedResult()
    {
        // Arrange
        var problem = Problem.Create("https://httpstatuses.io/500", "Server Error", 500, "Database error");

        // Act
        CollectionResult<int> result = problem;

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Collection.Should().BeEmpty();
        result.Problem.Should().Be(problem);
    }

    [Fact]
    public void ResultT_Fail_WithEnum_ShouldCreateFailedResultWithErrorCode()
    {
        // Act
        var result = Result<string>.Fail(TestError.ResourceLocked, "Resource is locked");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Problem.Should().NotBeNull();
        result.Problem!.ErrorCode.Should().Be("ResourceLocked");
        result.Problem.StatusCode.Should().Be(400);
        result.Problem.Detail.Should().Be("Resource is locked");
    }

    [Fact]
    public void CollectionResult_Fail_WithEnum_ShouldCreateFailedResultWithErrorCode()
    {
        // Act
        var result = CollectionResult<int>.Fail(TestError.InvalidInput, "Invalid collection query");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Collection.Should().BeEmpty();
        result.Problem.Should().NotBeNull();
        result.Problem!.ErrorCode.Should().Be("InvalidInput");
        result.Problem.Detail.Should().Be("Invalid collection query");
    }
}