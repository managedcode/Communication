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

                    break;
                }
            }
        }

        ExecuteOperation:
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
