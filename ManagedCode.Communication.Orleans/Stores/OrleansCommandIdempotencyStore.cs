using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Constants;
using ManagedCode.Communication.Orleans.Grains;
using Orleans;

namespace ManagedCode.Communication.Orleans.Stores;

/// <summary>Atomic, fenced Orleans-backed command idempotency store.</summary>
public sealed class OrleansCommandIdempotencyStore(IGrainFactory grainFactory) : ICommandIdempotencyStore
{
    /// <inheritdoc />
    public async Task<CommandIdempotencyAcquireResult<T>> TryAcquireAsync<T>(
        CommandIdempotencyDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        descriptor.Validate();
        var result = await GetGrain(descriptor.StorageKey)
            .TryAcquireAtomicAsync(new OrleansIdempotencyAcquireRequest
            {
                Operation = descriptor.Operation,
                Fingerprint = descriptor.Fingerprint,
                ResultContract = descriptor.ResultContract,
                ClaimLease = descriptor.ClaimLease,
                OutcomeRetention = descriptor.OutcomeRetention
            })
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        return result.State switch
        {
            CommandIdempotencyAcquireState.Acquired when result.Claim is not null =>
                CommandIdempotencyAcquireResult<T>.Acquired(ToCoreClaim(descriptor.StorageKey, result.Claim)),
            CommandIdempotencyAcquireState.Running => CommandIdempotencyAcquireResult<T>.Running(),
            CommandIdempotencyAcquireState.Completed when !result.HasOutcome =>
                CommandIdempotencyAcquireResult<T>.Corrupt(result.Problem ?? Problem.Create(
                    ProblemConstants.CommandExecutionTitles.CorruptIdempotencyOutcome,
                    ProblemConstants.CommandExecutionMessages.MissingCachedOutcome,
                    HttpStatusCode.InternalServerError)),
            CommandIdempotencyAcquireState.Completed when result.Outcome is null =>
                CommandIdempotencyAcquireResult<T>.Completed(default),
            CommandIdempotencyAcquireState.Completed when result.Outcome is T outcome =>
                CommandIdempotencyAcquireResult<T>.Completed(outcome),
            CommandIdempotencyAcquireState.Completed => CommandIdempotencyAcquireResult<T>.Corrupt(Problem.Create(
                ProblemConstants.CommandExecutionTitles.CorruptIdempotencyOutcome,
                ProblemConstants.CommandExecutionMessages.CachedOutcomeContractMismatch,
                HttpStatusCode.InternalServerError)),
            CommandIdempotencyAcquireState.Conflict => CommandIdempotencyAcquireResult<T>.Conflict(
                result.Problem ?? Problem.Create(HttpStatusCode.Conflict)),
            CommandIdempotencyAcquireState.Indeterminate => CommandIdempotencyAcquireResult<T>.Indeterminate(
                result.Problem ?? Problem.Create(
                    ProblemConstants.CommandExecutionTitles.IndeterminateCommandOutcome,
                    ProblemConstants.CommandExecutionMessages.PreviousExecutionIndeterminate,
                    HttpStatusCode.Conflict)),
            _ => CommandIdempotencyAcquireResult<T>.Conflict(Problem.Create(
                ProblemConstants.CommandExecutionTitles.InvalidIdempotencyResponse,
                ProblemConstants.CommandExecutionMessages.InvalidOrleansAcquisition,
                HttpStatusCode.InternalServerError))
        };
    }

    /// <inheritdoc />
    public async Task<bool> TryCompleteAsync<T>(
        CommandIdempotencyClaim claim,
        T outcome,
        TimeSpan retention,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(retention, TimeSpan.Zero);
        return await GetGrain(claim.StorageKey)
            .TryCompleteAtomicAsync(ToOrleansClaim(claim), outcome, DateTime.UtcNow.Add(retention))
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> TryRenewAsync(
        CommandIdempotencyClaim claim,
        TimeSpan lease,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(lease, TimeSpan.Zero);
        return await GetGrain(claim.StorageKey)
            .TryRenewAtomicAsync(ToOrleansClaim(claim), DateTime.UtcNow.Add(lease))
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> TryMarkIndeterminateAsync(
        CommandIdempotencyClaim claim,
        Problem problem,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(problem);
        return await GetGrain(claim.StorageKey)
            .TryMarkIndeterminateAtomicAsync(ToOrleansClaim(claim), problem)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> TryReleaseAsync(
        CommandIdempotencyClaim claim,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(claim);
        return await GetGrain(claim.StorageKey)
            .TryReleaseAtomicAsync(ToOrleansClaim(claim))
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> TryResolveIndeterminateAsync<T>(
        CommandIdempotencyDescriptor descriptor,
        T outcome,
        TimeSpan retention,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        descriptor.Validate();
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(retention, TimeSpan.Zero);
        return await GetGrain(descriptor.StorageKey)
            .TryResolveIndeterminateAtomicAsync(ToRequest(descriptor), outcome, DateTime.UtcNow.Add(retention))
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> TryResetIndeterminateAsync(
        CommandIdempotencyDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        descriptor.Validate();
        return await GetGrain(descriptor.StorageKey)
            .TryResetIndeterminateAtomicAsync(ToRequest(descriptor))
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private ICommandIdempotencyGrain GetGrain(string storageKey) =>
        grainFactory.GetGrain<ICommandIdempotencyGrain>(storageKey);

    private static OrleansIdempotencyAcquireRequest ToRequest(CommandIdempotencyDescriptor descriptor) => new()
    {
        Operation = descriptor.Operation,
        Fingerprint = descriptor.Fingerprint,
        ResultContract = descriptor.ResultContract,
        ClaimLease = descriptor.ClaimLease,
        OutcomeRetention = descriptor.OutcomeRetention
    };

    private static CommandIdempotencyClaim ToCoreClaim(string storageKey, OrleansIdempotencyClaim claim) => new(
        storageKey,
        claim.Operation,
        claim.Fingerprint,
        claim.ResultContract,
        claim.OwnerToken,
        claim.Generation);

    private static OrleansIdempotencyClaim ToOrleansClaim(CommandIdempotencyClaim claim) => new()
    {
        Operation = claim.Operation,
        Fingerprint = claim.Fingerprint,
        ResultContract = claim.ResultContract,
        OwnerToken = claim.OwnerToken,
        Generation = claim.Generation
    };
}
