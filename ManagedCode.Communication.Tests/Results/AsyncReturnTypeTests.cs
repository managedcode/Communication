using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.CollectionResults.Extensions;
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Commands.Execution;
using ManagedCode.Communication.Extensions;
using ManagedCode.Communication.Results.Extensions;
using Shouldly;

namespace ManagedCode.Communication.Tests.Results;

public class AsyncReturnTypeTests
{
    private const string CommandType = "test.async.return.type";
    private const int ExpectedValue = 42;

    [Test]
    public async Task ResultFactories_TaskAndValueTaskInputs_ShouldPreserveAsyncShape()
    {
        Func<Task> taskAction = static () => Task.CompletedTask;
        Func<ValueTask> valueTaskAction = static () => ValueTask.CompletedTask;
        Func<Task<int>> taskValue = static () => Task.FromResult(ExpectedValue);
        Func<ValueTask<int>> valueTaskValue = static () => ValueTask.FromResult(ExpectedValue);
        Func<Task<Result<int>>> taskResult = static () => Task.FromResult(Result<int>.Succeed(ExpectedValue));
        Func<ValueTask<Result<int>>> valueTaskResult = static () => ValueTask.FromResult(Result<int>.Succeed(ExpectedValue));

        Task<Result> taskActionResult = Result.From(taskAction);
        ValueTask<Result> valueTaskActionResult = Result.From(valueTaskAction);
        Task<Result<int>> taskValueResult = Result.From(taskValue);
        ValueTask<Result<int>> valueTaskValueResult = Result.From(valueTaskValue);
        Task<Result<int>> taskResultResult = Result<int>.From(taskResult);
        ValueTask<Result<int>> valueTaskResultResult = Result<int>.From(valueTaskResult);
        Task<Result<int>> typedTaskValueResult = Result<int>.From(taskValue);
        ValueTask<Result<int>> typedValueTaskValueResult = Result<int>.From(valueTaskValue);

        (await taskActionResult).IsSuccess.ShouldBeTrue();
        (await valueTaskActionResult).IsSuccess.ShouldBeTrue();
        (await taskValueResult).Value.ShouldBe(ExpectedValue);
        (await valueTaskValueResult).Value.ShouldBe(ExpectedValue);
        (await taskResultResult).Value.ShouldBe(ExpectedValue);
        (await valueTaskResultResult).Value.ShouldBe(ExpectedValue);
        (await typedTaskValueResult).Value.ShouldBe(ExpectedValue);
        (await typedValueTaskValueResult).Value.ShouldBe(ExpectedValue);
    }

    [Test]
    public async Task ResultExtensions_TaskAndValueTaskInputs_ShouldPreserveAsyncShape()
    {
        Func<Task> taskAction = static () => Task.CompletedTask;
        Func<ValueTask> valueTaskAction = static () => ValueTask.CompletedTask;
        Func<Task<int>> taskValue = static () => Task.FromResult(ExpectedValue);
        Func<ValueTask<int>> valueTaskValue = static () => ValueTask.FromResult(ExpectedValue);

        Task<Result> taskActionResult = taskAction.ToResultAsync();
        ValueTask<Result> valueTaskActionResult = valueTaskAction.ToResultAsync();
        Task<Result<int>> taskValueResult = taskValue.ToResultAsync();
        ValueTask<Result<int>> valueTaskValueResult = valueTaskValue.ToResultAsync();

        (await taskActionResult).IsSuccess.ShouldBeTrue();
        (await valueTaskActionResult).IsSuccess.ShouldBeTrue();
        (await taskValueResult).Value.ShouldBe(ExpectedValue);
        (await valueTaskValueResult).Value.ShouldBe(ExpectedValue);
    }

