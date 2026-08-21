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
    /// <summary>Atomically reads or claims an idempotent operation.</summary>
    Task<OrleansIdempotencyAcquireResult> TryAcquireAtomicAsync(OrleansIdempotencyAcquireRequest request);

    /// <summary>Atomically completes an operation when the fenced owner still holds it.</summary>
    Task<bool> TryCompleteAtomicAsync(OrleansIdempotencyClaim claim, object? outcome, DateTime expiresAtUtc);

    /// <summary>Renews an active fenced claim.</summary>
    Task<bool> TryRenewAtomicAsync(OrleansIdempotencyClaim claim, DateTime expiresAtUtc);

    /// <summary>Marks an active fenced claim as indeterminate.</summary>
    Task<bool> TryMarkIndeterminateAtomicAsync(
        OrleansIdempotencyClaim claim,
        Problem problem);

    /// <summary>Releases an active fenced claim that never invoked business work.</summary>
    Task<bool> TryReleaseAtomicAsync(OrleansIdempotencyClaim claim);

    /// <summary>Resolves matching indeterminate state with a known terminal outcome.</summary>
    Task<bool> TryResolveIndeterminateAtomicAsync(
        OrleansIdempotencyAcquireRequest request,
        object? outcome,
        DateTime expiresAtUtc);

    /// <summary>Deletes matching indeterminate state after retry has been established safe.</summary>
    Task<bool> TryResetIndeterminateAtomicAsync(OrleansIdempotencyAcquireRequest request);

}

/// <summary>Orleans wire request for an atomic idempotency acquisition.</summary>
[GenerateSerializer]
public sealed class OrleansIdempotencyAcquireRequest
{
    /// <summary>Logical operation bound to the key.</summary>
    [Id(0)]
    public string Operation { get; set; } = string.Empty;

    /// <summary>Request fingerprint bound to the key.</summary>
    [Id(1)]
    public string Fingerprint { get; set; } = string.Empty;

    /// <summary>Expected result contract.</summary>
    [Id(2)]
    public string ResultContract { get; set; } = string.Empty;

    /// <summary>How long the owner claim remains valid without renewal.</summary>
    [Id(3)]
    public TimeSpan ClaimLease { get; set; }

    /// <summary>How long a completed replay outcome remains available.</summary>
    [Id(4)]
    public TimeSpan OutcomeRetention { get; set; }
}

/// <summary>Orleans wire representation of a fenced idempotency claim.</summary>
[GenerateSerializer]
public sealed class OrleansIdempotencyClaim
{
    /// <summary>Logical operation bound to the key.</summary>
    [Id(0)]
    public string Operation { get; set; } = string.Empty;

    /// <summary>Request fingerprint bound to the key.</summary>
    [Id(1)]
    public string Fingerprint { get; set; } = string.Empty;

    /// <summary>Expected result contract.</summary>
    [Id(2)]
    public string ResultContract { get; set; } = string.Empty;

    /// <summary>Unpredictable fenced owner token.</summary>
    [Id(3)]
    public Guid OwnerToken { get; set; }

    /// <summary>Monotonic claim generation.</summary>
    [Id(4)]
    public long Generation { get; set; }
}

/// <summary>Orleans wire response for an atomic idempotency acquisition.</summary>
[GenerateSerializer]
public sealed class OrleansIdempotencyAcquireResult
{
    /// <summary>Current coordination state.</summary>
    [Id(0)]
    public CommandIdempotencyAcquireState State { get; set; }

    /// <summary>Fenced claim when ownership was acquired.</summary>
    [Id(1)]
    public OrleansIdempotencyClaim? Claim { get; set; }

    /// <summary>Whether a terminal payload is present, including null/default.</summary>
    [Id(2)]
    public bool HasOutcome { get; set; }

    /// <summary>Cached terminal outcome.</summary>
    [Id(3)]
    public object? Outcome { get; set; }

    /// <summary>Conflict, corruption, or indeterminate-state details.</summary>
    [Id(4)]
    public Problem? Problem { get; set; }
}
