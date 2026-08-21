using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Constants;

namespace ManagedCode.Communication.Commands;

/// <summary>
///     Storage behind idempotent command execution: tracks each command's status and caches its result so a repeated call returns the original outcome instead of running again.
/// </summary>
public interface ICommandIdempotencyStore
{
    /// <summary>
    ///     Atomically returns a terminal outcome, reports an existing owner, or claims execution for the caller.
    /// </summary>
    Task<CommandIdempotencyAcquireResult<T>> TryAcquireAsync<T>(
        CommandIdempotencyDescriptor descriptor,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Atomically persists a terminal outcome only when the supplied fenced claim still owns the execution.
    /// </summary>
    Task<bool> TryCompleteAsync<T>(
        CommandIdempotencyClaim claim,
        T outcome,
        TimeSpan retention,
        CancellationToken cancellationToken = default);

    /// <summary>Extends the active claim lease when the supplied fence still owns the execution.</summary>
    Task<bool> TryRenewAsync(
        CommandIdempotencyClaim claim,
        TimeSpan lease,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Atomically records that the side-effect outcome is unknown, preventing automatic duplicate execution.
    /// </summary>
    Task<bool> TryMarkIndeterminateAsync(
        CommandIdempotencyClaim claim,
        Problem problem,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Releases a claim only when no business handler was invoked and the supplied fence still owns it.
    /// </summary>
    Task<bool> TryReleaseAsync(
        CommandIdempotencyClaim claim,
        CancellationToken cancellationToken = default);

    /// <summary>Explicitly resolves an indeterminate record with a known terminal outcome.</summary>
    Task<bool> TryResolveIndeterminateAsync<T>(
        CommandIdempotencyDescriptor descriptor,
        T outcome,
        TimeSpan retention,
        CancellationToken cancellationToken = default) =>
        Task.FromException<bool>(new NotSupportedException(
            ProblemConstants.CommandExecutionMessages.UnsupportedIndeterminateResolution));

    /// <summary>
    ///     Explicitly abandons an indeterminate record after an operator has established that retry is safe.
    /// </summary>
    Task<bool> TryResetIndeterminateAsync(
        CommandIdempotencyDescriptor descriptor,
        CancellationToken cancellationToken = default) =>
        Task.FromException<bool>(new NotSupportedException(
            ProblemConstants.CommandExecutionMessages.UnsupportedIndeterminateReset));

}
