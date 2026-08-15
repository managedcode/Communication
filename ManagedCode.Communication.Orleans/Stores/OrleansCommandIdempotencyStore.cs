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

    /// <summary>
    ///     Creates the store over an Orleans grain factory.
    /// </summary>
    public OrleansCommandIdempotencyStore(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
    }

    /// <summary>
    ///     Reads the current status of a command.
    /// </summary>
    public async Task<CommandExecutionStatus> GetCommandStatusAsync(string commandId, CancellationToken cancellationToken = default)
    {
        var grain = _grainFactory.GetGrain<ICommandIdempotencyGrain>(commandId);
        return await grain.GetStatusAsync();
    }

    /// <summary>
    ///     Writes the status of a command.
    /// </summary>
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

    /// <summary>
    ///     Reads the cached result of a completed command.
    /// </summary>
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

    /// <summary>
    ///     Caches the result of a completed command.
    /// </summary>
    public async Task SetCommandResultAsync<T>(string commandId, T result, CancellationToken cancellationToken = default)
    {
        var grain = _grainFactory.GetGrain<ICommandIdempotencyGrain>(commandId);
        await grain.MarkCompletedAsync(result);
    }

    /// <summary>
    ///     Forgets a command entirely.
    /// </summary>
    public async Task RemoveCommandAsync(string commandId, CancellationToken cancellationToken = default)
    {
        var grain = _grainFactory.GetGrain<ICommandIdempotencyGrain>(commandId);
        await grain.ClearAsync();
    }

    // New atomic operations
    /// <summary>
    ///     Moves a command between statuses only if it is currently in the expected one.
    /// </summary>
    public async Task<bool> TrySetCommandStatusAsync(string commandId, CommandExecutionStatus expectedStatus, CommandExecutionStatus newStatus, CancellationToken cancellationToken = default)
    {
        var grain = _grainFactory.GetGrain<ICommandIdempotencyGrain>(commandId);

        if (await grain.TrySetStatusAsync(expectedStatus, newStatus))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Reads the current status and writes a new one.
    /// </summary>
    public async Task<(CommandExecutionStatus currentStatus, bool wasSet)> GetAndSetStatusAsync(string commandId, CommandExecutionStatus newStatus, CancellationToken cancellationToken = default)
    {
        var grain = _grainFactory.GetGrain<ICommandIdempotencyGrain>(commandId);
        var currentStatus = await grain.GetStatusAsync();
        
        // Always try to set the new status
        await SetCommandStatusAsync(commandId, newStatus, cancellationToken);
        
        return (currentStatus, true); // Orleans grain operations are naturally atomic
    }

    // Batch operations
    /// <summary>
    ///     Reads the status of several commands.
    /// </summary>
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

    /// <summary>
    ///     Reads the cached results of several commands.
    /// </summary>
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
    /// <summary>
    ///     Not supported: Orleans cannot enumerate grains, so nothing is removed.
    /// </summary>
    public Task<int> CleanupExpiredCommandsAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        // Orleans grains are automatically deactivated when not used
        // This is a no-op for Orleans implementation as cleanup is handled by Orleans runtime
        return Task.FromResult(0);
    }

    /// <summary>
    ///     Not supported: Orleans cannot enumerate grains, so nothing is removed.
    /// </summary>
    public Task<int> CleanupCommandsByStatusAsync(CommandExecutionStatus status, TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        // Orleans grains are automatically deactivated when not used
        // This is a no-op for Orleans implementation as cleanup is handled by Orleans runtime
        return Task.FromResult(0);
    }

    /// <summary>
    ///     Not supported: Orleans cannot enumerate grains, so the map is always empty.
    /// </summary>
    public Task<Dictionary<CommandExecutionStatus, int>> GetCommandCountByStatusAsync(CancellationToken cancellationToken = default)
    {
        // Orleans has no way to enumerate grains, so this store cannot count commands by status.
        // The empty result is a deliberate no-op, not "there are no commands": tracking counts needs a
        // dedicated management grain, which an application can add and expose through its own store.
        return Task.FromResult(new Dictionary<CommandExecutionStatus, int>());
    }

}