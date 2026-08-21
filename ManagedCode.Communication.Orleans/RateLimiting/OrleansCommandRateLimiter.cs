using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands.Execution;
using ManagedCode.Communication.Constants;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication.Orleans.RateLimiting;

/// <summary>
///     Uses ManagedCode.Orleans.RateLimiting as Communication's cluster-wide command limiter.
/// </summary>
public sealed class OrleansCommandRateLimiter(
    IRateLimitRequestOrchestrator orchestrator,
    OrleansCommandRateLimiterOptions options,
    ILogger<OrleansCommandRateLimiter>? logger = null) : ICommandRateLimiter, IAsyncDisposable
{
    private readonly ConcurrentDictionary<long, Task> _cancelledAcquisitionCleanups = new();
    private readonly OrleansCommandRateLimiterOptions _options =
        OrleansCommandRateLimiterOptions.CreateSnapshot(options);
    private long _cleanupId;

    /// <inheritdoc />
    public async ValueTask<CommandRateLimitLease> AcquireAsync(
        ICommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_options.CancellationCleanupTimeout, TimeSpan.Zero);

        var context = CreateContext(command, _options);
        var holder = await orchestrator.CreateLimiterGroupAsync(context, cancellationToken).ConfigureAwait(false);
        var acquireTask = holder.AcquireAsync();
        var wasQueued = !acquireTask.IsCompletedSuccessfully;
        OrleansRateLimitLease? lease;

        try
        {
            lease = await acquireTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TrackCancelledAcquisitionCleanup(acquireTask, holder);
            throw;
        }
        catch (Exception)
        {
            try
            {
                await holder.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception cleanupFailure)
            {
                logger?.LogWarning(
                    cleanupFailure,
                    OrleansCommandExecutionConstants.DisposeHolderAfterAcquireFailureLog);
            }

            throw;
        }

        if (lease is null)
        {
            return CommandRateLimitLease.Acquired(
                wasQueued,
                disposeAsync: holder.DisposeAsync);
        }

        var metadata = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var pair in lease.GetAllMetadata())
        {
            metadata[pair.Key] = pair.Value;
        }

        if (lease.RetryAfter > TimeSpan.Zero)
        {
            metadata[ProblemConstants.ExtensionKeys.RetryAfter] = lease.RetryAfter;
        }

        var problem = Problem.Create(
            ProblemConstants.CommandExecutionTitles.CommandRateLimitExceeded,
            lease.Reason,
            HttpStatusCode.TooManyRequests);
        foreach (var pair in metadata)
        {
            problem.Extensions[pair.Key] = pair.Value;
        }

        await lease.DisposeAsync().ConfigureAwait(false);
        await holder.DisposeAsync().ConfigureAwait(false);
        return CommandRateLimitLease.Rejected(problem, wasQueued, metadata);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        var cleanups = _cancelledAcquisitionCleanups.Values.ToArray();
        if (cleanups.Length == 0)
        {
            return;
        }

        try
        {
            await Task.WhenAll(cleanups).ConfigureAwait(false);
        }
        catch (Exception caught)
        {
            logger?.LogWarning(caught, OrleansCommandExecutionConstants.CancellationCleanupShutdownFailureLog);
        }
    }

    private static RateLimitRequestContext CreateContext(
        ICommand command,
        OrleansCommandRateLimiterOptions options)
    {
        return new RateLimitRequestContext
        {
            OperationName = command.CommandType,
            UserId = options.UserId(command),
            GroupId = options.GroupId(command),
            TenantId = options.TenantId(command),
            Role = options.Role(command),
            IpAddress = options.IpAddress(command),
            Resource = options.Resource(command),
            PolicyName = options.PolicyName(command),
            Metadata = new Dictionary<string, string>(options.Metadata(command), StringComparer.Ordinal)
        };
    }

    private void TrackCancelledAcquisitionCleanup(
        Task<OrleansRateLimitLease?> acquireTask,
        GroupLimiterHolder holder)
    {
        var id = Interlocked.Increment(ref _cleanupId);
        var cleanup = DisposeAfterAcquireAsync(acquireTask, holder, _options.CancellationCleanupTimeout, logger);
        _cancelledAcquisitionCleanups[id] = cleanup;
        _ = cleanup.ContinueWith(
            (completedCleanup, state) =>
            {
                _ = completedCleanup.Exception;
                var tuple = ((ConcurrentDictionary<long, Task> Registry, long Id))state!;
                tuple.Registry.TryRemove(tuple.Id, out _);
            },
            (_cancelledAcquisitionCleanups, id),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private static async Task DisposeAfterAcquireAsync(
        Task<OrleansRateLimitLease?> acquireTask,
        GroupLimiterHolder holder,
        TimeSpan cleanupTimeout,
        ILogger? logger)
    {
        try
        {
            var lease = await acquireTask.WaitAsync(cleanupTimeout).ConfigureAwait(false);
            if (lease is not null)
            {
                await lease.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (TimeoutException caught)
        {
            logger?.LogWarning(
                caught,
                OrleansCommandExecutionConstants.CancellationCleanupTimeoutLog,
                cleanupTimeout);
            ObserveAndDisposeLateLease(acquireTask, logger);
        }
        catch (Exception caught)
        {
            logger?.LogWarning(caught, OrleansCommandExecutionConstants.CancellationCleanupFailureLog);
        }
        finally
        {
            try
            {
                await holder.DisposeAsync().AsTask().WaitAsync(cleanupTimeout).ConfigureAwait(false);
            }
            catch (Exception caught)
            {
                logger?.LogWarning(caught, OrleansCommandExecutionConstants.DisposeHolderAfterCancellationFailureLog);
            }
        }
    }

    private static void ObserveAndDisposeLateLease(
        Task<OrleansRateLimitLease?> acquireTask,
        ILogger? logger)
    {
        _ = acquireTask.ContinueWith(
            static async (completed, state) =>
            {
                var cleanupLogger = (ILogger?)state;
                try
                {
                    if (completed.Status == TaskStatus.RanToCompletion && completed.Result is { } lease)
                    {
                        await lease.DisposeAsync().ConfigureAwait(false);
                    }
                    else if (completed.Exception is { } exception)
                    {
                        cleanupLogger?.LogWarning(exception, OrleansCommandExecutionConstants.LateAcquisitionFailureLog);
                    }
                }
                catch (Exception caught)
                {
                    cleanupLogger?.LogWarning(caught, OrleansCommandExecutionConstants.DisposeLateLeaseFailureLog);
                }
            },
            logger,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default).Unwrap();
    }
}
