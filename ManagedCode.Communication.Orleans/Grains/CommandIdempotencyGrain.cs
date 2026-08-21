using System;
using System.Net;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Constants;
using Orleans;
using Orleans.Runtime;

namespace ManagedCode.Communication.Orleans.Grains;

/// <summary>One atomic, fenced idempotency record per Orleans grain key.</summary>
public sealed class CommandIdempotencyGrain(
    [PersistentState(
        OrleansCommandExecutionDefaults.IdempotencyStateName,
        OrleansCommandExecutionDefaults.IdempotencyStorageName)]
    IPersistentState<CommandState> state)
    : Grain, ICommandIdempotencyGrain
{
    /// <inheritdoc />
    public async Task<OrleansIdempotencyAcquireResult> TryAcquireAtomicAsync(
        OrleansIdempotencyAcquireRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Fingerprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ResultContract);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(request.ClaimLease, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(request.OutcomeRetention, TimeSpan.Zero);
        var now = DateTime.UtcNow;

        if (IsExpired(now) && state.State.Status is CommandExecutionStatus.InProgress or CommandExecutionStatus.Processing)
        {
            state.State.Status = CommandExecutionStatus.Indeterminate;
            state.State.Result = null;
            state.State.HasResult = false;
            state.State.Problem = CreateExpiredClaimProblem();
            state.State.FailedAt = now;
            state.State.ExpiresAt = null;
            await state.WriteStateAsync();
        }
        else if (IsExpired(now))
        {
            ResetState(preserveGeneration: true);
            await state.ClearStateAsync();
        }

        if (state.State.Status is CommandExecutionStatus.NotFound or CommandExecutionStatus.NotStarted)
        {
            var generation = state.State.Generation == long.MaxValue
                ? long.MaxValue
                : state.State.Generation + 1;
            var claim = new OrleansIdempotencyClaim
            {
                Operation = request.Operation,
                Fingerprint = request.Fingerprint,
                ResultContract = request.ResultContract,
                OwnerToken = Guid.CreateVersion7(),
                Generation = generation
            };
            state.State.Status = CommandExecutionStatus.Processing;
            state.State.Operation = request.Operation;
            state.State.Fingerprint = request.Fingerprint;
            state.State.ResultContract = request.ResultContract;
            state.State.OwnerToken = claim.OwnerToken;
            state.State.Generation = generation;
            state.State.Result = null;
            state.State.HasResult = false;
            state.State.Problem = null;
            state.State.StartedAt = now;
            state.State.CompletedAt = null;
            state.State.FailedAt = null;
            state.State.ExpiresAt = now.Add(request.ClaimLease);
            await state.WriteStateAsync();
            return new OrleansIdempotencyAcquireResult
            {
                State = CommandIdempotencyAcquireState.Acquired,
                Claim = claim
            };
        }

        if (!Matches(request.Operation, request.Fingerprint, request.ResultContract))
        {
            return new OrleansIdempotencyAcquireResult
            {
                State = CommandIdempotencyAcquireState.Conflict,
                Problem = Problem.Create(
                    ProblemConstants.CommandExecutionTitles.IdempotencyKeyConflict,
                    ProblemConstants.CommandExecutionMessages.IdempotencyKeyConflict,
                    HttpStatusCode.Conflict)
            };
        }

        return state.State.Status switch
        {
            CommandExecutionStatus.InProgress or CommandExecutionStatus.Processing => new OrleansIdempotencyAcquireResult
            {
                State = CommandIdempotencyAcquireState.Running
            },
            CommandExecutionStatus.Completed => new OrleansIdempotencyAcquireResult
            {
                State = CommandIdempotencyAcquireState.Completed,
                HasOutcome = state.State.HasResult,
                Outcome = state.State.Result,
                Problem = state.State.HasResult
                    ? null
                    : Problem.Create(
                        ProblemConstants.CommandExecutionTitles.CorruptIdempotencyOutcome,
                        ProblemConstants.CommandExecutionMessages.MissingCachedOutcome,
                        HttpStatusCode.InternalServerError)
            },
            CommandExecutionStatus.Indeterminate => new OrleansIdempotencyAcquireResult
            {
                State = CommandIdempotencyAcquireState.Indeterminate,
                Problem = state.State.Problem ?? CreateExpiredClaimProblem()
            },
            _ => new OrleansIdempotencyAcquireResult
            {
                State = CommandIdempotencyAcquireState.Conflict,
                Problem = Problem.Create(
                    ProblemConstants.CommandExecutionTitles.InvalidIdempotencyState,
                    string.Format(
                        ProblemConstants.CommandExecutionMessages.UnsupportedIdempotencyStateFormat,
                        state.State.Status),
                    HttpStatusCode.InternalServerError)
            }
        };
    }

    /// <inheritdoc />
    public async Task<bool> TryCompleteAtomicAsync(
        OrleansIdempotencyClaim claim,
        object? outcome,
        DateTime expiresAtUtc)
    {
        ArgumentNullException.ThrowIfNull(claim);
        if (!OwnsAtomicClaim(claim) || expiresAtUtc <= DateTime.UtcNow)
        {
            return false;
        }

        state.State.Status = CommandExecutionStatus.Completed;
        state.State.Result = outcome;
        state.State.HasResult = true;
        state.State.Problem = null;
        state.State.CompletedAt = DateTime.UtcNow;
        state.State.ExpiresAt = expiresAtUtc;
        await state.WriteStateAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> TryRenewAtomicAsync(OrleansIdempotencyClaim claim, DateTime expiresAtUtc)
    {
        ArgumentNullException.ThrowIfNull(claim);
        if (!OwnsAtomicClaim(claim) || expiresAtUtc <= DateTime.UtcNow)
        {
            return false;
        }

        state.State.ExpiresAt = expiresAtUtc;
        await state.WriteStateAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> TryMarkIndeterminateAtomicAsync(
        OrleansIdempotencyClaim claim,
        Problem problem)
    {
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(problem);
        if (!OwnsAtomicClaim(claim))
        {
            return false;
        }

        state.State.Status = CommandExecutionStatus.Indeterminate;
        state.State.Result = null;
        state.State.HasResult = false;
        state.State.Problem = problem;
        state.State.FailedAt = DateTime.UtcNow;
        // Indeterminate records do not expire automatically: an operator must explicitly resolve the ambiguity.
        state.State.ExpiresAt = null;
        await state.WriteStateAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> TryReleaseAtomicAsync(OrleansIdempotencyClaim claim)
    {
        ArgumentNullException.ThrowIfNull(claim);
        if (!OwnsAtomicClaim(claim))
        {
            return false;
        }

        ResetState(preserveGeneration: true);
        await state.ClearStateAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> TryResolveIndeterminateAtomicAsync(
        OrleansIdempotencyAcquireRequest request,
        object? outcome,
        DateTime expiresAtUtc)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (state.State.Status != CommandExecutionStatus.Indeterminate
            || !Matches(request.Operation, request.Fingerprint, request.ResultContract)
            || expiresAtUtc <= DateTime.UtcNow)
        {
            return false;
        }

        state.State.Status = CommandExecutionStatus.Completed;
        state.State.Result = outcome;
        state.State.HasResult = true;
        state.State.Problem = null;
        state.State.CompletedAt = DateTime.UtcNow;
        state.State.ExpiresAt = expiresAtUtc;
        await state.WriteStateAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> TryResetIndeterminateAtomicAsync(OrleansIdempotencyAcquireRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (state.State.Status != CommandExecutionStatus.Indeterminate
            || !Matches(request.Operation, request.Fingerprint, request.ResultContract))
        {
            return false;
        }

        ResetState(preserveGeneration: true);
        await state.ClearStateAsync();
        return true;
    }

    private bool OwnsAtomicClaim(OrleansIdempotencyClaim claim) =>
        state.State.Status is CommandExecutionStatus.InProgress or CommandExecutionStatus.Processing
        && !IsExpired(DateTime.UtcNow)
        && state.State.OwnerToken == claim.OwnerToken
        && state.State.Generation == claim.Generation
        && Matches(claim.Operation, claim.Fingerprint, claim.ResultContract);

    private bool Matches(string operation, string fingerprint, string resultContract) =>
        string.Equals(state.State.Operation, operation, StringComparison.Ordinal)
        && string.Equals(state.State.Fingerprint, fingerprint, StringComparison.Ordinal)
        && string.Equals(state.State.ResultContract, resultContract, StringComparison.Ordinal);

    private bool IsExpired(DateTime nowUtc) => state.State.ExpiresAt is { } expiresAt && nowUtc >= expiresAt;

    private static Problem CreateExpiredClaimProblem() => Problem.Create(
        ProblemConstants.CommandExecutionTitles.IndeterminateCommandOutcome,
        ProblemConstants.CommandExecutionMessages.PreviousExecutionIndeterminate,
        HttpStatusCode.Conflict);

    private void ResetState(bool preserveGeneration)
    {
        var generation = preserveGeneration ? state.State.Generation : 0;
        state.State.Status = CommandExecutionStatus.NotFound;
        state.State.Result = null;
        state.State.HasResult = false;
        state.State.Problem = null;
        state.State.Operation = null;
        state.State.Fingerprint = null;
        state.State.ResultContract = null;
        state.State.OwnerToken = Guid.Empty;
        state.State.Generation = generation;
        state.State.StartedAt = null;
        state.State.CompletedAt = null;
        state.State.FailedAt = null;
        state.State.ExpiresAt = null;
    }
}

/// <summary>Persisted atomic idempotency state.</summary>
[GenerateSerializer]
public sealed class CommandState
{
    /// <summary>Coordination state.</summary>
    [Id(0)] public CommandExecutionStatus Status { get; set; } = CommandExecutionStatus.NotFound;
    /// <summary>Terminal payload.</summary>
    [Id(1)] public object? Result { get; set; }
    /// <summary>Reserved legacy error detail.</summary>
    [Id(2)] public string? ErrorMessage { get; set; }
    /// <summary>Claim start time in UTC.</summary>
    [Id(3)] public DateTime? StartedAt { get; set; }
    /// <summary>Completion time in UTC.</summary>
    [Id(4)] public DateTime? CompletedAt { get; set; }
    /// <summary>Indeterminate transition time in UTC.</summary>
    [Id(5)] public DateTime? FailedAt { get; set; }
    /// <summary>Lease or terminal retention expiry in UTC; null for unresolved ambiguity.</summary>
    [Id(6)] public DateTime? ExpiresAt { get; set; }
    /// <summary>Bound logical operation.</summary>
    [Id(7)] public string? Operation { get; set; }
    /// <summary>Bound request fingerprint.</summary>
    [Id(8)] public string? Fingerprint { get; set; }
    /// <summary>Bound result contract.</summary>
    [Id(9)] public string? ResultContract { get; set; }
    /// <summary>Current fenced owner token.</summary>
    [Id(10)] public Guid OwnerToken { get; set; }
    /// <summary>Claim generation.</summary>
    [Id(11)] public long Generation { get; set; }
    /// <summary>Whether the terminal payload is present, including null/default.</summary>
    [Id(12)] public bool HasResult { get; set; }
    /// <summary>Indeterminate-state details.</summary>
    [Id(13)] public Problem? Problem { get; set; }
}
