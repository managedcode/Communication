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
    /// <summary>
    ///     Reads the command's current status.
    /// </summary>
    public Task<CommandExecutionStatus> GetStatusAsync()
    {
        // Check if expired
        if (state.State.ExpiresAt.HasValue && DateTime.UtcNow > state.State.ExpiresAt.Value)
        {
            return Task.FromResult(CommandExecutionStatus.NotFound);
        }

        return Task.FromResult(state.State.Status);
    }

    /// <summary>
    ///     Claims the command for processing; returns <c>false</c> when someone else already has.
    /// </summary>
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

    /// <summary>
    ///     Moves the command between statuses only if it is currently in the expected one.
    /// </summary>
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
                await MarkCompletedAsync(state.State.Result);
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

    /// <summary>
    ///     Records the command as completed and caches its result.
    /// </summary>
    public async Task MarkCompletedAsync<TResult>(TResult result)
    {
        state.State.Status = CommandExecutionStatus.Completed;
        state.State.CompletedAt = DateTime.UtcNow;
        state.State.Result = result;
        state.State.ExpiresAt = DateTime.UtcNow.AddHours(1);

        await state.WriteStateAsync();
    }

    /// <summary>
    ///     Records the command as failed.
    /// </summary>
    public async Task MarkFailedAsync(string errorMessage)
    {
        state.State.Status = CommandExecutionStatus.Failed;
        state.State.FailedAt = DateTime.UtcNow;
        state.State.ErrorMessage = errorMessage;
        state.State.ExpiresAt = DateTime.UtcNow.AddMinutes(15); // Shorter TTL for failures

        await state.WriteStateAsync();
    }

    /// <summary>
    ///     Reads the cached result, if the command completed.
    /// </summary>
    public Task<(bool success, object? result)> TryGetResultAsync()
    {
        if (state.State.Status == CommandExecutionStatus.Completed)
        {
            return Task.FromResult((true, state.State.Result));
        }

        return Task.FromResult((false, (object?)null));
    }

    /// <summary>
    ///     Forgets the command entirely.
    /// </summary>
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
    /// <summary>
    ///     Serialized member carried across the grain boundary.
    /// </summary>
    [Id(0)]
    public CommandExecutionStatus Status { get; set; } = CommandExecutionStatus.NotFound;

    /// <summary>
    ///     Serialized member carried across the grain boundary.
    /// </summary>
    [Id(1)]
    public object? Result { get; set; }

    /// <summary>
    ///     Serialized member carried across the grain boundary.
    /// </summary>
    [Id(2)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     Serialized member carried across the grain boundary.
    /// </summary>
    [Id(3)]
    public DateTime? StartedAt { get; set; }

    /// <summary>
    ///     Serialized member carried across the grain boundary.
    /// </summary>
    [Id(4)]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    ///     Serialized member carried across the grain boundary.
    /// </summary>
    [Id(5)]
    public DateTime? FailedAt { get; set; }

    /// <summary>
    ///     Serialized member carried across the grain boundary.
    /// </summary>
    [Id(6)]
    public DateTime? ExpiresAt { get; set; }
}
