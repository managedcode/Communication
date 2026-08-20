using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Communication.Commands.Execution;

/// <summary>
///     Static entry point for native command execution.
/// </summary>
public static class CommandExecutor
{
    /// <summary>Executes a Task handler and wraps its value into <see cref="Result{T}" />.</summary>
    [OverloadResolutionPriority(1)]
    public static async Task<Result<TValue>> ExecuteAsync<TCommand, TValue>(
        TCommand command,
        Func<TCommand, CancellationToken, Task<TValue>> handler,
        CommandExecutionRuntime runtime,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(handler);
        return await CommandExecutionEngine.ExecuteAsync(
                command,
                async (_, token) => Result<TValue>.Succeed(await handler(command, token).ConfigureAwait(false)),
                runtime,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Executes a ValueTask handler and wraps its value into <see cref="Result{T}" />.</summary>
    public static ValueTask<Result<TValue>> ExecuteAsync<TCommand, TValue>(
        TCommand command,
        Func<TCommand, CancellationToken, ValueTask<TValue>> handler,
        CommandExecutionRuntime runtime,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(handler);
        return CommandExecutionEngine.ExecuteAsync(
            command,
            async (_, token) => Result<TValue>.Succeed(await handler(command, token).ConfigureAwait(false)),
            runtime,
            cancellationToken);
    }

    /// <summary>Executes a Task handler that already returns <see cref="Result{T}" /> unchanged.</summary>
    [OverloadResolutionPriority(2)]
    public static Task<Result<TValue>> ExecuteAsync<TCommand, TValue>(
        TCommand command,
        Func<TCommand, CancellationToken, Task<Result<TValue>>> handler,
        CommandExecutionRuntime runtime,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        return ExecuteResultAsync(command, handler, runtime, cancellationToken);
    }

    /// <summary>Executes a ValueTask handler that already returns <see cref="Result{T}" /> unchanged.</summary>
    [OverloadResolutionPriority(2)]
    public static ValueTask<Result<TValue>> ExecuteAsync<TCommand, TValue>(
        TCommand command,
        Func<TCommand, CancellationToken, ValueTask<Result<TValue>>> handler,
        CommandExecutionRuntime runtime,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        return ExecuteResultAsync(command, handler, runtime, cancellationToken);
    }

    /// <summary>Executes a Task handler without a value and wraps completion into <see cref="Result" />.</summary>
    [OverloadResolutionPriority(1)]
    public static async Task<Result> ExecuteAsync<TCommand>(
        TCommand command,
        Func<TCommand, CancellationToken, Task> handler,
        CommandExecutionRuntime runtime,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(handler);
        return await CommandExecutionEngine.ExecuteAsync(
                command,
                async (_, token) =>
                {
                    await handler(command, token).ConfigureAwait(false);
                    return Result.Succeed();
                },
                runtime,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Executes a ValueTask handler without a value and wraps completion into <see cref="Result" />.</summary>
    public static ValueTask<Result> ExecuteAsync<TCommand>(
        TCommand command,
        Func<TCommand, CancellationToken, ValueTask> handler,
        CommandExecutionRuntime runtime,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(handler);
        return CommandExecutionEngine.ExecuteAsync(
            command,
            async (_, token) =>
            {
                await handler(command, token).ConfigureAwait(false);
                return Result.Succeed();
            },
            runtime,
            cancellationToken);
    }

    /// <summary>Executes a Task handler that already returns <see cref="Result" /> unchanged.</summary>
    [OverloadResolutionPriority(2)]
    public static Task<Result> ExecuteAsync<TCommand>(
        TCommand command,
        Func<TCommand, CancellationToken, Task<Result>> handler,
        CommandExecutionRuntime runtime,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        return ExecuteResultAsync(command, handler, runtime, cancellationToken);
    }

    /// <summary>Executes a ValueTask handler that already returns <see cref="Result" /> unchanged.</summary>
    [OverloadResolutionPriority(2)]
    public static ValueTask<Result> ExecuteAsync<TCommand>(
        TCommand command,
        Func<TCommand, CancellationToken, ValueTask<Result>> handler,
        CommandExecutionRuntime runtime,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        return ExecuteResultAsync(command, handler, runtime, cancellationToken);
    }

    /// <summary>Executes a Task handler that already returns <see cref="Result{T}" /> without wrapping it again.</summary>
    [OverloadResolutionPriority(1)]
    public static async Task<Result<TValue>> ExecuteResultAsync<TCommand, TValue>(
        TCommand command,
        Func<TCommand, CancellationToken, Task<Result<TValue>>> handler,
        CommandExecutionRuntime runtime,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(handler);
        return await CommandExecutionEngine.ExecuteAsync(
                command,
                (_, token) => new ValueTask<Result<TValue>>(handler(command, token)),
                runtime,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Executes a ValueTask handler that already returns <see cref="Result{T}" /> unchanged.</summary>
    public static ValueTask<Result<TValue>> ExecuteResultAsync<TCommand, TValue>(
        TCommand command,
        Func<TCommand, CancellationToken, ValueTask<Result<TValue>>> handler,
        CommandExecutionRuntime runtime,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(handler);
        return CommandExecutionEngine.ExecuteAsync(
            command,
            (_, token) => handler(command, token),
            runtime,
            cancellationToken);
    }

    /// <summary>Executes a Task handler that already returns <see cref="Result" /> unchanged.</summary>
    [OverloadResolutionPriority(1)]
    public static async Task<Result> ExecuteResultAsync<TCommand>(
        TCommand command,
        Func<TCommand, CancellationToken, Task<Result>> handler,
        CommandExecutionRuntime runtime,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(handler);
        return await CommandExecutionEngine.ExecuteAsync(
                command,
                (_, token) => new ValueTask<Result>(handler(command, token)),
                runtime,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Executes a ValueTask handler that already returns <see cref="Result" /> unchanged.</summary>
    public static ValueTask<Result> ExecuteResultAsync<TCommand>(
        TCommand command,
        Func<TCommand, CancellationToken, ValueTask<Result>> handler,
        CommandExecutionRuntime runtime,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(handler);
        return CommandExecutionEngine.ExecuteAsync(
            command,
            (_, token) => handler(command, token),
            runtime,
            cancellationToken);
    }
}
