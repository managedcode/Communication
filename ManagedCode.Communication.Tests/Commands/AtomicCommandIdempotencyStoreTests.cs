using System;
using System.Net;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Commands.Stores;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ManagedCode.Communication.Tests.Commands;

public sealed class AtomicCommandIdempotencyStoreTests
{
    [Test]
    public async Task AtomicStore_CompletedNull_IsDistinctFromMissingPayload()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var store = CreateStore(cache);
        var descriptor = CreateDescriptor(CommandExecutionTestConstants.NullResult, typeof(string).FullName!);
        var acquired = await store.TryAcquireAsync<string?>(descriptor);

        (await store.TryCompleteAsync<string?>(acquired.Claim!, null, TimeSpan.FromMinutes(1))).ShouldBeTrue();
        var replay = await store.TryAcquireAsync<string?>(descriptor);

        replay.State.ShouldBe(CommandIdempotencyAcquireState.Completed);
        replay.HasOutcome.ShouldBeTrue();
        replay.Outcome.ShouldBeNull();
    }

    [Test]
    public async Task AtomicStore_ReusedKeyWithDifferentFingerprint_FailsClosed()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var store = CreateStore(cache);
        var first = CreateDescriptor(
            CommandExecutionTestConstants.Shared,
            typeof(int).FullName!,
            CommandExecutionTestConstants.RequestA);
        var second = CreateDescriptor(
            CommandExecutionTestConstants.Shared,
            typeof(int).FullName!,
            CommandExecutionTestConstants.RequestB);

        (await store.TryAcquireAsync<int>(first)).State.ShouldBe(CommandIdempotencyAcquireState.Acquired);
        var conflict = await store.TryAcquireAsync<int>(second);

        conflict.State.ShouldBe(CommandIdempotencyAcquireState.Conflict);
        conflict.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.Conflict);
    }

    [Test]
    public async Task AtomicStore_ExpiredOwnerBecomesIndeterminateAndCannotBeReclaimed()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var store = CreateStore(cache);
        var descriptor = CreateDescriptor(
            CommandExecutionTestConstants.Fenced,
            typeof(int).FullName!,
            claimLease: TimeSpan.FromMilliseconds(20));
        var stale = await store.TryAcquireAsync<int>(descriptor);
        await Task.Delay(40);
        var current = await store.TryAcquireAsync<int>(descriptor);

        current.State.ShouldBe(CommandIdempotencyAcquireState.Indeterminate);
        (await store.TryCompleteAsync(stale.Claim!, 1, TimeSpan.FromMinutes(1))).ShouldBeFalse();
        (await store.TryResolveIndeterminateAsync(descriptor, 2, TimeSpan.FromMinutes(1))).ShouldBeTrue();
        (await store.TryAcquireAsync<int>(descriptor)).Outcome.ShouldBe(2);
    }

    [Test]
    public async Task Maintenance_CleansAndCountsAtomicRecords()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var store = CreateStore(cache);
        var descriptor = CreateDescriptor(CommandExecutionTestConstants.Cleanup, typeof(int).FullName!);
        var acquired = await store.TryAcquireAsync<int>(descriptor);
        (await store.TryCompleteAsync(acquired.Claim!, 3, TimeSpan.FromMinutes(1))).ShouldBeTrue();

        (await store.GetCommandCountByStatusAsync())[CommandExecutionStatus.Completed].ShouldBe(1);
        await Task.Delay(20);
        (await store.CleanupCompletedCommandsAsync(TimeSpan.FromMilliseconds(1)))
            .ShouldBe(1);
        (await store.GetCommandCountByStatusAsync()).ShouldBeEmpty();
    }

    [Test]
    public async Task Maintenance_NeverDeletesIndeterminateRecords()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var store = CreateStore(cache);
        var descriptor = CreateDescriptor(CommandExecutionTestConstants.IndeterminateCleanup, typeof(int).FullName!);
        var acquired = await store.TryAcquireAsync<int>(descriptor);
        (await store.TryMarkIndeterminateAsync(
                acquired.Claim!,
                Problem.Create(
                    CommandExecutionTestConstants.UnknownOutcome,
                    CommandExecutionTestConstants.OperatorResolutionRequired,
                    HttpStatusCode.Conflict)))
            .ShouldBeTrue();
        await Task.Delay(20);

        (await store.CleanupCompletedCommandsAsync(TimeSpan.FromMilliseconds(1))).ShouldBe(0);
        (await store.TryAcquireAsync<int>(descriptor)).State.ShouldBe(CommandIdempotencyAcquireState.Indeterminate);
    }

    private static MemoryCacheCommandIdempotencyStore CreateStore(IMemoryCache cache) =>
        new(cache, NullLogger<MemoryCacheCommandIdempotencyStore>.Instance);

    private static CommandIdempotencyDescriptor CreateDescriptor(
        string key,
        string resultContract,
        string fingerprint = CommandExecutionTestConstants.Request,
        TimeSpan? claimLease = null) =>
        new(
            key,
            CommandExecutionTestConstants.Operation,
            fingerprint,
            resultContract,
            claimLease ?? TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1));
}
