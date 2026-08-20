using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands.Execution;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    /// <summary>Executes a Task value handler through native command reliability.</summary>
    [OverloadResolutionPriority(1)]
    public static Task<Result<T>> ExecuteAsync<TCommand>(
        TCommand command,
        Func<TCommand, CancellationToken, Task<T>> handler,
        CommandExecutionRuntime runtime,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        return CommandExecutor.ExecuteAsync(command, handler, runtime, cancellationToken);
    }

    /// <summary>Executes a ValueTask value handler through native command reliability.</summary>
    public static ValueTask<Result<T>> ExecuteAsync<TCommand>(
        TCommand command,
        Func<TCommand, CancellationToken, ValueTask<T>> handler,
        CommandExecutionRuntime runtime,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        return CommandExecutor.ExecuteAsync(command, handler, runtime, cancellationToken);
    }

    /// <summary>Executes a Task result handler and preserves its result unchanged.</summary>
    [OverloadResolutionPriority(1)]
    public static Task<Result<T>> ExecuteAsync<TCommand>(
        TCommand command,
        Func<TCommand, CancellationToken, Task<Result<T>>> handler,
        CommandExecutionRuntime runtime,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        return CommandExecutor.ExecuteResultAsync(command, handler, runtime, cancellationToken);
    }

    /// <summary>Executes a ValueTask result handler and preserves its result unchanged.</summary>
    public static ValueTask<Result<T>> ExecuteAsync<TCommand>(
        TCommand command,
        Func<TCommand, CancellationToken, ValueTask<Result<T>>> handler,
        CommandExecutionRuntime runtime,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        return CommandExecutor.ExecuteResultAsync(command, handler, runtime, cancellationToken);
    }
}