    [Test]
    public async Task CollectionFactories_TaskAndValueTaskInputs_ShouldPreserveAsyncShape()
    {
        Func<Task<int[]>> taskFactory = static () => Task.FromResult(new[] { ExpectedValue });
        Func<ValueTask<int[]>> valueTaskFactory = static () => ValueTask.FromResult(new[] { ExpectedValue });
        Func<Task<IEnumerable<int>>> taskEnumerableFactory = static () => Task.FromResult<IEnumerable<int>>(new[] { ExpectedValue });
        Func<ValueTask<IEnumerable<int>>> valueTaskEnumerableFactory = static () =>
            ValueTask.FromResult<IEnumerable<int>>(new[] { ExpectedValue });
        Func<Task<CollectionResult<int>>> taskResultFactory = static () =>
            Task.FromResult(CollectionResult<int>.Succeed(new[] { ExpectedValue }));
        Func<ValueTask<CollectionResult<int>>> valueTaskResultFactory = static () =>
            ValueTask.FromResult(CollectionResult<int>.Succeed(new[] { ExpectedValue }));

        Task<CollectionResult<int>> taskResult = CollectionResult<int>.From(taskFactory);
        ValueTask<CollectionResult<int>> valueTaskResult = CollectionResult<int>.From(valueTaskFactory);
        Task<CollectionResult<int>> taskExtensionResult = taskEnumerableFactory.ToCollectionResultAsync();
        ValueTask<CollectionResult<int>> valueTaskExtensionResult = valueTaskEnumerableFactory.ToCollectionResultAsync();
        Task<CollectionResult<int>> taskResultResult = CollectionResult<int>.From(taskResultFactory);
        ValueTask<CollectionResult<int>> valueTaskResultResult = CollectionResult<int>.From(valueTaskResultFactory);

        (await taskResult).Collection.ShouldContain(ExpectedValue);
        (await valueTaskResult).Collection.ShouldContain(ExpectedValue);
        (await taskExtensionResult).Collection.ShouldContain(ExpectedValue);
        (await valueTaskExtensionResult).Collection.ShouldContain(ExpectedValue);
        (await taskResultResult).Collection.ShouldContain(ExpectedValue);
        (await valueTaskResultResult).Collection.ShouldContain(ExpectedValue);
    }

    [Test]
    public async Task CommandExecutorExtensions_TaskAndValueTaskHandlers_ShouldPreserveAsyncShape()
    {
        var executor = new DefaultCommandExecutor(new CommandExecutionRuntime(new CommandExecutionOptions()));
        var command = Command.Create(CommandType);
        Func<Command, CancellationToken, Task<int>> taskHandler = static (_, _) => Task.FromResult(ExpectedValue);
        Func<Command, CancellationToken, ValueTask<int>> valueTaskHandler = static (_, _) => ValueTask.FromResult(ExpectedValue);
        Func<Command, CancellationToken, Task> taskAction = static (_, _) => Task.CompletedTask;
        Func<Command, CancellationToken, ValueTask> valueTaskAction = static (_, _) => ValueTask.CompletedTask;
        Func<Command, CancellationToken, Task<Result<int>>> taskResultHandler = static (_, _) =>
            Task.FromResult(Result<int>.Succeed(ExpectedValue));
        Func<Command, CancellationToken, ValueTask<Result<int>>> valueTaskResultHandler = static (_, _) =>
            ValueTask.FromResult(Result<int>.Succeed(ExpectedValue));

        Task<Result<int>> taskResult = executor.ExecuteValueAsync(command, taskHandler);
        ValueTask<Result<int>> valueTaskResult = executor.ExecuteValueAsync(command, valueTaskHandler);
        Task<Result> taskActionResult = executor.ExecuteAsync(command, taskAction);
        ValueTask<Result> valueTaskActionResult = executor.ExecuteAsync(command, valueTaskAction);
        Task<Result<int>> preservedTaskResult = executor.ExecuteResultAsync(command, taskResultHandler);
        ValueTask<Result<int>> preservedValueTaskResult = executor.ExecuteResultAsync(command, valueTaskResultHandler);

        (await taskResult).Value.ShouldBe(ExpectedValue);
        (await valueTaskResult).Value.ShouldBe(ExpectedValue);
        (await taskActionResult).IsSuccess.ShouldBeTrue();
        (await valueTaskActionResult).IsSuccess.ShouldBeTrue();
        (await preservedTaskResult).Value.ShouldBe(ExpectedValue);
        (await preservedValueTaskResult).Value.ShouldBe(ExpectedValue);
    }

    [Test]
    public async Task Railway_ValueTaskReceiverAndContinuations_ShouldRemainValueTask()
    {
        Func<int, ValueTask<int>> mapper = static value => ValueTask.FromResult(value + 1);
        Func<int, ValueTask<bool>> predicate = static value => ValueTask.FromResult(value > 0);
        Func<int, ValueTask> action = static _ => ValueTask.CompletedTask;
        Func<int, ValueTask<Result<int>>> binder = static value =>
            ValueTask.FromResult(Result<int>.Succeed(value * 2));

        ValueTask<Result<int>> pipeline = Result<int>.Succeed(ExpectedValue)
            .AsValueTask()
            .MapAsync(mapper)
            .EnsureAsync(predicate, Problem.OutOfRange())
            .DoAsync(action)
            .BindAsync(binder);

        var result = await pipeline;

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe((ExpectedValue + 1) * 2);
    }
}
