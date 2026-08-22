using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Communication.Commands.Execution;

/// <summary>
///     Ergonomic raw-value and concrete-result overloads for dependency-injected command executors.
/// </summary>
public static class CommandExecutorExtensions
{
    /// <summary>Executes a Task handler without a value.</summary>
    [OverloadResolutionPriority(1)]
    public static Task<Result> ExecuteAsync<TCommand>(
        this ICommandExecutor executor,
        TCommand command,
        Func<TCommand, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(handler);
        return executor.ExecuteAsync(
            command,
            async (_, token) =>
            {
                await handler(command, token).ConfigureAwait(false);
                return Result.Succeed();
            },
            cancellationToken).AsTask();
    }

    /// <summary>Executes a ValueTask handler without a value.</summary>
    public static ValueTask<Result> ExecuteAsync<TCommand>(
        this ICommandExecutor executor,
        TCommand command,
        Func<TCommand, CancellationToken, ValueTask> handler,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(handler);
        return executor.ExecuteAsync(
            command,
            async (_, token) =>
            {
                await handler(command, token).ConfigureAwait(false);
                return Result.Succeed();
            },
            cancellationToken);
    }

    /// <summary>Executes a raw Task value handler.</summary>
    [OverloadResolutionPriority(1)]
    public static Task<Result<TValue>> ExecuteValueAsync<TCommand, TValue>(
        this ICommandExecutor executor,
        TCommand command,
        Func<TCommand, CancellationToken, Task<TValue>> handler,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(handler);
        return executor.ExecuteAsync(
            command,
            async (_, token) => Result<TValue>.Succeed(await handler(command, token).ConfigureAwait(false)),
            cancellationToken).AsTask();
    }

    /// <summary>Executes a raw ValueTask value handler.</summary>
    public static ValueTask<Result<TValue>> ExecuteValueAsync<TCommand, TValue>(
        this ICommandExecutor executor,
        TCommand command,
        Func<TCommand, CancellationToken, ValueTask<TValue>> handler,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(handler);
        return executor.ExecuteAsync(
            command,
            async (_, token) => Result<TValue>.Succeed(await handler(command, token).ConfigureAwait(false)),
            cancellationToken);
    }

    /// <summary>Executes a Task result handler without changing its result.</summary>
    [OverloadResolutionPriority(1)]
    public static Task<Result> ExecuteResultAsync<TCommand>(
        this ICommandExecutor executor,
        TCommand command,
        Func<TCommand, CancellationToken, Task<Result>> handler,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(handler);
        return executor.ExecuteAsync(
            command,
            (_, token) => new ValueTask<Result>(handler(command, token)),
            cancellationToken).AsTask();
    }

    /// <summary>Executes a ValueTask result handler without changing its result.</summary>
    public static ValueTask<Result> ExecuteResultAsync<TCommand>(
        this ICommandExecutor executor,
        TCommand command,
        Func<TCommand, CancellationToken, ValueTask<Result>> handler,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(handler);
        return executor.ExecuteAsync(
            command,
            (_, token) => handler(command, token),
            cancellationToken);
    }

    /// <summary>Executes a Task result handler without changing its result.</summary>
    [OverloadResolutionPriority(1)]
    public static Task<Result<TValue>> ExecuteResultAsync<TCommand, TValue>(
        this ICommandExecutor executor,
        TCommand command,
        Func<TCommand, CancellationToken, Task<Result<TValue>>> handler,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(handler);
        return executor.ExecuteAsync(
            command,
            (_, token) => new ValueTask<Result<TValue>>(handler(command, token)),
            cancellationToken).AsTask();
    }

    /// <summary>Executes a ValueTask result handler without changing its result.</summary>
    public static ValueTask<Result<TValue>> ExecuteResultAsync<TCommand, TValue>(
        this ICommandExecutor executor,
        TCommand command,
        Func<TCommand, CancellationToken, ValueTask<Result<TValue>>> handler,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(handler);
        return executor.ExecuteAsync(
            command,
            (_, token) => handler(command, token),
            cancellationToken);
    }
}
