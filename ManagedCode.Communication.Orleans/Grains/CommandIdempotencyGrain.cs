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
public class CommandIdempotencyGrain([PersistentState("commandState", "commandStore")] IPersistentState<CommandState> state)
    : Grain, ICommandIdempotencyGrain
{
    public Task<CommandExecutionStatus> GetStatusAsync()
    {
        // Check if expired
        if (state.State.ExpiresAt.HasValue && DateTime.UtcNow > state.State.ExpiresAt.Value)
        {
            return Task.FromResult(CommandExecutionStatus.NotFound);
        }

        return Task.FromResult(state.State.Status);
    }

    public async Task<bool> TryStartProcessingAsync()
    {
        // Reject concurrent executions
        switch (state.State.Status)
        {
            case CommandExecutionStatus.InProgress:
            case CommandExecutionStatus.Processing:
            case CommandExecutionStatus.Completed:
                return false;

            case CommandExecutionStatus.Failed:
                state.State.Result = null;
                state.State.ErrorMessage = null;
                state.State.CompletedAt = null;
                state.State.FailedAt = null;
                break;

            case CommandExecutionStatus.NotFound:
            case CommandExecutionStatus.NotStarted:
                break;

            default:
                return false;
        }

        state.State.Status = CommandExecutionStatus.Processing;
        state.State.StartedAt = DateTime.UtcNow;
        state.State.ExpiresAt = DateTime.UtcNow.AddHours(1); // Default 1 hour expiration

        await state.WriteStateAsync();
        return true;
    }

    public async Task<bool> TrySetStatusAsync(CommandExecutionStatus expectedStatus, CommandExecutionStatus newStatus)
    {
        if (state.State.Status != expectedStatus)
        {
            return false;
        }

        switch (newStatus)
        {
            case CommandExecutionStatus.InProgress:
            case CommandExecutionStatus.Processing:
                return await TryStartProcessingAsync();

            case CommandExecutionStatus.Completed:
                await MarkCompletedAsync<object?>(state.State.Result);
                return true;

            case CommandExecutionStatus.Failed:
                await MarkFailedAsync(state.State.ErrorMessage ?? "Status set to failed");
                return true;

            case CommandExecutionStatus.NotFound:
                await ClearAsync();
                return true;

            case CommandExecutionStatus.NotStarted:
                state.State.Status = CommandExecutionStatus.NotStarted;
                state.State.Result = null;
                state.State.ErrorMessage = null;
                state.State.StartedAt = null;
                state.State.CompletedAt = null;
                state.State.FailedAt = null;
                state.State.ExpiresAt = null;
                await state.WriteStateAsync();
                return true;

            default:
                return false;
        }
    }

    public async Task MarkCompletedAsync<TResult>(TResult result)
    {
        state.State.Status = CommandExecutionStatus.Completed;
        state.State.CompletedAt = DateTime.UtcNow;
        state.State.Result = result;
        state.State.ExpiresAt = DateTime.UtcNow.AddHours(1);

        await state.WriteStateAsync();
    }

    public async Task MarkFailedAsync(string errorMessage)
    {
        state.State.Status = CommandExecutionStatus.Failed;
        state.State.FailedAt = DateTime.UtcNow;
        state.State.ErrorMessage = errorMessage;
        state.State.ExpiresAt = DateTime.UtcNow.AddMinutes(15); // Shorter TTL for failures

        await state.WriteStateAsync();
    }

    public Task<(bool success, object? result)> TryGetResultAsync()
    {
        if (state.State.Status == CommandExecutionStatus.Completed)
        {
            return Task.FromResult((true, state.State.Result));
        }

        return Task.FromResult((false, (object?)null));
    }

    public async Task ClearAsync()
    {
        state.State.Status = CommandExecutionStatus.NotFound;
        state.State.Result = null;
        state.State.ErrorMessage = null;
        state.State.StartedAt = null;
        state.State.CompletedAt = null;
        state.State.FailedAt = null;
        state.State.ExpiresAt = null;

        await state.WriteStateAsync();
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
    public DateTime? StartedAt { get; set; }

    [Id(4)]
    public DateTime? CompletedAt { get; set; }

    [Id(5)]
    public DateTime? FailedAt { get; set; }

    [Id(6)]
    public DateTime? ExpiresAt { get; set; }
}
