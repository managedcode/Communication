using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Communication.Constants;

namespace ManagedCode.Communication.Commands.Execution;

/// <summary>
///     Adapts .NET's partitioned rate limiter to command execution.
/// </summary>
public sealed class PartitionedCommandRateLimiter : ICommandRateLimiter, IAsyncDisposable
{
    private readonly PartitionedRateLimiter<ICommand> _rateLimiter;
    private readonly Func<ICommand, int> _permitCountSelector;
    private readonly bool _ownsRateLimiter;

    /// <summary>Creates an adapter over an application-owned partitioned limiter.</summary>
    public PartitionedCommandRateLimiter(
        PartitionedRateLimiter<ICommand> rateLimiter,
        Func<ICommand, int>? permitCountSelector = null,
        bool ownsRateLimiter = false)
    {
        ArgumentNullException.ThrowIfNull(rateLimiter);
        _rateLimiter = rateLimiter;
        _permitCountSelector = permitCountSelector ?? (static _ => 1);
        _ownsRateLimiter = ownsRateLimiter;
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
        bool autoReplenishment = true,
        Func<ICommand, int>? permitCountSelector = null)
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

        return new PartitionedCommandRateLimiter(limiter, permitCountSelector, ownsRateLimiter: true);
    }

    /// <summary>Creates a partitioned concurrency limiter.</summary>
    public static PartitionedCommandRateLimiter CreateConcurrency(
        Func<ICommand, string> partitionKeySelector,
        int permitLimit,
        int queueLimit = 0,
        QueueProcessingOrder queueProcessingOrder = QueueProcessingOrder.OldestFirst,
        Func<ICommand, int>? permitCountSelector = null)
    {
        ArgumentNullException.ThrowIfNull(partitionKeySelector);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(permitLimit);
        ArgumentOutOfRangeException.ThrowIfNegative(queueLimit);
        var limiter = PartitionedRateLimiter.Create<ICommand, string>(command =>
            RateLimitPartition.GetConcurrencyLimiter(
                partitionKeySelector(command),
                _ => new ConcurrencyLimiterOptions
                {
                    PermitLimit = permitLimit,
                    QueueLimit = queueLimit,
                    QueueProcessingOrder = queueProcessingOrder
                }));
        return new PartitionedCommandRateLimiter(limiter, permitCountSelector, ownsRateLimiter: true);
    }

    /// <summary>Creates a partitioned sliding-window limiter.</summary>
    public static PartitionedCommandRateLimiter CreateSlidingWindow(
        Func<ICommand, string> partitionKeySelector,
        int permitLimit,
        TimeSpan window,
        int segmentsPerWindow,
        int queueLimit = 0,
        QueueProcessingOrder queueProcessingOrder = QueueProcessingOrder.OldestFirst,
        bool autoReplenishment = true,
        Func<ICommand, int>? permitCountSelector = null)
    {
        ArgumentNullException.ThrowIfNull(partitionKeySelector);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(permitLimit);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(window, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(segmentsPerWindow);
        ArgumentOutOfRangeException.ThrowIfNegative(queueLimit);
        var limiter = PartitionedRateLimiter.Create<ICommand, string>(command =>
            RateLimitPartition.GetSlidingWindowLimiter(
                partitionKeySelector(command),
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    SegmentsPerWindow = segmentsPerWindow,
                    QueueLimit = queueLimit,
                    QueueProcessingOrder = queueProcessingOrder,
                    AutoReplenishment = autoReplenishment
                }));
        return new PartitionedCommandRateLimiter(limiter, permitCountSelector, ownsRateLimiter: true);
    }

    /// <summary>Creates a partitioned token-bucket limiter.</summary>
    public static PartitionedCommandRateLimiter CreateTokenBucket(
        Func<ICommand, string> partitionKeySelector,
        int tokenLimit,
        int tokensPerPeriod,
        TimeSpan replenishmentPeriod,
        int queueLimit = 0,
        QueueProcessingOrder queueProcessingOrder = QueueProcessingOrder.OldestFirst,
        bool autoReplenishment = true,
        Func<ICommand, int>? permitCountSelector = null)
    {
        ArgumentNullException.ThrowIfNull(partitionKeySelector);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tokenLimit);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tokensPerPeriod);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(replenishmentPeriod, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfNegative(queueLimit);
        var limiter = PartitionedRateLimiter.Create<ICommand, string>(command =>
            RateLimitPartition.GetTokenBucketLimiter(
                partitionKeySelector(command),
                _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = tokenLimit,
                    TokensPerPeriod = tokensPerPeriod,
                    ReplenishmentPeriod = replenishmentPeriod,
                    QueueLimit = queueLimit,
                    QueueProcessingOrder = queueProcessingOrder,
                    AutoReplenishment = autoReplenishment
                }));
        return new PartitionedCommandRateLimiter(limiter, permitCountSelector, ownsRateLimiter: true);
    }

    /// <inheritdoc />
    public async ValueTask<CommandRateLimitLease> AcquireAsync(
        ICommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var permitCount = _permitCountSelector(command);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(permitCount);
        var pendingLease = _rateLimiter.AcquireAsync(command, permitCount, cancellationToken);
        var wasQueued = !pendingLease.IsCompletedSuccessfully;
        var lease = await pendingLease.ConfigureAwait(false);
        var metadata = ReadMetadata(lease);

        if (!lease.IsAcquired)
        {
            lease.Dispose();
            var problem = Problem.Create(
                ProblemConstants.CommandExecutionTitles.CommandRateLimitExceeded,
                ProblemConstants.CommandExecutionMessages.RateLimitExceeded,
                HttpStatusCode.TooManyRequests);

            if (metadata is not null)
            {
                foreach (var pair in metadata)
                {
                    problem.Extensions[pair.Key] = pair.Value;
                }
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
        return _ownsRateLimiter ? _rateLimiter.DisposeAsync() : ValueTask.CompletedTask;
    }

    private static Dictionary<string, object?>? ReadMetadata(RateLimitLease lease)
    {
        Dictionary<string, object?>? metadata = null;
        foreach (var name in lease.MetadataNames)
        {
            if (lease.TryGetMetadata(name, out var value))
            {
                metadata ??= new Dictionary<string, object?>(StringComparer.Ordinal);
                metadata[name] = value;
            }
        }

        if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            metadata ??= new Dictionary<string, object?>(StringComparer.Ordinal);
            metadata[ProblemConstants.ExtensionKeys.RetryAfter] = retryAfter;
        }

        return metadata;
    }
}
