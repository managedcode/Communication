using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands.Execution;
using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
using ManagedCode.Orleans.RateLimiting.Core.Models;
using ManagedCode.Orleans.RateLimiting.Core.Models.Holders;
using ManagedCode.Orleans.RateLimiting.Core.Models.Orchestration;

namespace ManagedCode.Communication.Orleans.RateLimiting;

/// <summary>
///     Uses ManagedCode.Orleans.RateLimiting as Communication's cluster-wide command limiter.
/// </summary>
public sealed class OrleansCommandRateLimiter(
    IRateLimitRequestOrchestrator orchestrator,
    OrleansCommandRateLimiterOptions options) : ICommandRateLimiter
{
    /// <inheritdoc />
    public async ValueTask<CommandRateLimitLease> AcquireAsync(
        ICommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var context = CreateContext(command, options);
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
            _ = DisposeAfterAcquireAsync(acquireTask, holder);
            throw;
        }
        catch
        {
            await holder.DisposeAsync().ConfigureAwait(false);
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
            metadata["retryAfter"] = lease.RetryAfter;
        }

        var problem = Problem.Create(
            "Command rate limit exceeded",
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

    private static RateLimitRequestContext CreateContext(
        ICommand command,
        OrleansCommandRateLimiterOptions options)
    {
        return new RateLimitRequestContext
        {
            OperationName = command.CommandType,
            UserId = command.UserId,
            GroupId = options.GroupId(command),
            TenantId = options.TenantId(command),
            Role = options.Role(command),
            IpAddress = command.Metadata?.IpAddress,
            Resource = options.Resource(command),
            PolicyName = options.PolicyName(command),
            Metadata = command.Metadata?.Tags is { } tags
                ? new Dictionary<string, string>(tags, StringComparer.Ordinal)
                : new Dictionary<string, string>(StringComparer.Ordinal)
        };
    }

    private static async Task DisposeAfterAcquireAsync(
        Task<OrleansRateLimitLease?> acquireTask,
        GroupLimiterHolder holder)
    {
        try
        {
            var lease = await acquireTask.ConfigureAwait(false);
            if (lease is not null)
            {
                await lease.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch
        {
            // The original execution has already observed cancellation. Cleanup is best effort.
        }
        finally
        {
            try
            {
                await holder.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
                // Cleanup is best effort after the original caller has already observed cancellation.
            }
        }
    }
}
