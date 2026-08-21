using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Commands.Execution;
using ManagedCode.Communication.Orleans.Stores;
using ManagedCode.Communication.Tests.Orleans.Fixtures;
using Shouldly;

namespace ManagedCode.Communication.Tests.Orleans;

[ClassDataSource<OrleansClusterFixture>(Shared = SharedType.PerClass)]
[NotInParallel(nameof(CommandIdempotencyOrleansIntegrationTests))]
public sealed class CommandIdempotencyOrleansIntegrationTests
{
    private readonly OrleansCommandIdempotencyStore _store;

    public CommandIdempotencyOrleansIntegrationTests(OrleansClusterFixture fixture)
    {
        _store = new OrleansCommandIdempotencyStore(fixture.Cluster.GrainFactory);
    }

    [Test]
    public async Task AtomicStore_CompletesAndReplaysOutcome()
    {
        var descriptor = CreateDescriptor(string.Format(
            CommandExecutionTestConstants.CompleteKeyFormat,
            Guid.CreateVersion7()));
        var acquired = await _store.TryAcquireAsync<Result<int>>(descriptor);

        (await _store.TryCompleteAsync(acquired.Claim!, Result<int>.Succeed(17), TimeSpan.FromMinutes(1)))
            .ShouldBeTrue();
        var replay = await _store.TryAcquireAsync<Result<int>>(descriptor);

        replay.State.ShouldBe(CommandIdempotencyAcquireState.Completed);
        replay.HasOutcome.ShouldBeTrue();
        replay.Outcome!.Value.ShouldBe(17);
    }

    [Test]
    public async Task AtomicStore_ExpiredClaimBecomesIndeterminateAndCannotBeReclaimedAutomatically()
    {
        var descriptor = CreateDescriptor(
            string.Format(CommandExecutionTestConstants.ExpiredKeyFormat, Guid.CreateVersion7()),
            claimLease: TimeSpan.FromMilliseconds(500));
        var stale = await _store.TryAcquireAsync<Result<int>>(descriptor);
        await Task.Delay(700);

        var next = await _store.TryAcquireAsync<Result<int>>(descriptor);

        next.State.ShouldBe(CommandIdempotencyAcquireState.Indeterminate);
        (await _store.TryCompleteAsync(stale.Claim!, Result<int>.Succeed(1), TimeSpan.FromMinutes(1)))
            .ShouldBeFalse();
        (await _store.TryResetIndeterminateAsync(descriptor)).ShouldBeTrue();
        (await _store.TryAcquireAsync<Result<int>>(descriptor)).State.ShouldBe(CommandIdempotencyAcquireState.Acquired);
    }

    [Test]
    public void Store_DoesNotPretendToSupportGlobalMaintenance()
    {
        ((object)_store is ICommandIdempotencyMaintenance).ShouldBeFalse();
    }

    [Test]
    public async Task CommandExecutor_WithRealOrleansStore_ReplaysWithoutRepeatingHandler()
    {
        var options = new CommandExecutionOptions();
        options.Timeout.Enabled = false;
        options.Retry.Enabled = false;
        options.Idempotency.ScopeSelector = static _ => CommandExecutionTestConstants.TenantA;
        options.Idempotency.FingerprintSelector = static _ => CommandExecutionTestConstants.RequestV1;
        var runtime = new CommandExecutionRuntime(options, _store);
        var command = Command.Create(CommandExecutionTestConstants.OrdersCreate, Guid.CreateVersion7());
        var calls = 0;

        var first = await CommandExecutor.ExecuteAsync(
            command,
            (_, _) => Task.FromResult(Interlocked.Increment(ref calls)),
            runtime);
        var replay = await CommandExecutor.ExecuteAsync(
            command,
            (_, _) => Task.FromResult(Interlocked.Increment(ref calls)),
            runtime);

        first.Value.ShouldBe(1);
        replay.Value.ShouldBe(1);
        calls.ShouldBe(1);
    }

    private static CommandIdempotencyDescriptor CreateDescriptor(
        string key,
        TimeSpan? claimLease = null) =>
        new(
            key,
            CommandExecutionTestConstants.OrdersCreate,
            CommandExecutionTestConstants.PayloadV1,
            typeof(Result<int>).FullName!,
            claimLease ?? TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1));
}
