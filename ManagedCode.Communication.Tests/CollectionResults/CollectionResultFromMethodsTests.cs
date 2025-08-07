using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Constants;
using ManagedCode.Communication.Tests.TestHelpers;
using Xunit;

namespace ManagedCode.Communication.Tests.CollectionResults;

public class CollectionResultFromMethodsTests
{
    #region From(Func<T[]>) Tests

    [Fact]
    public void From_FuncReturningArray_ShouldCreateSuccessResult()
    {
        // Arrange
        Func<int[]> func = () => new[] { 1, 2, 3, 4, 5 };

        // Act
        var result = CollectionResult<int>.From(func);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailed.Should().BeFalse();
        result.Collection.Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5 });
        result.HasProblem.Should().BeFalse();
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(5);
        result.TotalItems.Should().Be(5);
    }

    [Fact]
    public void From_FuncReturningEmptyArray_ShouldCreateEmptySuccessResult()
    {
        // Arrange
        Func<string[]> func = () => Array.Empty<string>();

        // Act
        var result = CollectionResult<string>.From(func);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Collection.Should().BeEmpty();
        result.IsEmpty.Should().BeTrue();
        result.HasItems.Should().BeFalse();
    }

    [Fact]
    public void From_FuncThrowingException_ShouldCreateFailedResult()
    {
        // Arrange
        Func<User[]> func = () => throw new InvalidOperationException("Test exception");

        // Act
        var result = CollectionResult<User>.From(func);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.HasProblem.Should().BeTrue();
        result.ShouldHaveProblem().WithTitle("InvalidOperationException");
        result.ShouldHaveProblem().WithDetail("Test exception");
        result.ShouldHaveProblem().WithStatusCode(500);
        result.Collection.Should().BeEmpty();
    }

    [Fact]
    public void From_FuncReturningNull_ShouldCreateFailedResultDueToNullReference()
    {
        // Arrange
        Func<int[]> func = () => null!;

        // Act
        var result = CollectionResult<int>.From(func);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasProblem.Should().BeTrue();
        result.ShouldHaveProblem().WithTitle("NullReferenceException");
        result.Collection.Should().BeEmpty();
    }

    #endregion

    #region From(Func<IEnumerable<T>>) Tests

    [Fact]
    public void From_FuncReturningEnumerable_ShouldCreateSuccessResult()
    {
        // Arrange
        Func<IEnumerable<string>> func = () => new List<string> { "a", "b", "c" };

        // Act
        var result = CollectionResult<string>.From(func);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Collection.Should().BeEquivalentTo(new[] { "a", "b", "c" });
        result.TotalItems.Should().Be(3);
    }

    [Fact]
    public void From_FuncReturningLinqQuery_ShouldCreateSuccessResult()
    {
        // Arrange
        var source = new[] { 1, 2, 3, 4, 5 };
        Func<IEnumerable<int>> func = () => source.Where(x => x > 2);

        // Act
        var result = CollectionResult<int>.From(func);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Collection.Should().BeEquivalentTo(new[] { 3, 4, 5 });
    }

    [Fact]
    public void From_FuncReturningHashSet_ShouldCreateSuccessResult()
    {
        // Arrange
        Func<IEnumerable<int>> func = () => new HashSet<int> { 1, 2, 3 };

        // Act
        var result = CollectionResult<int>.From(func);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Collection.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    #endregion

    #region From(Func<CollectionResult<T>>) Tests

    [Fact]
    public void From_FuncReturningCollectionResult_ShouldReturnSameResult()
    {
        // Arrange
        var expectedResult = CollectionResult<int>.Succeed(new[] { 10, 20, 30 });
        Func<CollectionResult<int>> func = () => expectedResult;

        // Act
        var result = CollectionResult<int>.From(func);

        // Assert
        result.Should().Be(expectedResult);
        result.Collection.Should().BeEquivalentTo(new[] { 10, 20, 30 });
    }

    [Fact]
    public void From_FuncReturningFailedCollectionResult_ShouldReturnFailedResult()
    {
        // Arrange
        var problem = Problem.Create("Error", "Something went wrong", 400);
        Func<CollectionResult<string>> func = () => CollectionResult<string>.Fail(problem);

        // Act
        var result = CollectionResult<string>.From(func);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Problem.Should().Be(problem);
    }

    [Fact]
    public void From_FuncThrowingExceptionForCollectionResult_ShouldReturnFailedResult()
    {
        // Arrange
        Func<CollectionResult<int>> func = () => throw new ArgumentException("Invalid argument");

        // Act
        var result = CollectionResult<int>.From(func);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.ShouldHaveProblem().WithTitle("ArgumentException");
        result.ShouldHaveProblem().WithDetail("Invalid argument");
    }

    #endregion

    #region From(Task<T[]>) Tests

    [Fact]
    public async Task From_TaskReturningArray_ShouldCreateSuccessResult()
    {
        // Arrange
        var task = Task.FromResult(new[] { 1, 2, 3 });

        // Act
        var result = await CollectionResult<int>.From(task);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Collection.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task From_TaskWithDelay_ShouldWaitAndReturnSuccessResult()
    {
        // Arrange
        var task = Task.Run(async () =>
        {
            await Task.Delay(50);
            return new[] { "delayed", "result" };
        });

        // Act
        var result = await CollectionResult<string>.From(task);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Collection.Should().BeEquivalentTo(new[] { "delayed", "result" });
    }

    [Fact]
    public async Task From_FaultedTask_ShouldReturnFailedResult()
    {
        // Arrange
        var task = Task.FromException<User[]>(new InvalidOperationException("Task failed"));

        // Act
        var result = await CollectionResult<User>.From(task);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.ShouldHaveProblem().WithTitle("InvalidOperationException");
        result.ShouldHaveProblem().WithDetail("Task failed");
    }

    [Fact]
    public async Task From_CanceledTask_ShouldReturnFailedResult()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var task = Task.FromCanceled<int[]>(cts.Token);

        // Act
        var result = await CollectionResult<int>.From(task);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasProblem.Should().BeTrue();
    }

    #endregion

    #region From(Task<IEnumerable<T>>) Tests

    [Fact]
    public async Task From_TaskReturningEnumerable_ShouldCreateSuccessResult()
    {
        // Arrange
        var task = Task.FromResult<IEnumerable<decimal>>(new[] { 1.5m, 2.5m, 3.5m });

        // Act
        var result = await CollectionResult<decimal>.From(task);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Collection.Should().BeEquivalentTo(new[] { 1.5m, 2.5m, 3.5m });
    }

    [Fact]
    public async Task From_TaskReturningList_ShouldCreateSuccessResult()
    {
        // Arrange
        var task = Task.FromResult<IEnumerable<string>>(new List<string> { "x", "y", "z" });

        // Act
        var result = await CollectionResult<string>.From(task);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Collection.Should().BeEquivalentTo(new[] { "x", "y", "z" });
    }

    #endregion

    #region From(Task<CollectionResult<T>>) Tests

    [Fact]
    public async Task From_TaskReturningCollectionResult_ShouldReturnSameResult()
    {
        // Arrange
        var expectedResult = CollectionResult<int>.Succeed(new[] { 100, 200 });
        var task = Task.FromResult(expectedResult);

        // Act
        var result = await CollectionResult<int>.From(task);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task From_TaskReturningFailedCollectionResult_ShouldReturnFailedResult()
    {
        // Arrange
        var task = Task.FromResult(CollectionResult<string>.FailNotFound("Items not found"));

        // Act
        var result = await CollectionResult<string>.From(task);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.ShouldHaveProblem().WithStatusCode(404);
        result.ShouldHaveProblem().WithDetail("Items not found");
    }

    #endregion

    #region From(Func<Task<T[]>>) with CancellationToken Tests

    [Fact]
    public async Task From_FuncTaskWithCancellationToken_ShouldCreateSuccessResult()
    {
        // Arrange
        Func<Task<int[]>> func = async () =>
        {
            await Task.Delay(10);
            return new[] { 5, 10, 15 };
        };

        // Act
        var result = await CollectionResult<int>.From(func, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Collection.Should().BeEquivalentTo(new[] { 5, 10, 15 });
    }

    [Fact]
    public async Task From_FuncTaskWithCancellation_ShouldReturnFailedResult()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        Func<Task<string[]>> func = async () =>
        {
            await Task.Delay(1000);
            return new[] { "should", "not", "complete" };
        };
        cts.Cancel();

        // Act
        var result = await CollectionResult<string>.From(func, cts.Token);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasProblem.Should().BeTrue();
    }

    [Fact]
    public async Task From_FuncTaskThrowingException_ShouldReturnFailedResult()
    {
        // Arrange
        Func<Task<User[]>> func = () => throw new NotSupportedException("Not supported");

        // Act
        var result = await CollectionResult<User>.From(func);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.ShouldHaveProblem().WithTitle("NotSupportedException");
        result.ShouldHaveProblem().WithDetail("Not supported");
    }

    #endregion

    #region From(Func<Task<IEnumerable<T>>>) with CancellationToken Tests

    [Fact]
    public async Task From_FuncTaskEnumerable_ShouldCreateSuccessResult()
    {
        // Arrange
        Func<Task<IEnumerable<char>>> func = async () =>
        {
            await Task.Delay(10);
            return new[] { 'a', 'b', 'c' };
        };

        // Act
        var result = await CollectionResult<char>.From(func);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Collection.Should().BeEquivalentTo(new[] { 'a', 'b', 'c' });
    }

    #endregion

    #region From(Func<Task<CollectionResult<T>>>) with CancellationToken Tests

    [Fact]
    public async Task From_FuncTaskCollectionResult_ShouldReturnResult()
    {
        // Arrange
        Func<Task<CollectionResult<int>>> func = async () =>
        {
            await Task.Delay(10);
            return CollectionResult<int>.Succeed(new[] { 7, 8, 9 });
        };

        // Act
        var result = await CollectionResult<int>.From(func);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Collection.Should().BeEquivalentTo(new[] { 7, 8, 9 });
    }

    #endregion

    #region From(CollectionResult<T>) Tests

    [Fact]
    public void From_SuccessCollectionResult_ShouldReturnSameResult()
    {
        // Arrange
        var originalResult = CollectionResult<string>.Succeed(new[] { "test1", "test2" });

        // Act
        var result = CollectionResult<string>.From(originalResult);

        // Assert
        result.Should().Be(originalResult);
        result.IsSuccess.Should().BeTrue();
        result.Collection.Should().BeEquivalentTo(new[] { "test1", "test2" });
    }

    [Fact]
    public void From_FailedCollectionResultWithProblem_ShouldReturnFailedResult()
    {
        // Arrange
        var problem = Problem.Create("Error", "Details", 500);
        var originalResult = CollectionResult<int>.Fail(problem);

        // Act
        var result = CollectionResult<int>.From(originalResult);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Problem.Should().Be(problem);
    }

    [Fact]
    public void From_FailedCollectionResultWithoutProblem_ShouldReturnFailedResult()
    {
        // Arrange
        var originalResult = CollectionResult<User>.Fail();

        // Act
        var result = CollectionResult<User>.From(originalResult);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasProblem.Should().BeFalse();
    }

    #endregion

    #region From<U>(CollectionResult<U>) Tests

    [Fact]
    public void From_GenericSuccessCollectionResult_ShouldReturnSuccessResult()
    {
        // Arrange
        var collectionResult = CollectionResult<string>.Succeed(new[] { "a", "b" });

        // Act
        var result = CollectionResult<int>.From(collectionResult);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailed.Should().BeFalse();
    }

    [Fact]
    public void From_GenericFailedCollectionResultWithProblem_ShouldReturnFailedResult()
    {
        // Arrange
        var problem = Problem.Create("Generic Error", "Generic Details", 400);
        var collectionResult = CollectionResult<User>.Fail(problem);

        // Act
        var result = CollectionResult<string>.From(collectionResult);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Problem.Should().Be(problem);
    }

    [Fact]
    public void From_GenericFailedCollectionResultWithoutProblem_ShouldReturnFailedResult()
    {
        // Arrange
        var collectionResult = CollectionResult<decimal>.Fail();

        // Act
        var result = CollectionResult<int>.From(collectionResult);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasProblem.Should().BeFalse();
    }

    #endregion

    #region From(ValueTask<T[]>) Tests

    [Fact]
    public async Task From_ValueTaskReturningArray_ShouldCreateSuccessResult()
    {
        // Arrange
        var valueTask = new ValueTask<int[]>(new[] { 11, 22, 33 });

        // Act
        var result = await CollectionResult<int>.From(valueTask);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Collection.Should().BeEquivalentTo(new[] { 11, 22, 33 });
    }

    [Fact]
    public async Task From_ValueTaskWithException_ShouldReturnFailedResult()
    {
        // Arrange
        var valueTask = new ValueTask<string[]>(Task.FromException<string[]>(new ArgumentNullException("param")));

        // Act
        var result = await CollectionResult<string>.From(valueTask);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.ShouldHaveProblem().WithTitle("ArgumentNullException");
    }

    #endregion

    #region From(ValueTask<IEnumerable<T>>) Tests

    [Fact]
    public async Task From_ValueTaskReturningEnumerable_ShouldCreateSuccessResult()
    {
        // Arrange
        var valueTask = new ValueTask<IEnumerable<bool>>(new[] { true, false, true });

        // Act
        var result = await CollectionResult<bool>.From(valueTask);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Collection.Should().BeEquivalentTo(new[] { true, false, true });
    }

    #endregion

    #region From(ValueTask<CollectionResult<T>>) Tests

    [Fact]
    public async Task From_ValueTaskReturningCollectionResult_ShouldReturnSameResult()
    {
        // Arrange
        var expectedResult = CollectionResult<Guid>.Succeed(new[] { Guid.Empty });
        var valueTask = new ValueTask<CollectionResult<Guid>>(expectedResult);

        // Act
        var result = await CollectionResult<Guid>.From(valueTask);

        // Assert
        result.Should().Be(expectedResult);
    }

    #endregion

    #region From(Func<ValueTask<T[]>>) Tests

    [Fact]
    public async Task From_FuncValueTaskArray_ShouldCreateSuccessResult()
    {
        // Arrange
        Func<ValueTask<DateTime[]>> func = () => new ValueTask<DateTime[]>(new[] { DateTime.MinValue, DateTime.MaxValue });

        // Act
        var result = await CollectionResult<DateTime>.From(func);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Collection.Should().HaveCount(2);
    }

    [Fact]
    public async Task From_FuncValueTaskThrowingException_ShouldReturnFailedResult()
    {
        // Arrange
        Func<ValueTask<int[]>> func = () => throw new InvalidCastException("Invalid cast");

        // Act
        var result = await CollectionResult<int>.From(func);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.ShouldHaveProblem().WithTitle("InvalidCastException");
        result.ShouldHaveProblem().WithDetail("Invalid cast");
    }

    #endregion

    #region From(Func<ValueTask<IEnumerable<T>>>) Tests

    [Fact]
    public async Task From_FuncValueTaskEnumerable_ShouldCreateSuccessResult()
    {
        // Arrange
        Func<ValueTask<IEnumerable<double>>> func = () => 
            new ValueTask<IEnumerable<double>>(new[] { 1.1, 2.2, 3.3 });

        // Act
        var result = await CollectionResult<double>.From(func);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Collection.Should().BeEquivalentTo(new[] { 1.1, 2.2, 3.3 });
    }

    #endregion

    #region From(Func<ValueTask<CollectionResult<T>>>) Tests

    [Fact]
    public async Task From_FuncValueTaskCollectionResult_ShouldReturnResult()
    {
        // Arrange
        Func<ValueTask<CollectionResult<string>>> func = () => 
            new ValueTask<CollectionResult<string>>(CollectionResult<string>.Succeed(new[] { "value" }));

        // Act
        var result = await CollectionResult<string>.From(func);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Collection.Should().BeEquivalentTo(new[] { "value" });
    }

    #endregion

    #region Complex Type Tests

    [Fact]
    public void From_ComplexTypes_ShouldHandleCorrectly()
    {
        // Arrange
        Func<Dictionary<string, int>[]> func = () => new[]
        {
            new Dictionary<string, int> { ["key1"] = 1 },
            new Dictionary<string, int> { ["key2"] = 2 }
        };

        // Act
        var result = CollectionResult<Dictionary<string, int>>.From(func);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Collection.Should().HaveCount(2);
        result.Collection[0].Should().ContainKey("key1");
        result.Collection[1].Should().ContainKey("key2");
    }

    [Fact]
    public async Task From_TupleTypes_ShouldHandleCorrectly()
    {
        // Arrange
        var task = Task.FromResult(new[]
        {
            (1, "First"),
            (2, "Second")
        });

        // Act
        var result = await CollectionResult<(int Id, string Name)>.From(task);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Collection.Should().HaveCount(2);
        result.Collection[0].Id.Should().Be(1);
        result.Collection[0].Name.Should().Be("First");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void From_FuncWithSideEffects_ShouldExecuteOnce()
    {
        // Arrange
        int executionCount = 0;
        Func<int[]> func = () =>
        {
            executionCount++;
            return new[] { executionCount };
        };

        // Act
        var result = CollectionResult<int>.From(func);
        var collection1 = result.Collection;
        var collection2 = result.Collection;

        // Assert
        executionCount.Should().Be(1);
        collection1.Should().BeEquivalentTo(new[] { 1 });
        collection2.Should().BeEquivalentTo(new[] { 1 });
    }

    [Fact]
    public void From_LargeCollection_ShouldHandleEfficiently()
    {
        // Arrange
        Func<int[]> func = () => Enumerable.Range(1, 10000).ToArray();

        // Act
        var result = CollectionResult<int>.From(func);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TotalItems.Should().Be(10000);
        result.Collection.First().Should().Be(1);
        result.Collection.Last().Should().Be(10000);
    }

    [Fact]
    public async Task From_SlowAsyncOperation_ShouldComplete()
    {
        // Arrange
        var task = Task.Run(async () =>
        {
            await Task.Delay(100);
            return new[] { "slow", "operation" };
        });

        // Act
        var result = await CollectionResult<string>.From(task);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Collection.Should().BeEquivalentTo(new[] { "slow", "operation" });
    }

    [Fact]
    public void From_RecursiveDataStructure_ShouldHandleCorrectly()
    {
        // Arrange
        var node1 = new Node { Value = 1 };
        var node2 = new Node { Value = 2 };
        node1.Next = node2;
        node2.Next = node1; // Circular reference
        
        Func<Node[]> func = () => new[] { node1, node2 };

        // Act
        var result = CollectionResult<Node>.From(func);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Collection.Should().HaveCount(2);
        result.Collection[0].Value.Should().Be(1);
        result.Collection[1].Value.Should().Be(2);
    }

    #endregion

    #region Test Helpers

    private class User
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public int Age { get; set; }
    }

    private class Node
    {
        public int Value { get; set; }
        public Node? Next { get; set; }
    }

    #endregion
}