using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Commands.Execution;
using ManagedCode.Communication.Commands.Stores;
using ManagedCode.Communication.Constants;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ManagedCode.Communication.Tests.Commands;

[NotInParallel]
public sealed class CommandExecutionIdempotencyFaultTests
{
    [Test]
    public async Task MissingTrustedScopeOrFingerprint_FailsClosedWithoutInvokingHandler()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var store = CreateStore(cache);
        var missingScopeOptions = new CommandExecutionOptions();
        missingScopeOptions.Timeout.Enabled = false;
        var missingFingerprintOptions = new CommandExecutionOptions();
        missingFingerprintOptions.Timeout.Enabled = false;
        missingFingerprintOptions.Idempotency.ScopeSelector = static _ => CommandExecutionTestConstants.TenantA;
        var calls = 0;

        var missingScope = await CommandExecutor.ExecuteAsync(
            Command.Create(CommandExecutionTestConstants.PaymentsCapture, Guid.CreateVersion7()),
            (_, _) => Task.FromResult(Interlocked.Increment(ref calls)),
            new CommandExecutionRuntime(missingScopeOptions, store));
        var missingFingerprint = await CommandExecutor.ExecuteAsync(
            Command.Create(CommandExecutionTestConstants.PaymentsCapture, Guid.CreateVersion7()),
            (_, _) => Task.FromResult(Interlocked.Increment(ref calls)),
            new CommandExecutionRuntime(missingFingerprintOptions, store));

        missingScope.Problem!.Title.ShouldBe(ProblemConstants.CommandExecutionTitles.MissingIdempotencyScope);
        missingFingerprint.Problem!.Title.ShouldBe(
            ProblemConstants.CommandExecutionTitles.MissingIdempotencyFingerprint);
        calls.ShouldBe(0);
    }

    [Test]
    public async Task CompleteFailureBeforeCommit_MarksOutcomeIndeterminateAndNeverRepeatsHandler()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var inner = CreateStore(cache);
        var store = new FaultingCompletionStore(inner, throwAfterCommit: false);
        var runtime = CreateRuntime(store);
        var command = Command.Create(CommandExecutionTestConstants.PaymentsCapture, Guid.CreateVersion7());
        var calls = 0;

        var first = await CommandExecutor.ExecuteAsync(
            command,
            (_, _) => Task.FromResult(Interlocked.Increment(ref calls)),
            runtime);
        var duplicate = await CommandExecutor.ExecuteAsync(
            command,
            (_, _) => Task.FromResult(Interlocked.Increment(ref calls)),
            runtime);

        first.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
        (first.Problem.Detail ?? string.Empty).ShouldNotContain(CommandExecutionTestConstants.Injected);
        duplicate.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.Conflict);
        calls.ShouldBe(1);
    }

    [Test]
    public async Task CompleteFailureAfterCommit_ReplaysCommittedOutcomeAndNeverRepeatsHandler()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var inner = CreateStore(cache);
        var store = new FaultingCompletionStore(inner, throwAfterCommit: true);
        var runtime = CreateRuntime(store);
        var command = Command.Create(CommandExecutionTestConstants.PaymentsCapture, Guid.CreateVersion7());
        var calls = 0;

        var first = await CommandExecutor.ExecuteAsync(
            command,
            (_, _) => Task.FromResult(Interlocked.Increment(ref calls)),
            runtime);
        var duplicate = await CommandExecutor.ExecuteAsync(
            command,
            (_, _) => Task.FromResult(Interlocked.Increment(ref calls)),
            runtime);

        first.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
        duplicate.Value.ShouldBe(1);
        calls.ShouldBe(1);
    }

    private static CommandExecutionRuntime CreateRuntime(ICommandIdempotencyStore store)
    {
        var options = new CommandExecutionOptions();
        options.Timeout.Enabled = false;
        options.Retry.Enabled = false;
        options.Idempotency.ScopeSelector = static _ => CommandExecutionTestConstants.TenantA;
        options.Idempotency.FingerprintSelector = static _ => CommandExecutionTestConstants.RequestV1;
        return new CommandExecutionRuntime(options, store);
    }

    private static MemoryCacheCommandIdempotencyStore CreateStore(IMemoryCache cache) =>
        new(cache, NullLogger<MemoryCacheCommandIdempotencyStore>.Instance);

    private sealed class FaultingCompletionStore(
        ICommandIdempotencyStore inner,
        bool throwAfterCommit) : ICommandIdempotencyStore
    {
        private int _faulted;

        public Task<CommandIdempotencyAcquireResult<T>> TryAcquireAsync<T>(
            CommandIdempotencyDescriptor descriptor,
            CancellationToken cancellationToken = default) =>
            inner.TryAcquireAsync<T>(descriptor, cancellationToken);

        public async Task<bool> TryCompleteAsync<T>(
            CommandIdempotencyClaim claim,
            T outcome,
            TimeSpan retention,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _faulted, 1) != 0)
            {
                return await inner.TryCompleteAsync(claim, outcome, retention, cancellationToken);
            }

            if (throwAfterCommit)
            {
                _ = await inner.TryCompleteAsync(claim, outcome, retention, cancellationToken);
            }

            throw new InvalidOperationException(CommandExecutionTestConstants.InjectedCompletionFailure);
        }

        public Task<bool> TryRenewAsync(
            CommandIdempotencyClaim claim,
            TimeSpan lease,
            CancellationToken cancellationToken = default) =>
            inner.TryRenewAsync(claim, lease, cancellationToken);

        public Task<bool> TryMarkIndeterminateAsync(
            CommandIdempotencyClaim claim,
            Problem problem,
            CancellationToken cancellationToken = default) =>
            inner.TryMarkIndeterminateAsync(claim, problem, cancellationToken);

        public Task<bool> TryReleaseAsync(
            CommandIdempotencyClaim claim,
            CancellationToken cancellationToken = default) =>
            inner.TryReleaseAsync(claim, cancellationToken);

        public Task<bool> TryResolveIndeterminateAsync<T>(
            CommandIdempotencyDescriptor descriptor,
            T outcome,
            TimeSpan retention,
            CancellationToken cancellationToken = default) =>
            inner.TryResolveIndeterminateAsync(descriptor, outcome, retention, cancellationToken);

        public Task<bool> TryResetIndeterminateAsync(
            CommandIdempotencyDescriptor descriptor,
            CancellationToken cancellationToken = default) =>
            inner.TryResetIndeterminateAsync(descriptor, cancellationToken);
    }
}
