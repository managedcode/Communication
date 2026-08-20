using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Results;

namespace ManagedCode.Communication.Commands.Execution;

/// <summary>
///     Executes commands through Communication's native reliability behaviors.
/// </summary>
public interface ICommandExecutor
{
    /// <summary>Executes a handler that already returns a result-like value.</summary>
    ValueTask<TResult> ExecuteAsync<TResult>(
        ICommand command,
        Func<ICommand, CancellationToken, ValueTask<TResult>> handler,
        CancellationToken cancellationToken = default)
        where TResult : struct, IResult, IResultFactory<TResult>;
}

/// <summary>
///     Default dependency-injection implementation of <see cref="ICommandExecutor" />.
/// </summary>
public sealed class DefaultCommandExecutor(CommandExecutionRuntime runtime) : ICommandExecutor
{
    /// <inheritdoc />
    public ValueTask<TResult> ExecuteAsync<TResult>(
        ICommand command,
        Func<ICommand, CancellationToken, ValueTask<TResult>> handler,
        CancellationToken cancellationToken = default)
        where TResult : struct, IResult, IResultFactory<TResult>
    {
        return CommandExecutionEngine.ExecuteAsync(command, handler, runtime, cancellationToken);
    }
}
