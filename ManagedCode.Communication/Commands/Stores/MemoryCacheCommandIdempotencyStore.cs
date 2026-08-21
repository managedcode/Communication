using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Constants;
using ManagedCode.Communication.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication.Commands.Stores;

/// <summary>Atomic, fenced idempotency storage for one application process.</summary>
public sealed class MemoryCacheCommandIdempotencyStore :
    ICommandIdempotencyStore,
    ICommandIdempotencyMaintenance,
    IDisposable
{
    private const int CommandLockStripeCount = 257;
    private readonly SemaphoreSlim[] _commandLocks;
    private readonly ConcurrentDictionary<string, TimestampIndexEntry> _commandTimestamps = new(StringComparer.Ordinal);
    private readonly ILogger<MemoryCacheCommandIdempotencyStore> _logger;
    private readonly IMemoryCache _memoryCache;
    private int _disposed;

    /// <summary>Creates a single-process idempotency store over <see cref="IMemoryCache" />.</summary>
    public MemoryCacheCommandIdempotencyStore(
        IMemoryCache memoryCache,
        ILogger<MemoryCacheCommandIdempotencyStore> logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _commandLocks = Enumerable.Range(0, CommandLockStripeCount)
            .Select(static _ => new SemaphoreSlim(1, 1))
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<CommandIdempotencyAcquireResult<T>> TryAcquireAsync<T>(
        CommandIdempotencyDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        descriptor.Validate();
        using var scope = await AcquireLockAsync(descriptor.StorageKey, cancellationToken).ConfigureAwait(false);
        var now = DateTime.UtcNow;
        var state = _memoryCache.Get<AtomicCommandState>(GetAtomicKey(descriptor.StorageKey));

        if (state is { Status: AtomicCommandStatus.Running } && state.ExpiresAtUtc <= now)
        {
            state.Status = AtomicCommandStatus.Indeterminate;
            state.HasOutcome = false;
            state.Outcome = null;
            state.Problem = CreateExpiredClaimProblem();
            state.UpdatedAtUtc = now;
            state.ExpiresAtUtc = DateTime.MaxValue;
            SetAtomicState(descriptor.StorageKey, state);
        }
        else if (state is not null && state.ExpiresAtUtc <= now)
        {
            RemoveAtomicState(descriptor.StorageKey);
            state = null;
        }

        if (state is null)
        {
            var claim = new CommandIdempotencyClaim(
                descriptor.StorageKey,
                descriptor.Operation,
                descriptor.Fingerprint,
                descriptor.ResultContract,
                Guid.CreateVersion7(),
                1);
            SetAtomicState(descriptor.StorageKey, AtomicCommandState.Running(claim, now.Add(descriptor.ClaimLease)));
            return CommandIdempotencyAcquireResult<T>.Acquired(claim);
        }

        if (!state.Matches(descriptor.Operation, descriptor.Fingerprint, descriptor.ResultContract))
        {
            return CommandIdempotencyAcquireResult<T>.Conflict(Problem.Create(
                ProblemConstants.CommandExecutionTitles.IdempotencyKeyConflict,
                ProblemConstants.CommandExecutionMessages.IdempotencyKeyConflict,
                HttpStatusCode.Conflict));
        }

        return state.Status switch
        {
            AtomicCommandStatus.Running => CommandIdempotencyAcquireResult<T>.Running(),
            AtomicCommandStatus.Indeterminate => CommandIdempotencyAcquireResult<T>.Indeterminate(
                state.Problem ?? CreateExpiredClaimProblem()),
            AtomicCommandStatus.Completed when !state.HasOutcome => CommandIdempotencyAcquireResult<T>.Corrupt(
                Problem.Create(
                    ProblemConstants.CommandExecutionTitles.CorruptIdempotencyOutcome,
                    ProblemConstants.CommandExecutionMessages.MissingCachedOutcome,
                    HttpStatusCode.InternalServerError)),
            AtomicCommandStatus.Completed when state.Outcome is null => CommandIdempotencyAcquireResult<T>.Completed(default),
            AtomicCommandStatus.Completed when state.Outcome is T outcome => CommandIdempotencyAcquireResult<T>.Completed(outcome),
            _ => CommandIdempotencyAcquireResult<T>.Corrupt(Problem.Create(
                ProblemConstants.CommandExecutionTitles.CorruptIdempotencyOutcome,
                ProblemConstants.CommandExecutionMessages.CachedOutcomeContractMismatch,
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
        using var scope = await AcquireLockAsync(claim.StorageKey, cancellationToken).ConfigureAwait(false);
        var state = _memoryCache.Get<AtomicCommandState>(GetAtomicKey(claim.StorageKey));
        if (!OwnsClaim(state, claim))
        {
            return false;
        }

        var now = DateTime.UtcNow;
        state!.Status = AtomicCommandStatus.Completed;
        state.HasOutcome = true;
        state.Outcome = outcome;
        state.Problem = null;
        state.UpdatedAtUtc = now;
        state.ExpiresAtUtc = now.Add(retention);
        SetAtomicState(claim.StorageKey, state);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> TryRenewAsync(
        CommandIdempotencyClaim claim,
        TimeSpan lease,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(lease, TimeSpan.Zero);
        using var scope = await AcquireLockAsync(claim.StorageKey, cancellationToken).ConfigureAwait(false);
        var state = _memoryCache.Get<AtomicCommandState>(GetAtomicKey(claim.StorageKey));
        if (!OwnsClaim(state, claim))
        {
            return false;
        }

        var now = DateTime.UtcNow;
        state!.UpdatedAtUtc = now;
        state.ExpiresAtUtc = now.Add(lease);
        SetAtomicState(claim.StorageKey, state);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> TryMarkIndeterminateAsync(
        CommandIdempotencyClaim claim,
        Problem problem,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(problem);
        using var scope = await AcquireLockAsync(claim.StorageKey, cancellationToken).ConfigureAwait(false);
        var state = _memoryCache.Get<AtomicCommandState>(GetAtomicKey(claim.StorageKey));
        if (!OwnsClaim(state, claim))
        {
            return false;
        }

        var now = DateTime.UtcNow;
        state!.Status = AtomicCommandStatus.Indeterminate;
        state.HasOutcome = false;
        state.Outcome = null;
        state.Problem = problem;
        state.UpdatedAtUtc = now;
        state.ExpiresAtUtc = DateTime.MaxValue;
        SetAtomicState(claim.StorageKey, state);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> TryReleaseAsync(
        CommandIdempotencyClaim claim,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(claim);
        using var scope = await AcquireLockAsync(claim.StorageKey, cancellationToken).ConfigureAwait(false);
        var state = _memoryCache.Get<AtomicCommandState>(GetAtomicKey(claim.StorageKey));
        if (!OwnsClaim(state, claim))
        {
            return false;
        }

        RemoveAtomicState(claim.StorageKey);
        return true;
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
        using var scope = await AcquireLockAsync(descriptor.StorageKey, cancellationToken).ConfigureAwait(false);
        var state = _memoryCache.Get<AtomicCommandState>(GetAtomicKey(descriptor.StorageKey));
        if (state is not { Status: AtomicCommandStatus.Indeterminate }
            || !state.Matches(descriptor.Operation, descriptor.Fingerprint, descriptor.ResultContract))
        {
            return false;
        }

        var now = DateTime.UtcNow;
        state.Status = AtomicCommandStatus.Completed;
        state.HasOutcome = true;
        state.Outcome = outcome;
        state.Problem = null;
        state.UpdatedAtUtc = now;
        state.ExpiresAtUtc = now.Add(retention);
        SetAtomicState(descriptor.StorageKey, state);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> TryResetIndeterminateAsync(
        CommandIdempotencyDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        descriptor.Validate();
        using var scope = await AcquireLockAsync(descriptor.StorageKey, cancellationToken).ConfigureAwait(false);
        var state = _memoryCache.Get<AtomicCommandState>(GetAtomicKey(descriptor.StorageKey));
        if (state is not { Status: AtomicCommandStatus.Indeterminate }
            || !state.Matches(descriptor.Operation, descriptor.Fingerprint, descriptor.ResultContract))
        {
            return false;
        }

        RemoveAtomicState(descriptor.StorageKey);
        return true;
    }

    /// <inheritdoc />
    public async Task<int> CleanupCompletedCommandsAsync(
        TimeSpan maxAge,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAge, TimeSpan.Zero);
        var cutoff = DateTime.UtcNow.Subtract(maxAge);
        var candidates = _commandTimestamps
            .Where(pair => pair.Value.UpdatedAtUtc < cutoff)
            .Select(pair => pair.Key)
            .ToList();
        var cleaned = 0;
        foreach (var key in candidates)
        {
            using var scope = await AcquireLockAsync(key, cancellationToken).ConfigureAwait(false);
            if (!_commandTimestamps.TryGetValue(key, out var indexEntry)
                || indexEntry.UpdatedAtUtc >= cutoff)
            {
                continue;
            }

            var state = _memoryCache.Get<AtomicCommandState>(GetAtomicKey(key));
            if (state is { Status: AtomicCommandStatus.Completed })
            {
                RemoveAtomicState(key);
                cleaned++;
            }
        }

        if (cleaned > 0)
        {
            LoggerCenter.LogCommandCleanupByStatus(_logger, cleaned, CommandExecutionStatus.Completed, maxAge);
        }

        return cleaned;
    }

    /// <inheritdoc />
    public Task<Dictionary<CommandExecutionStatus, int>> GetCommandCountByStatusAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        var counts = new Dictionary<CommandExecutionStatus, int>();
        foreach (var key in _commandTimestamps.Keys)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_memoryCache.Get<AtomicCommandState>(GetAtomicKey(key)) is { } state)
            {
                var status = ToExecutionStatus(state.Status);
                counts[status] = counts.GetValueOrDefault(status) + 1;
            }
        }

        return Task.FromResult(counts);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _commandTimestamps.Clear();
        // Active holders may still unwind during host shutdown. Disposing the bounded stripe semaphores here would
        // turn that normal path into ObjectDisposedException and could split one key across two live locks.
    }

    private async Task<LockScope> AcquireLockAsync(string key, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        var commandLock = GetCommandLock(key);
        await commandLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        if (Volatile.Read(ref _disposed) != 0)
        {
            commandLock.Release();
            throw new ObjectDisposedException(nameof(MemoryCacheCommandIdempotencyStore));
        }

        return new LockScope(commandLock);
    }

    private SemaphoreSlim GetCommandLock(string key)
    {
        var hash = StringComparer.Ordinal.GetHashCode(key) & int.MaxValue;
        return _commandLocks[hash % _commandLocks.Length];
    }

    private void SetAtomicState(string key, AtomicCommandState state)
    {
        var options = new MemoryCacheEntryOptions();
        if (state.Status == AtomicCommandStatus.Completed)
        {
            var remaining = state.ExpiresAtUtc - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                remaining = TimeSpan.FromMilliseconds(1);
            }

            options.AbsoluteExpirationRelativeToNow = remaining;
        }

        var indexEntry = new TimestampIndexEntry(_commandTimestamps, key, state.UpdatedAtUtc);
        _commandTimestamps[key] = indexEntry;
        options.RegisterPostEvictionCallback(static (_, _, _, callbackState) =>
        {
            if (callbackState is TimestampIndexEntry entry)
            {
                ((ICollection<KeyValuePair<string, TimestampIndexEntry>>)entry.Index).Remove(
                    new KeyValuePair<string, TimestampIndexEntry>(entry.Key, entry));
            }
        }, indexEntry);
        _memoryCache.Set(GetAtomicKey(key), state, options);
    }

    private void RemoveAtomicState(string key)
    {
        _memoryCache.Remove(GetAtomicKey(key));
        _commandTimestamps.TryRemove(key, out _);
    }

    private static bool OwnsClaim(AtomicCommandState? state, CommandIdempotencyClaim claim) =>
        state is { Status: AtomicCommandStatus.Running }
        && state.ExpiresAtUtc > DateTime.UtcNow
        && state.OwnerToken == claim.OwnerToken
        && state.Generation == claim.Generation
        && state.Matches(claim.Operation, claim.Fingerprint, claim.ResultContract);

    private static Problem CreateExpiredClaimProblem() => Problem.Create(
        ProblemConstants.CommandExecutionTitles.IndeterminateCommandOutcome,
        ProblemConstants.CommandExecutionMessages.PreviousExecutionIndeterminate,
        HttpStatusCode.Conflict);

    private static CommandExecutionStatus ToExecutionStatus(AtomicCommandStatus status) => status switch
    {
        AtomicCommandStatus.Running => CommandExecutionStatus.InProgress,
        AtomicCommandStatus.Completed => CommandExecutionStatus.Completed,
        AtomicCommandStatus.Indeterminate => CommandExecutionStatus.Indeterminate,
        _ => CommandExecutionStatus.NotFound
    };

    private const string AtomicKeyPrefix = "cmd_atomic_";

    private static string GetAtomicKey(string key) => AtomicKeyPrefix + key;

    private enum AtomicCommandStatus
    {
        Running,
        Completed,
        Indeterminate
    }

    private sealed class AtomicCommandState
    {
        public required string Operation { get; init; }
        public required string Fingerprint { get; init; }
        public required string ResultContract { get; init; }
        public required Guid OwnerToken { get; init; }
        public required long Generation { get; init; }
        public required AtomicCommandStatus Status { get; set; }
        public bool HasOutcome { get; set; }
        public object? Outcome { get; set; }
        public Problem? Problem { get; set; }
        public required DateTime UpdatedAtUtc { get; set; }
        public required DateTime ExpiresAtUtc { get; set; }

        public bool Matches(string operation, string fingerprint, string resultContract) =>
            string.Equals(Operation, operation, StringComparison.Ordinal)
            && string.Equals(Fingerprint, fingerprint, StringComparison.Ordinal)
            && string.Equals(ResultContract, resultContract, StringComparison.Ordinal);

        public static AtomicCommandState Running(CommandIdempotencyClaim claim, DateTime expiresAtUtc) => new()
        {
            Operation = claim.Operation,
            Fingerprint = claim.Fingerprint,
            ResultContract = claim.ResultContract,
            OwnerToken = claim.OwnerToken,
            Generation = claim.Generation,
            Status = AtomicCommandStatus.Running,
            UpdatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    private sealed class TimestampIndexEntry(
        ConcurrentDictionary<string, TimestampIndexEntry> index,
        string key,
        DateTime updatedAtUtc)
    {
        public ConcurrentDictionary<string, TimestampIndexEntry> Index { get; } = index;
        public string Key { get; } = key;
        public DateTime UpdatedAtUtc { get; } = updatedAtUtc;
    }

    private sealed class LockScope(SemaphoreSlim commandLock) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                commandLock.Release();
            }
        }
    }
}
