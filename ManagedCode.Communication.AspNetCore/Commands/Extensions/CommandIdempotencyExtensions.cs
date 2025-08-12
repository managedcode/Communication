using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands;

namespace ManagedCode.Communication.AspNetCore.Extensions;

/// <summary>
/// Extension methods for easier idempotent command execution
/// </summary>
public static class CommandIdempotencyExtensions
{
    /// <summary>
    /// Execute an operation idempotently with automatic result caching
    /// </summary>
    public static async Task<T> ExecuteIdempotentAsync<T>(
        this ICommandIdempotencyStore store,
        string commandId,
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteIdempotentAsync(store, commandId, operation, null, cancellationToken);
    }

    /// <summary>
    /// Execute an operation idempotently with metadata and automatic result caching
    /// </summary>
    public static async Task<T> ExecuteIdempotentAsync<T>(
        this ICommandIdempotencyStore store,
        string commandId,
        Func<Task<T>> operation,
        CommandMetadata? metadata,
        CancellationToken cancellationToken = default)
    {
        // Check for existing result
        var existingResult = await store.GetCommandResultAsync<T>(commandId, cancellationToken);
        if (existingResult != null)
        {
            return existingResult;
        }

        // Check current status
        var status = await store.GetCommandStatusAsync(commandId, cancellationToken);
        
        switch (status)
        {
            case CommandExecutionStatus.Completed:
                // Result should exist but might have been evicted, re-execute
                break;
                
            case CommandExecutionStatus.InProgress:
            case CommandExecutionStatus.Processing:
                // Wait for completion and return result
                return await WaitForCompletionAsync<T>(store, commandId, cancellationToken);
                
            case CommandExecutionStatus.Failed:
                // Previous execution failed, can retry
                break;
                
            case CommandExecutionStatus.NotFound:
            case CommandExecutionStatus.NotStarted:
            default:
                // First execution
                break;
        }

        // Set status to in progress
        await store.SetCommandStatusAsync(commandId, CommandExecutionStatus.InProgress, cancellationToken);

        try
        {
            // Execute the operation
            var result = await operation();
            
            // Store the result and mark as completed
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
    /// Execute an idempotent command with retry logic
    /// </summary>
    public static async Task<T> ExecuteIdempotentWithRetryAsync<T>(
        this ICommandIdempotencyStore store,
        string commandId,
        Func<Task<T>> operation,
        int maxRetries = 3,
        CommandMetadata? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var retryCount = 0;
        Exception? lastException = null;

        while (retryCount <= maxRetries)
        {
            try
            {
                return await store.ExecuteIdempotentAsync(
                    commandId,
                    operation,
                    metadata,
                    cancellationToken);
            }
            catch (Exception ex) when (retryCount < maxRetries)
            {
                lastException = ex;
                retryCount++;
                
                // Exponential backoff
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount - 1));
                await Task.Delay(delay, cancellationToken);
            }
        }

        // All retries exhausted
        throw lastException ?? new InvalidOperationException("Command execution failed after all retries");
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
            return (result != null, result);
        }

        return (false, default);
    }

    /// <summary>
    /// Execute operation with timeout
    /// </summary>
    public static async Task<T> ExecuteWithTimeoutAsync<T>(
        this ICommandIdempotencyStore store,
        string commandId,
        Func<Task<T>> operation,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        return await store.ExecuteIdempotentAsync(commandId, operation, combinedCts.Token);
    }

    /// <summary>
    /// Wait for command completion with polling
    /// </summary>
    private static async Task<T> WaitForCompletionAsync<T>(
        ICommandIdempotencyStore store,
        string commandId,
        CancellationToken cancellationToken,
        TimeSpan? maxWaitTime = null,
        TimeSpan? pollInterval = null)
    {
        maxWaitTime ??= TimeSpan.FromMinutes(5);
        pollInterval ??= TimeSpan.FromMilliseconds(500);

        var endTime = DateTimeOffset.UtcNow.Add(maxWaitTime.Value);

        while (DateTimeOffset.UtcNow < endTime)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var status = await store.GetCommandStatusAsync(commandId, cancellationToken);
            
            switch (status)
            {
                case CommandExecutionStatus.Completed:
                    var result = await store.GetCommandResultAsync<T>(commandId, cancellationToken);
                    if (result != null)
                        return result;
                    break;
                    
                case CommandExecutionStatus.Failed:
                    throw new InvalidOperationException($"Command {commandId} failed during execution");
                    
                case CommandExecutionStatus.NotFound:
                    throw new InvalidOperationException($"Command {commandId} was not found");
            }

            await Task.Delay(pollInterval.Value, cancellationToken);
        }

        throw new TimeoutException($"Command {commandId} did not complete within {maxWaitTime}");
    }
}