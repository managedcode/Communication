using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Communication.Commands.Execution;

/// <summary>
///     Acquires permits for command attempts from a local or distributed rate limiter.
/// </summary>
public interface ICommandRateLimiter
{
    /// <summary>Acquires a permit for one command attempt.</summary>
    ValueTask<CommandRateLimitLease> AcquireAsync(ICommand command, CancellationToken cancellationToken = default);
}

/// <summary>
///     Represents a command rate-limit permit. Successful leases must be disposed after the attempt.
/// </summary>
public sealed class CommandRateLimitLease : IAsyncDisposable
{
    private readonly Func<ValueTask>? _disposeAsync;
    private int _disposed;

    private CommandRateLimitLease(
        bool isAcquired,
        bool wasQueued,
        Problem? problem,
        IReadOnlyDictionary<string, object?> metadata,
        Func<ValueTask>? disposeAsync)
    {
        IsAcquired = isAcquired;
        WasQueued = wasQueued;
        Problem = problem;
        Metadata = metadata;
        _disposeAsync = disposeAsync;
    }

    /// <summary>Whether the permit was acquired.</summary>
    public bool IsAcquired { get; }

    /// <summary>Whether acquisition waited in a limiter queue.</summary>
    public bool WasQueued { get; }

    /// <summary>Failure returned by a rejected lease.</summary>
    public Problem? Problem { get; }

    /// <summary>Limiter-specific metadata such as retry-after.</summary>
    public IReadOnlyDictionary<string, object?> Metadata { get; }

    /// <summary>Creates an acquired lease.</summary>
    public static CommandRateLimitLease Acquired(
        bool wasQueued = false,
        IReadOnlyDictionary<string, object?>? metadata = null,
        Func<ValueTask>? disposeAsync = null)
    {
        return new CommandRateLimitLease(
            true,
            wasQueued,
            null,
            metadata ?? EmptyMetadata,
            disposeAsync);
    }

    /// <summary>Creates a rejected lease.</summary>
    public static CommandRateLimitLease Rejected(
        Problem problem,
        bool wasQueued = false,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(problem);
        return new CommandRateLimitLease(
            false,
            wasQueued,
            problem,
            metadata ?? EmptyMetadata,
            null);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return ValueTask.CompletedTask;
        }

        return _disposeAsync?.Invoke() ?? ValueTask.CompletedTask;
    }

    private static IReadOnlyDictionary<string, object?> EmptyMetadata { get; } =
        FrozenDictionary<string, object?>.Empty;
}
