using System;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands;
using Orleans;
using Orleans.Runtime;

namespace ManagedCode.Communication.Orleans.Grains;

/// <summary>
/// Orleans grain implementation for command idempotency.
/// Stores command execution state and results in grain state.
/// </summary>
public class CommandIdempotencyGrain : Grain, ICommandIdempotencyGrain
{
    private readonly IPersistentState<CommandState> _state;

    public CommandIdempotencyGrain(
        [PersistentState("commandState", "commandStore")] IPersistentState<CommandState> state)
    {
        _state = state;
    }

    public Task<CommandExecutionStatus> GetStatusAsync()
    {
        // Check if expired
        if (_state.State.ExpiresAt.HasValue && DateTimeOffset.UtcNow > _state.State.ExpiresAt.Value)
        {
            return Task.FromResult(CommandExecutionStatus.NotFound);
        }

        return Task.FromResult(_state.State.Status);
    }

    public async Task<bool> TryStartProcessingAsync()
    {
        // Check if already processing or completed
        if (_state.State.Status != CommandExecutionStatus.NotFound)
        {
            return false;
        }

        _state.State.Status = CommandExecutionStatus.Processing;
        _state.State.StartedAt = DateTimeOffset.UtcNow;
        _state.State.ExpiresAt = DateTimeOffset.UtcNow.AddHours(1); // Default 1 hour expiration
        
        await _state.WriteStateAsync();
        return true;
    }

    public async Task MarkCompletedAsync<TResult>(TResult result)
    {
        _state.State.Status = CommandExecutionStatus.Completed;
        _state.State.CompletedAt = DateTimeOffset.UtcNow;
        _state.State.Result = result;
        _state.State.ExpiresAt = DateTimeOffset.UtcNow.AddHours(1);
        
        await _state.WriteStateAsync();
    }

    public async Task MarkFailedAsync(string errorMessage)
    {
        _state.State.Status = CommandExecutionStatus.Failed;
        _state.State.FailedAt = DateTimeOffset.UtcNow;
        _state.State.ErrorMessage = errorMessage;
        _state.State.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15); // Shorter TTL for failures
        
        await _state.WriteStateAsync();
    }

    public Task<(bool success, object? result)> TryGetResultAsync()
    {
        if (_state.State.Status == CommandExecutionStatus.Completed)
        {
            return Task.FromResult((true, _state.State.Result));
        }

        return Task.FromResult((false, (object?)null));
    }

    public async Task ClearAsync()
    {
        _state.State.Status = CommandExecutionStatus.NotFound;
        _state.State.Result = null;
        _state.State.ErrorMessage = null;
        _state.State.StartedAt = null;
        _state.State.CompletedAt = null;
        _state.State.FailedAt = null;
        _state.State.ExpiresAt = null;
        
        await _state.WriteStateAsync();
    }
}

/// <summary>
/// State for command idempotency grain.
/// </summary>
[GenerateSerializer]
public class CommandState
{
    [Id(0)]
    public CommandExecutionStatus Status { get; set; } = CommandExecutionStatus.NotFound;
    
    [Id(1)]
    public object? Result { get; set; }
    
    [Id(2)]
    public string? ErrorMessage { get; set; }
    
    [Id(3)]
    public DateTimeOffset? StartedAt { get; set; }
    
    [Id(4)]
    public DateTimeOffset? CompletedAt { get; set; }
    
    [Id(5)]
    public DateTimeOffset? FailedAt { get; set; }
    
    [Id(6)]
    public DateTimeOffset? ExpiresAt { get; set; }
}