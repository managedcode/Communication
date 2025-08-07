using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.AspNetCore;
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
                await grain.MarkCompletedAsync<object?>(null);
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
        var result = await GetCommandResultAsync<TResult>(commandId.ToString(), cancellationToken);
        return (result != null, result);
    }
}