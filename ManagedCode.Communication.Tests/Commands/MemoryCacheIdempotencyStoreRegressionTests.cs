using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Commands.Extensions;
using ManagedCode.Communication.Commands.Stores;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ManagedCode.Communication.Tests.Commands;

/// <summary>
///     Regressions for defects found while auditing the in-memory idempotency store.
/// </summary>
public class MemoryCacheIdempotencyStoreRegressionTests
{
    private static MemoryCacheCommandIdempotencyStore CreateStore(out MemoryCache cache)
    {
        cache = new MemoryCache(new MemoryCacheOptions());
        return new MemoryCacheCommandIdempotencyStore(cache, NullLogger<MemoryCacheCommandIdempotencyStore>.Instance);
    }

    [Test]
    public async Task EvictingACacheEntryAlsoPrunesTheTimestampIndex()
    {
        // The store keeps a shadow index of command ids so it can implement age-based cleanup. Cache entries
        // expire on their own, but nothing used to remove them from that index, so it grew without bound in
        // any process that did not run the optional cleanup service.
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var store = new MemoryCacheCommandIdempotencyStore(
            cache, NullLogger<MemoryCacheCommandIdempotencyStore>.Instance);

        for (var i = 0; i < 50; i++)
        {
            await store.SetCommandStatusAsync($"cmd-{i}", CommandExecutionStatus.Completed);
        }

        (await store.GetCommandCountByStatusAsync())[CommandExecutionStatus.Completed].ShouldBe(50);

        // Evicting every cache entry must drain the index too.
        cache.Clear();
        cache.Compact(1.0);

        var counts = await store.GetCommandCountByStatusAsync();
        counts.ShouldBeEmpty();
    }

    [Test]
    public async Task RemovingACommandDropsBothStatusAndResult()
    {
        using var store = CreateStore(out var cache);
        using (cache)
        {
            await store.SetCommandStatusAsync("cmd", CommandExecutionStatus.Completed);
            await store.SetCommandResultAsync("cmd", 42);

            await store.RemoveCommandAsync("cmd");

            (await store.GetCommandStatusAsync("cmd")).ShouldBe(CommandExecutionStatus.NotFound);
            (await store.GetCommandResultAsync<int>("cmd")).ShouldBe(0);
        }
    }

    [Test]
    public void DisposeIsIdempotent()
    {
        var store = CreateStore(out var cache);
        using (cache)
        {
            Should.NotThrow(() =>
            {
                store.Dispose();
                store.Dispose();
            });
        }
    }

    [Test]
    public async Task ConcurrentCallersRunTheOperationExactlyOnce()
    {
        using var store = CreateStore(out var cache);
        using (cache)
        {
            var executions = 0;

            var callers = new Task<int>[16];
            for (var i = 0; i < callers.Length; i++)
            {
                callers[i] = store.ExecuteIdempotentAsync("shared", async () =>
                {
                    Interlocked.Increment(ref executions);
                    await Task.Delay(20);
                    return 7;
                });
            }

            var results = await Task.WhenAll(callers);

            results.ShouldAllBe(value => value == 7);
            executions.ShouldBe(1);
        }
    }

    [Test]
    public async Task ExecuteIdempotentRejectsMissingArguments()
    {
        using var store = CreateStore(out var cache);
        using (cache)
        {
            await Should.ThrowAsync<ArgumentNullException>(() =>
                CommandIdempotencyExtensions.ExecuteIdempotentAsync(null!, "id", () => Task.FromResult(1)));
            await Should.ThrowAsync<ArgumentException>(() =>
                store.ExecuteIdempotentAsync("  ", () => Task.FromResult(1)));
            await Should.ThrowAsync<ArgumentNullException>(() =>
                store.ExecuteIdempotentAsync("id", (Func<Task<int>>)null!));
        }
    }

    [Test]
    public async Task RetryReExecutesAfterAFailureAndEventuallySucceeds()
    {
        using var store = CreateStore(out var cache);
        using (cache)
        {
            var attempts = 0;

            var value = await store.ExecuteIdempotentWithRetryAsync(
                "flaky",
                () =>
                {
                    attempts++;
                    if (attempts < 3)
                    {
                        throw new InvalidOperationException("transient");
                    }

                    return Task.FromResult("ok");
                },
                baseDelay: TimeSpan.FromMilliseconds(1));

            value.ShouldBe("ok");
            attempts.ShouldBe(3);
        }
    }

    [Test]
    public async Task RetryGivesUpAndRethrowsTheLastFailure()
    {
        using var store = CreateStore(out var cache);
        using (cache)
        {
            var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
                store.ExecuteIdempotentWithRetryAsync<int>(
                    "always-fails",
                    () => throw new InvalidOperationException("permanent"),
                    maxRetries: 2,
                    baseDelay: TimeSpan.FromMilliseconds(1)));

            exception.Message.ShouldBe("permanent");
        }
    }

    [Test]
    public async Task TimeoutSurfacesAsTimeoutExceptionRatherThanCancellation()
    {
        using var store = CreateStore(out var cache);
        using (cache)
        {
            // The token-aware overload lets the timeout actually interrupt the operation.
            await Should.ThrowAsync<TimeoutException>(() =>
                store.ExecuteWithTimeoutAsync(
                    "slow",
                    async token =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30), token);
                        return 1;
                    },
                    TimeSpan.FromMilliseconds(50)));
        }
    }

    [Test]
    public async Task CallerCancellationStillSurfacesAsCancellation()
    {
        using var store = CreateStore(out var cache);
        using (cache)
        {
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();

            await Should.ThrowAsync<OperationCanceledException>(() =>
                store.ExecuteWithTimeoutAsync(
                    "cancelled",
                    () => Task.FromResult(1),
                    TimeSpan.FromMinutes(1),
                    cancellation.Token));
        }
    }
}
