using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace ManagedCode.Communication.Commands.Execution;

/// <summary>
///     Adapts .NET's partitioned rate limiter to command execution.
/// </summary>
public sealed class PartitionedCommandRateLimiter : ICommandRateLimiter, IAsyncDisposable
{
    private readonly PartitionedRateLimiter<ICommand> _rateLimiter;

    /// <summary>Creates an adapter over an application-owned partitioned limiter.</summary>
    public PartitionedCommandRateLimiter(PartitionedRateLimiter<ICommand> rateLimiter)
    {
        ArgumentNullException.ThrowIfNull(rateLimiter);
        _rateLimiter = rateLimiter;
    }

    /// <summary>
    ///     Creates a partitioned fixed-window limiter. The partition key can use command type, user, tenant metadata,
    ///     or any application-specific command property.
    /// </summary>
    public static PartitionedCommandRateLimiter CreateFixedWindow(
        Func<ICommand, string> partitionKeySelector,
        int permitLimit,
        TimeSpan window,
        int queueLimit = 0,
        QueueProcessingOrder queueProcessingOrder = QueueProcessingOrder.OldestFirst,
        bool autoReplenishment = true)
    {
        ArgumentNullException.ThrowIfNull(partitionKeySelector);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(permitLimit);
        if (window <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(window));
        }
        ArgumentOutOfRangeException.ThrowIfNegative(queueLimit);

        var limiter = PartitionedRateLimiter.Create<ICommand, string>(command =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKeySelector(command),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    QueueLimit = queueLimit,
                    QueueProcessingOrder = queueProcessingOrder,
                    AutoReplenishment = autoReplenishment
                }));

        return new PartitionedCommandRateLimiter(limiter);
    }

    /// <inheritdoc />
    public async ValueTask<CommandRateLimitLease> AcquireAsync(
        ICommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var pendingLease = _rateLimiter.AcquireAsync(command, cancellationToken: cancellationToken);
        var wasQueued = !pendingLease.IsCompletedSuccessfully;
        var lease = await pendingLease.ConfigureAwait(false);
        var metadata = ReadMetadata(lease);

        if (!lease.IsAcquired)
        {
            lease.Dispose();
            var problem = Problem.Create(
                "Command rate limit exceeded",
                "The command could not acquire a rate-limit permit.",
                HttpStatusCode.TooManyRequests);

            foreach (var pair in metadata)
            {
                problem.Extensions[pair.Key] = pair.Value;
            }

            return CommandRateLimitLease.Rejected(problem, wasQueued, metadata);
        }

        return CommandRateLimitLease.Acquired(
            wasQueued,
            metadata,
            () =>
            {
                lease.Dispose();
                return ValueTask.CompletedTask;
            });
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return _rateLimiter.DisposeAsync();
    }

    private static Dictionary<string, object?> ReadMetadata(RateLimitLease lease)
    {
        var metadata = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var name in lease.MetadataNames)
        {
            if (lease.TryGetMetadata(name, out var value))
            {
                metadata[name] = value;
            }
        }

        if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            metadata["retryAfter"] = retryAfter;
        }

        return metadata;
    }
}
