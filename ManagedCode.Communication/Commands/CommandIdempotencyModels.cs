using System;

namespace ManagedCode.Communication.Commands;

/// <summary>
///     Describes one idempotent operation independently of its storage implementation.
/// </summary>
public sealed record CommandIdempotencyDescriptor(
    string StorageKey,
    string Operation,
    string Fingerprint,
    string ResultContract,
    TimeSpan ClaimLease,
    TimeSpan OutcomeRetention)
{
    /// <summary>Validates the descriptor before it crosses a storage boundary.</summary>
    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(StorageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(Operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(Fingerprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(ResultContract);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ClaimLease, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(OutcomeRetention, TimeSpan.Zero);
    }
}

/// <summary>
///     Identifies the caller that currently owns an idempotent execution. Stores use the token as a fence so a stale
///     owner cannot overwrite a newer outcome.
/// </summary>
public sealed record CommandIdempotencyClaim(
    string StorageKey,
    string Operation,
    string Fingerprint,
    string ResultContract,
    Guid OwnerToken,
    long Generation);

/// <summary>Result of atomically reading or claiming an idempotent operation.</summary>
public enum CommandIdempotencyAcquireState
{
    /// <summary>The caller acquired execution ownership.</summary>
    Acquired,

    /// <summary>Another owner is still executing the operation.</summary>
    Running,

    /// <summary>A terminal outcome is available.</summary>
    Completed,

    /// <summary>The storage key belongs to a different operation, request fingerprint, or result contract.</summary>
    Conflict,

    /// <summary>The prior owner may have performed the side effect, but its terminal outcome is unknown.</summary>
    Indeterminate
}

/// <summary>
///     Atomic read/claim result. <see cref="HasOutcome" /> distinguishes a stored <see langword="null" /> or default
///     value from a missing/corrupt terminal payload.
/// </summary>
public sealed record CommandIdempotencyAcquireResult<T>
{
    private CommandIdempotencyAcquireResult(
        CommandIdempotencyAcquireState state,
        CommandIdempotencyClaim? claim,
        bool hasOutcome,
        T? outcome,
        Problem? problem)
    {
        State = state;
        Claim = claim;
        HasOutcome = hasOutcome;
        Outcome = outcome;
        Problem = problem;
    }

    /// <summary>Current coordination state.</summary>
    public CommandIdempotencyAcquireState State { get; }

    /// <summary>Execution claim when <see cref="State" /> is <see cref="CommandIdempotencyAcquireState.Acquired" />.</summary>
    public CommandIdempotencyClaim? Claim { get; }

    /// <summary>Whether a terminal payload was present, including a deliberately stored null/default value.</summary>
    public bool HasOutcome { get; }

    /// <summary>Cached terminal outcome.</summary>
    public T? Outcome { get; }

    /// <summary>Conflict, corruption, or indeterminate-state details.</summary>
    public Problem? Problem { get; }

    /// <summary>Creates an acquired result.</summary>
    public static CommandIdempotencyAcquireResult<T> Acquired(CommandIdempotencyClaim claim)
    {
        ArgumentNullException.ThrowIfNull(claim);
        return new CommandIdempotencyAcquireResult<T>(CommandIdempotencyAcquireState.Acquired, claim, false, default, null);
    }

    /// <summary>Creates a running result.</summary>
    public static CommandIdempotencyAcquireResult<T> Running() =>
        new(CommandIdempotencyAcquireState.Running, null, false, default, null);

    /// <summary>Creates a completed result, preserving null/default values.</summary>
    public static CommandIdempotencyAcquireResult<T> Completed(T? outcome) =>
        new(CommandIdempotencyAcquireState.Completed, null, true, outcome, null);

    /// <summary>Creates a completed result whose payload is missing or corrupt.</summary>
    public static CommandIdempotencyAcquireResult<T> Corrupt(Problem problem)
    {
        ArgumentNullException.ThrowIfNull(problem);
        return new CommandIdempotencyAcquireResult<T>(CommandIdempotencyAcquireState.Completed, null, false, default, problem);
    }

    /// <summary>Creates a conflict result.</summary>
    public static CommandIdempotencyAcquireResult<T> Conflict(Problem problem)
    {
        ArgumentNullException.ThrowIfNull(problem);
        return new CommandIdempotencyAcquireResult<T>(CommandIdempotencyAcquireState.Conflict, null, false, default, problem);
    }

    /// <summary>Creates an indeterminate result.</summary>
    public static CommandIdempotencyAcquireResult<T> Indeterminate(Problem problem)
    {
        ArgumentNullException.ThrowIfNull(problem);
        return new CommandIdempotencyAcquireResult<T>(CommandIdempotencyAcquireState.Indeterminate, null, false, default, problem);
    }
}
