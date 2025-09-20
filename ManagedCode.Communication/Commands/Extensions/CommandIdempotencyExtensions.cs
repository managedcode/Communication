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
    public static async Task<T> ExecuteIdempotentAsync<T>(
        this ICommandIdempotencyStore store,
        string commandId,
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        // Fast path: check for existing completed result
        var existingResult = await store.GetCommandResultAsync<T>(commandId, cancellationToken);
        if (existingResult != null)
        {
            return existingResult;
        }

        // Atomically try to claim the command for execution
        var (currentStatus, wasSet) = await store.GetAndSetStatusAsync(
            commandId, 
            CommandExecutionStatus.InProgress, 
            cancellationToken);

        switch (currentStatus)
        {
            case CommandExecutionStatus.Completed:
                // Result exists but might have been evicted, get it again
                existingResult = await store.GetCommandResultAsync<T>(commandId, cancellationToken);
                return existingResult ?? throw new InvalidOperationException($"Command {commandId} marked as completed but result not found");
                
            case CommandExecutionStatus.InProgress:
            case CommandExecutionStatus.Processing:
                // Another thread is executing, wait for completion
                return await WaitForCompletionAsync<T>(store, commandId, cancellationToken);
                
            case CommandExecutionStatus.Failed:
                // Previous execution failed, we can retry (wasSet should be true)
                if (!wasSet)
                {
                    // Race condition - another thread claimed it
                    return await WaitForCompletionAsync<T>(store, commandId, cancellationToken);
                }
                break;
                
            case CommandExecutionStatus.NotFound:
            case CommandExecutionStatus.NotStarted:
            default:
                // First execution (wasSet should be true)
                if (!wasSet)
                {
                    // Race condition - another thread claimed it
                    return await WaitForCompletionAsync<T>(store, commandId, cancellationToken);
                }
                break;
        }

        // We successfully claimed the command for execution
        try
        {
            var result = await operation();
            
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
    /// Execute multiple commands in batch
    /// </summary>
    public static async Task<Dictionary<string, T>> ExecuteBatchIdempotentAsync<T>(
        this ICommandIdempotencyStore store,
        IEnumerable<(string commandId, Func<Task<T>> operation)> operations,
        CancellationToken cancellationToken = default)
    {
        var operationsList = operations.ToList();
        var commandIds = operationsList.Select(op => op.commandId).ToList();
        
        // Check for existing results in batch
        var existingResults = await store.GetMultipleResultsAsync<T>(commandIds, cancellationToken);
        var results = new Dictionary<string, T>();
        var pendingOperations = new List<(string commandId, Func<Task<T>> operation)>();

        // Separate completed from pending
        foreach (var (commandId, operation) in operationsList)
        {
            if (existingResults.TryGetValue(commandId, out var existingResult) && existingResult != null)
            {
                results[commandId] = existingResult;
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
            return (result != null, result);
        }

        return (false, default);
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
        var endTime = DateTimeOffset.UtcNow.Add(maxWaitTime.Value);
        
        // Adaptive polling: start fast, then slow down
        var pollInterval = TimeSpan.FromMilliseconds(10);
        const int maxInterval = 1000; // Max 1 second

        while (DateTimeOffset.UtcNow < endTime)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var status = await store.GetCommandStatusAsync(commandId, cancellationToken);
            
            switch (status)
            {
                case CommandExecutionStatus.Completed:
                    var result = await store.GetCommandResultAsync<T>(commandId, cancellationToken);
                    return result ?? throw new InvalidOperationException($"Command {commandId} completed but result not found");
                    
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