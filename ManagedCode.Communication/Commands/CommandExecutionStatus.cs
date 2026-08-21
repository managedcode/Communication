namespace ManagedCode.Communication.Commands;

/// <summary>
///     Lifecycle state of an idempotent command.
/// </summary>
public enum CommandExecutionStatus
{
    /// <summary>
    ///     No record of the command exists.
    /// </summary>
    NotFound,
    /// <summary>
    ///     The command is known but has not begun.
    /// </summary>
    NotStarted,
    /// <summary>
    ///     The command is being handled.
    /// </summary>
    Processing,
    /// <summary>
    ///     The command is being handled.
    /// </summary>
    InProgress,
    /// <summary>
    ///     The command finished successfully; its result is cached.
    /// </summary>
    Completed,
    /// <summary>
    ///     The command failed; it may be claimed again by a retry.
    /// </summary>
    Failed,

    /// <summary>
    ///     The previous owner may have performed the side effect, but no trustworthy terminal outcome is available.
    ///     This state is not automatically reclaimable.
    /// </summary>
    Indeterminate
}
