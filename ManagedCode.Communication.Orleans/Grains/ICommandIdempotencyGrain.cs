using System;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands;
using Orleans;

namespace ManagedCode.Communication.Orleans.Grains;

/// <summary>
/// Orleans grain interface for command idempotency.
/// Each command gets its own grain instance.
/// </summary>
[Alias("ManagedCode.Communication.Orleans.Grains.ICommandIdempotencyGrain")]
public interface ICommandIdempotencyGrain : IGrainWithStringKey
{
    /// <summary>
    /// Gets the current execution status of the command.
    /// </summary>
    Task<CommandExecutionStatus> GetStatusAsync();
    
    /// <summary>
    /// Tries to start processing the command.
    /// Returns false if already processing or completed.
    /// </summary>
    Task<bool> TryStartProcessingAsync();
    
    /// <summary>
    /// Marks the command as completed with a result.
    /// </summary>
    Task MarkCompletedAsync<TResult>(TResult result);
    
    /// <summary>
    /// Marks the command as failed.
    /// </summary>
    Task MarkFailedAsync(string errorMessage);
    
    /// <summary>
    /// Gets the cached result if the command completed successfully.
    /// </summary>
    Task<(bool success, object? result)> TryGetResultAsync();
    
    /// <summary>
    /// Clears the command state from the grain.
    /// </summary>
    Task ClearAsync();
}