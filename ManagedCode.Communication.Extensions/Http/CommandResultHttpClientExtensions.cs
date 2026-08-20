using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands.Execution;

namespace ManagedCode.Communication.Extensions.Http;

/// <summary>
///     Executes HTTP calls as command attempts while retaining the raw-success/RFC-7807 wire contract.
/// </summary>
public static class CommandResultHttpClientExtensions
{
    /// <summary>
    ///     Sends and reads a JSON response through native command retry, timeout, idempotency, and rate limiting.
    /// </summary>
    public static Task<Result<T>> SendForResultAsync<T, TCommand>(
        this HttpClient client,
        TCommand command,
        Func<TCommand, HttpRequestMessage> requestFactory,
        CommandExecutionRuntime runtime,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(requestFactory);
        return Result<T>.ExecuteAsync(
            command,
            (current, token) => client.SendForResultAsync<T>(() => requestFactory(current), token),
            runtime,
            cancellationToken);
    }

    /// <summary>
    ///     Sends and projects a non-JSON success response through native command execution.
    /// </summary>
    public static Task<Result<T>> SendForResultAsync<T, TCommand>(
        this HttpClient client,
        TCommand command,
        Func<TCommand, HttpRequestMessage> requestFactory,
        Func<HttpResponseMessage, CancellationToken, Task<T?>> readSuccessAsync,
        CommandExecutionRuntime runtime,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(requestFactory);
        ArgumentNullException.ThrowIfNull(readSuccessAsync);
        return Result<T>.ExecuteAsync(
            command,
            (current, token) => client.SendForResultAsync(
                () => requestFactory(current),
                readSuccessAsync,
                token),
            runtime,
            cancellationToken);
    }

    /// <summary>Sends a payload-free response through native command execution.</summary>
    public static Task<Result> SendForResultAsync<TCommand>(
        this HttpClient client,
        TCommand command,
        Func<TCommand, HttpRequestMessage> requestFactory,
        CommandExecutionRuntime runtime,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(requestFactory);
        return Result.ExecuteAsync(
            command,
            (current, token) => client.SendForResultAsync(() => requestFactory(current), token),
            runtime,
            cancellationToken);
    }
}
