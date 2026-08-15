using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Communication.Commands;

/// <summary>
///     Storage behind idempotent command execution: tracks each command's status and caches its result so a repeated call returns the original outcome instead of running again.
/// </summary>
public interface ICommandIdempotencyStore
{
    // Basic operations
    /// <summary>
    ///     Reads the current status of a command.
    /// </summary>
    Task<CommandExecutionStatus> GetCommandStatusAsync(string commandId, CancellationToken cancellationToken = default);
    /// <summary>
    ///     Writes the status of a command.
    /// </summary>
    Task SetCommandStatusAsync(string commandId, CommandExecutionStatus status, CancellationToken cancellationToken = default);
    /// <summary>
    ///     Reads the cached result of a completed command.
    /// </summary>
    Task<T?> GetCommandResultAsync<T>(string commandId, CancellationToken cancellationToken = default);
    /// <summary>
    ///     Caches the result of a completed command.
    /// </summary>
    Task SetCommandResultAsync<T>(string commandId, T result, CancellationToken cancellationToken = default);
    /// <summary>
    ///     Forgets a command entirely, status and result.
    /// </summary>
    Task RemoveCommandAsync(string commandId, CancellationToken cancellationToken = default);

    // Atomic operations to prevent race conditions
    /// <summary>
    /// Atomically sets command status only if current status matches expected value
    /// </summary>
    Task<bool> TrySetCommandStatusAsync(string commandId, CommandExecutionStatus expectedStatus, CommandExecutionStatus newStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically gets status and sets to new value if condition matches
    /// </summary>
    Task<(CommandExecutionStatus currentStatus, bool wasSet)> GetAndSetStatusAsync(string commandId, CommandExecutionStatus newStatus, CancellationToken cancellationToken = default);

    // Batch operations for better performance
    /// <summary>
    /// Get multiple command statuses in a single operation
    /// </summary>
    Task<Dictionary<string, CommandExecutionStatus>> GetMultipleStatusAsync(IEnumerable<string> commandIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get multiple command results in a single operation
    /// </summary>
    Task<Dictionary<string, T?>> GetMultipleResultsAsync<T>(IEnumerable<string> commandIds, CancellationToken cancellationToken = default);

    // Cleanup operations to prevent memory leaks
    /// <summary>
    /// Remove all commands older than specified age
    /// </summary>
    Task<int> CleanupExpiredCommandsAsync(TimeSpan maxAge, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove commands with specific status older than specified age
    /// </summary>
    Task<int> CleanupCommandsByStatusAsync(CommandExecutionStatus status, TimeSpan maxAge, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get count of commands by status for monitoring
    /// </summary>
    Task<Dictionary<CommandExecutionStatus, int>> GetCommandCountByStatusAsync(CancellationToken cancellationToken = default);
}