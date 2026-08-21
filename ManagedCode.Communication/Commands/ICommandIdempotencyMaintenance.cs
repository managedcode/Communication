using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Communication.Commands;

/// <summary>
///     Optional administrative capabilities for idempotency stores that can enumerate their records. Distributed
///     stores such as Orleans intentionally do not implement this contract unless they own a real index.
/// </summary>
public interface ICommandIdempotencyMaintenance
{
    /// <summary>
    ///     Physically removes completed replay outcomes older than the supplied age. Active and indeterminate
    ///     coordination records are deliberately outside this API because deleting them can permit a duplicate side
    ///     effect.
    /// </summary>
    Task<int> CleanupCompletedCommandsAsync(
        TimeSpan maxAge,
        CancellationToken cancellationToken = default);

    /// <summary>Counts the records visible through the maintenance index by state.</summary>
    Task<Dictionary<CommandExecutionStatus, int>> GetCommandCountByStatusAsync(
        CancellationToken cancellationToken = default);
}
