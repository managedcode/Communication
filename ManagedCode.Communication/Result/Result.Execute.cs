using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands.Execution;

namespace ManagedCode.Communication;

public partial struct Result
{
    /// <summary>Executes a Task handler through native command reliability.</summary>
    [OverloadResolutionPriority(1)]
    public static Task<Result> ExecuteAsync<TCommand>(
        TCommand command,
        Func<TCommand, CancellationToken, Task> handler,
        CommandExecutionRuntime runtime,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        return CommandExecutor.ExecuteAsync(command, handler, runtime, cancellationToken);
    }

    /// <summary>Executes a ValueTask handler through native command reliability.</summary>
    public static ValueTask<Result> ExecuteAsync<TCommand>(
        TCommand command,
        Func<TCommand, CancellationToken, ValueTask> handler,
        CommandExecutionRuntime runtime,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        return CommandExecutor.ExecuteAsync(command, handler, runtime, cancellationToken);
    }

    /// <summary>Executes a Task result handler and preserves its result unchanged.</summary>
    [OverloadResolutionPriority(1)]
    public static Task<Result> ExecuteAsync<TCommand>(
        TCommand command,
        Func<TCommand, CancellationToken, Task<Result>> handler,
        CommandExecutionRuntime runtime,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        return CommandExecutor.ExecuteResultAsync(command, handler, runtime, cancellationToken);
    }

    /// <summary>Executes a ValueTask result handler and preserves its result unchanged.</summary>
    public static ValueTask<Result> ExecuteAsync<TCommand>(
        TCommand command,
        Func<TCommand, CancellationToken, ValueTask<Result>> handler,
        CommandExecutionRuntime runtime,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        return CommandExecutor.ExecuteResultAsync(command, handler, runtime, cancellationToken);
    }
}
