using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Communication.Commands.Extensions;

/// <summary>
/// Extension methods for easier idempotent command execution
/// </summary>
public static class CommandIdempotencyExtensions
{
    /// <summary>
    /// Execute an operation idempotently with automatic result caching
    /// </summary>
    public static Task<T> ExecuteIdempotentAsync<T>(
        this ICommandIdempotencyStore store,
        string commandId,
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return store.ExecuteIdempotentAsync(commandId, _ => operation(), cancellationToken);
    }

    /// <summary>
    ///     Execute an operation idempotently with automatic result caching, handing the operation the
    ///     cancellation token so it can stop when the caller does.
    /// </summary>
    /// <remarks>
    ///     Prefer this overload: the parameterless <see cref="Func{TResult}" /> form cannot observe cancellation,
    ///     so a timeout or a cancelled caller has to wait for the operation to finish on its own.
    /// </remarks>
    public static async Task<T> ExecuteIdempotentAsync<T>(
        this ICommandIdempotencyStore store,
        string commandId,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);
        ArgumentNullException.ThrowIfNull(operation);

        var contendedAttempts = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentStatus = await store.GetCommandStatusAsync(commandId, cancellationToken);

            switch (currentStatus)
            {
                case CommandExecutionStatus.Completed:
                {
                    var cachedResult = await store.GetCommandResultAsync<T>(commandId, cancellationToken);
                    return cachedResult ?? default!;
                }

                case CommandExecutionStatus.InProgress:
                case CommandExecutionStatus.Processing:
                    return await WaitForCompletionAsync<T>(store, commandId, cancellationToken);

                case CommandExecutionStatus.NotFound:
                case CommandExecutionStatus.NotStarted:
                case CommandExecutionStatus.Failed:
                default:
                {
                    var claimed = await store.TrySetCommandStatusAsync(
                        commandId,
                        currentStatus,
                        CommandExecutionStatus.InProgress,
                        cancellationToken);

                    if (claimed)
                    {
                        goto ExecuteOperation;
                    }

                    // Another caller won the claim. Back off before re-reading: without this the loop spins
                    // on the store as fast as the CPU allows whenever two callers contend for the same id.
                    contendedAttempts++;
                    await Task.Delay(ContentionBackoff(contendedAttempts), cancellationToken);
                    break;
                }
            }
        }

        ExecuteOperation:
        try
        {
            var result = await operation(cancellationToken);

            // Store result and mark as completed atomically
            await store.SetCommandResultAsync(commandId, result, cancellationToken);
            await store.SetCommandStatusAsync(commandId, CommandExecutionStatus.Completed, cancellationToken);

            return result;
        }
        catch (Exception)
        {
            // Mark as failed
            await store.SetCommandStatusAsync(commandId, CommandExecutionStatus.Failed, cancellationToken);
            throw;
        }
    }

    /// <summary>
    ///     Execute an operation idempotently, retrying the whole attempt on failure with exponential backoff and jitter.
    /// </summary>
    /// <remarks>
    ///     A failed attempt leaves the command in <see cref="CommandExecutionStatus.Failed" />, which the next
    ///     attempt is allowed to claim, so retries genuinely re-run the operation rather than replaying a cached failure.
    /// </remarks>
    public static async Task<T> ExecuteIdempotentWithRetryAsync<T>(
        this ICommandIdempotencyStore store,
        string commandId,
        Func<Task<T>> operation,
        int maxRetries = 3,
        TimeSpan? baseDelay = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

        var delay = baseDelay ?? TimeSpan.FromMilliseconds(100);
        var attempt = 0;

        while (true)
        {
            try
            {
                return await store.ExecuteIdempotentAsync(commandId, operation, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // Cancellation is not a failure to retry.
            }
            catch when (attempt < maxRetries)
            {
                attempt++;

                // Exponential backoff with 80%-120% jitter so concurrent callers do not retry in lockstep.
                var backoff = TimeSpan.FromMilliseconds(
                    delay.TotalMilliseconds * Math.Pow(2, attempt - 1) * (0.8 + Random.Shared.NextDouble() * 0.4));

                await Task.Delay(backoff, cancellationToken);
            }
        }
    }

    /// <summary>
    ///     Execute an operation idempotently, giving up after <paramref name="timeout" />.
    /// </summary>
    /// <remarks>
    ///     The timeout can only interrupt an operation that observes the token it is handed. The
    ///     <see cref="Func{TResult}" /> overload cannot, so it waits for the operation to finish regardless.
    /// </remarks>
    public static Task<T> ExecuteWithTimeoutAsync<T>(
        this ICommandIdempotencyStore store,
        string commandId,
        Func<Task<T>> operation,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return store.ExecuteWithTimeoutAsync(commandId, _ => operation(), timeout, cancellationToken);
    }

    /// <summary>
    ///     Execute an operation idempotently, giving up after <paramref name="timeout" />. The operation receives
    ///     a token that is cancelled when the timeout elapses or the caller cancels.
    /// </summary>
    public static async Task<T> ExecuteWithTimeoutAsync<T>(
        this ICommandIdempotencyStore store,
        string commandId,
        Func<CancellationToken, Task<T>> operation,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            return await store.ExecuteIdempotentAsync(commandId, operation, combined.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // Distinguish "the caller cancelled" from "we ran out of time"; the latter is a TimeoutException.
            throw new TimeoutException($"Command {commandId} did not complete within {timeout}.");
        }
    }

    /// <summary>
    /// Execute multiple commands in batch
    /// </summary>
    public static async Task<Dictionary<string, T>> ExecuteBatchIdempotentAsync<T>(
        this ICommandIdempotencyStore store,
        IEnumerable<(string commandId, Func<Task<T>> operation)> operations,
        CancellationToken cancellationToken = default)
    {
        var operationsList = operations.ToList();
        var commandIds = operationsList.Select(op => op.commandId).ToList();
        
        var existingStatuses = await store.GetMultipleStatusAsync(commandIds, cancellationToken);
        var existingResults = await store.GetMultipleResultsAsync<T>(commandIds, cancellationToken);
        var results = new Dictionary<string, T>();
        var pendingOperations = new List<(string commandId, Func<Task<T>> operation)>();

        // Separate completed from pending
        foreach (var (commandId, operation) in operationsList)
        {
            if (existingStatuses.TryGetValue(commandId, out var status) && status == CommandExecutionStatus.Completed)
            {
                existingResults.TryGetValue(commandId, out var existingResult);
                results[commandId] = existingResult ?? default!;
            }
            else
            {
                pendingOperations.Add((commandId, operation));
            }
        }

        // Execute pending operations concurrently
        if (pendingOperations.Count > 0)
        {
            var tasks = pendingOperations.Select(async op =>
            {
                var result = await store.ExecuteIdempotentAsync(op.commandId, op.operation, cancellationToken);
                return (op.commandId, result);
            });

            var pendingResults = await Task.WhenAll(tasks);
            foreach (var (commandId, result) in pendingResults)
            {
                results[commandId] = result;
            }
        }

        return results;
    }

    /// <summary>
    /// Try to get cached result without executing
    /// </summary>
    public static async Task<(bool hasResult, T? result)> TryGetCachedResultAsync<T>(
        this ICommandIdempotencyStore store,
        string commandId,
        CancellationToken cancellationToken = default)
    {
        var status = await store.GetCommandStatusAsync(commandId, cancellationToken);
        
        if (status == CommandExecutionStatus.Completed)
        {
            var result = await store.GetCommandResultAsync<T>(commandId, cancellationToken);
            return (true, result);
        }

        return (false, default);
    }

    /// <summary>
    ///     Backoff before re-reading a command whose claim was lost to a concurrent caller.
    /// </summary>
    private static TimeSpan ContentionBackoff(int attempt)
    {
        // 1ms, 2ms, 4ms … capped at 50ms. Long enough to stop a hot spin, short enough to stay responsive.
        var milliseconds = Math.Min(50d, Math.Pow(2, Math.Min(attempt, 6)) / 2d);
        return TimeSpan.FromMilliseconds(milliseconds);
    }

    /// <summary>
    /// Wait for command completion with adaptive polling
    /// </summary>
    private static async Task<T> WaitForCompletionAsync<T>(
        ICommandIdempotencyStore store,
        string commandId,
        CancellationToken cancellationToken,
        TimeSpan? maxWaitTime = null)
    {
        maxWaitTime ??= TimeSpan.FromSeconds(30);
        var endTime = DateTime.UtcNow.Add(maxWaitTime.Value);
        
        // Adaptive polling: start fast, then slow down
        var pollInterval = TimeSpan.FromMilliseconds(10);
        const int maxInterval = 1000; // Max 1 second

        while (DateTime.UtcNow < endTime)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var status = await store.GetCommandStatusAsync(commandId, cancellationToken);
            
            switch (status)
            {
                case CommandExecutionStatus.Completed:
                    var result = await store.GetCommandResultAsync<T>(commandId, cancellationToken);
                    return result ?? default!;
                    
                case CommandExecutionStatus.Failed:
                    throw new InvalidOperationException($"Command {commandId} failed during execution");
                    
                case CommandExecutionStatus.NotFound:
                    throw new InvalidOperationException($"Command {commandId} was not found");
                    
                case CommandExecutionStatus.InProgress:
                case CommandExecutionStatus.Processing:
                    // Continue waiting
                    break;
                    
                default:
                    throw new InvalidOperationException($"Command {commandId} in unexpected status: {status}");
            }

            await Task.Delay(pollInterval, cancellationToken);
            
            // Increase poll interval up to max (exponential backoff for polling)
            pollInterval = TimeSpan.FromMilliseconds(Math.Min(pollInterval.TotalMilliseconds * 1.5, maxInterval));
        }

        throw new TimeoutException($"Command {commandId} did not complete within {maxWaitTime}");
    }
}
