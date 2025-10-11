using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Orleans.Grains;
using Orleans;

namespace ManagedCode.Communication.Orleans.Stores;

/// <summary>
/// Orleans grain-based implementation of command idempotency store.
/// Uses Orleans grains for distributed state management.
/// </summary>
public class OrleansCommandIdempotencyStore : ICommandIdempotencyStore
{
    private readonly IGrainFactory _grainFactory;

    public OrleansCommandIdempotencyStore(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
    }

    public async Task<CommandExecutionStatus> GetCommandStatusAsync(string commandId, CancellationToken cancellationToken = default)
    {
        var grain = _grainFactory.GetGrain<ICommandIdempotencyGrain>(commandId);
        return await grain.GetStatusAsync();
    }

    public async Task SetCommandStatusAsync(string commandId, CommandExecutionStatus status, CancellationToken cancellationToken = default)
    {
        var grain = _grainFactory.GetGrain<ICommandIdempotencyGrain>(commandId);
        switch (status)
        {
            case CommandExecutionStatus.InProgress:
            case CommandExecutionStatus.Processing:
                await grain.TryStartProcessingAsync();
                break;
            case CommandExecutionStatus.Failed:
                await grain.MarkFailedAsync("Status set to failed");
                break;
            case CommandExecutionStatus.Completed:
                var (hasResult, result) = await grain.TryGetResultAsync();
                if (hasResult)
                {
                    await grain.MarkCompletedAsync(result);
                }
                else
                {
                    await grain.MarkCompletedAsync<object?>(default);
                }
                break;
            case CommandExecutionStatus.NotStarted:
            case CommandExecutionStatus.NotFound:
            default:
                // No action needed for NotStarted/NotFound
                break;
        }
    }

    public async Task<T?> GetCommandResultAsync<T>(string commandId, CancellationToken cancellationToken = default)
    {
        var grain = _grainFactory.GetGrain<ICommandIdempotencyGrain>(commandId);
        var (success, result) = await grain.TryGetResultAsync();
        
        if (success && result is T typedResult)
        {
            return typedResult;
        }

        return default;
    }

    public async Task SetCommandResultAsync<T>(string commandId, T result, CancellationToken cancellationToken = default)
    {
        var grain = _grainFactory.GetGrain<ICommandIdempotencyGrain>(commandId);
        await grain.MarkCompletedAsync(result);
    }

    public async Task RemoveCommandAsync(string commandId, CancellationToken cancellationToken = default)
    {
        var grain = _grainFactory.GetGrain<ICommandIdempotencyGrain>(commandId);
        await grain.ClearAsync();
    }

    // New atomic operations
    public async Task<bool> TrySetCommandStatusAsync(string commandId, CommandExecutionStatus expectedStatus, CommandExecutionStatus newStatus, CancellationToken cancellationToken = default)
    {
        var grain = _grainFactory.GetGrain<ICommandIdempotencyGrain>(commandId);
        var currentStatus = await grain.GetStatusAsync();
        
        if (currentStatus == expectedStatus)
        {
            await SetCommandStatusAsync(commandId, newStatus, cancellationToken);
            return true;
        }
        
        return false;
    }

    public async Task<(CommandExecutionStatus currentStatus, bool wasSet)> GetAndSetStatusAsync(string commandId, CommandExecutionStatus newStatus, CancellationToken cancellationToken = default)
    {
        var grain = _grainFactory.GetGrain<ICommandIdempotencyGrain>(commandId);
        var currentStatus = await grain.GetStatusAsync();
        
        // Always try to set the new status
        await SetCommandStatusAsync(commandId, newStatus, cancellationToken);
        
        return (currentStatus, true); // Orleans grain operations are naturally atomic
    }

    // Batch operations
    public async Task<Dictionary<string, CommandExecutionStatus>> GetMultipleStatusAsync(IEnumerable<string> commandIds, CancellationToken cancellationToken = default)
    {
        var tasks = commandIds.Select(async commandId =>
        {
            var status = await GetCommandStatusAsync(commandId, cancellationToken);
            return (commandId, status);
        });
        
        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(r => r.commandId, r => r.status);
    }

    public async Task<Dictionary<string, T?>> GetMultipleResultsAsync<T>(IEnumerable<string> commandIds, CancellationToken cancellationToken = default)
    {
        var tasks = commandIds.Select(async commandId =>
        {
            var result = await GetCommandResultAsync<T>(commandId, cancellationToken);
            return (commandId, result);
        });
        
        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(r => r.commandId, r => r.result);
    }

    // Cleanup operations - NOTE: Orleans grains have automatic lifecycle management
    public Task<int> CleanupExpiredCommandsAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        // Orleans grains are automatically deactivated when not used
        // This is a no-op for Orleans implementation as cleanup is handled by Orleans runtime
        return Task.FromResult(0);
    }

    public Task<int> CleanupCommandsByStatusAsync(CommandExecutionStatus status, TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        // Orleans grains are automatically deactivated when not used
        // This is a no-op for Orleans implementation as cleanup is handled by Orleans runtime
        return Task.FromResult(0);
    }

    public Task<Dictionary<CommandExecutionStatus, int>> GetCommandCountByStatusAsync(CancellationToken cancellationToken = default)
    {
        // Orleans doesn't provide built-in way to enumerate all grains
        // This would require a separate management grain to track command counts
        // For now, return empty dictionary - implementers can override if needed
        return Task.FromResult(new Dictionary<CommandExecutionStatus, int>());
    }

    // Legacy methods for backward compatibility
    public async Task<CommandExecutionStatus> GetStatusAsync(Guid commandId, CancellationToken cancellationToken = default)
    {
        return await GetCommandStatusAsync(commandId.ToString(), cancellationToken);
    }

    public async Task<bool> TryStartProcessingAsync(Guid commandId, CancellationToken cancellationToken = default)
    {
        var grain = _grainFactory.GetGrain<ICommandIdempotencyGrain>(commandId.ToString());
        return await grain.TryStartProcessingAsync();
    }

    public async Task MarkCompletedAsync<TResult>(Guid commandId, TResult result, CancellationToken cancellationToken = default)
    {
        await SetCommandResultAsync(commandId.ToString(), result, cancellationToken);
    }

    public async Task MarkFailedAsync(Guid commandId, string errorMessage, CancellationToken cancellationToken = default)
    {
        var grain = _grainFactory.GetGrain<ICommandIdempotencyGrain>(commandId.ToString());
        await grain.MarkFailedAsync(errorMessage);
    }

    public async Task<(bool success, TResult? result)> TryGetResultAsync<TResult>(Guid commandId, CancellationToken cancellationToken = default)
    {
        var grain = _grainFactory.GetGrain<ICommandIdempotencyGrain>(commandId.ToString());
        var status = await grain.GetStatusAsync();

        if (status != CommandExecutionStatus.Completed)
        {
            return (false, default);
        }

        var (_, result) = await grain.TryGetResultAsync();

        if (result is TResult typedResult)
        {
            return (true, typedResult);
        }

        return (true, default);
    }
}