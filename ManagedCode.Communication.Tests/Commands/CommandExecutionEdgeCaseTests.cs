using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.RateLimiting;
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
public sealed class CommandExecutionEdgeCaseTests
{
    [Test]
    public async Task ExecuteAsync_WithConcurrentDuplicates_ExecutesOnlyOneHandlerAndReplaysItsResult()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var store = CreateStore(cache);
        var runtime = CreateRuntime(idempotencyStore: store);
        var commandId = Guid.CreateVersion7();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handlerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseHandler = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executions = 0;

        var tasks = Enumerable.Range(0, 12)
            .Select(async _ =>
            {
                await start.Task;
                return await CommandExecutor.ExecuteAsync(
                    Command.Create("payment.capture", commandId),
                    async (_, token) =>
                    {
                        Interlocked.Increment(ref executions);
                        handlerStarted.TrySetResult();
                        await releaseHandler.Task.WaitAsync(token);
                        return 73;
                    },
                    runtime);
            })
            .ToArray();

        start.SetResult();
        await handlerStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(50);

        Volatile.Read(ref executions).ShouldBe(1);
        releaseHandler.SetResult();

        var results = await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(5));
        results.ShouldAllBe(static result => result.IsSuccess && result.Value == 73);
        executions.ShouldBe(1);
    }

    [Test]
    public async Task ExecuteAsync_WhenDuplicateCallerCancels_DoesNotCancelOwnerOrPoisonCachedResult()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var store = CreateStore(cache);
        var runtime = CreateRuntime(idempotencyStore: store);
        var commandId = Guid.CreateVersion7();
        var ownerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseOwner = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executions = 0;

        var owner = CommandExecutor.ExecuteAsync(
            Command.Create("invoice.issue", commandId),
            async (_, token) =>
            {
                Interlocked.Increment(ref executions);
                ownerStarted.SetResult();
                await releaseOwner.Task.WaitAsync(token);
                return 41;
            },
            runtime);
        await ownerStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        using var duplicateCancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(30));
        var duplicate = async () => await CommandExecutor.ExecuteAsync(
            Command.Create("invoice.issue", commandId),
            static (_, _) => Task.FromResult(99),
            runtime,
            duplicateCancellation.Token);

        await Should.ThrowAsync<OperationCanceledException>(duplicate);
        releaseOwner.SetResult();
        var ownerResult = await owner.WaitAsync(TimeSpan.FromSeconds(5));
        var replay = await CommandExecutor.ExecuteAsync(
            Command.Create("invoice.issue", commandId),
            static (_, _) => Task.FromResult(99),
            runtime);

        ownerResult.Value.ShouldBe(41);
        replay.Value.ShouldBe(41);
        executions.ShouldBe(1);
    }

    [Test]
    public async Task ExecuteResultAsync_WithRetryAndIdempotency_CachesOnlyTheFinalOutcome()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var store = CreateStore(cache);
        var runtime = CreateRuntime(
            options =>
            {
                options.Retry.Enabled = true;
                options.Retry.MaxRetries = 2;
            },
            store);
        var commandId = Guid.CreateVersion7();
        var executions = 0;

        var first = await CommandExecutor.ExecuteResultAsync(
            Command.Create("stock.reserve", commandId),
            (_, _) => Task.FromResult(Interlocked.Increment(ref executions) == 1
                ? Result<int>.Fail(Problem.Create(HttpStatusCode.ServiceUnavailable))
                : Result<int>.Succeed(12)),
            runtime);
        var replay = await CommandExecutor.ExecuteResultAsync(
            Command.Create("stock.reserve", commandId),
            static (_, _) => Task.FromResult(Result<int>.Succeed(999)),
            runtime);

        first.Value.ShouldBe(12);
        replay.Value.ShouldBe(12);
        executions.ShouldBe(2);
    }

    [Test]
    public async Task ExecuteAsync_WithEmptyCommandIdAndIdempotency_SkipsSideEffect()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var store = CreateStore(cache);
        var runtime = CreateRuntime(idempotencyStore: store);
        var command = Command.Create("email.send");
        command.CommandId = Guid.Empty;
        var invoked = false;

        var result = await CommandExecutor.ExecuteAsync(
            command,
            (_, _) =>
            {
                invoked = true;
                return Task.FromResult(1);
            },
            runtime);

        result.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
        invoked.ShouldBeFalse();
    }

    [Test]
    public async Task ExecuteResultAsync_WhenTotalTimeoutExpiresDuringBackoff_ReturnsTimeoutWithoutAnotherAttempt()
    {
        var attempts = 0;
        var runtime = CreateRuntime(options =>
        {
            options.Timeout.Enabled = true;
            options.Timeout.TotalTimeout = TimeSpan.FromMilliseconds(30);
            options.Retry.Enabled = true;
            options.Retry.MaxRetries = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(10);
            options.Retry.MaxDelay = TimeSpan.FromSeconds(10);
        });

        var result = await CommandExecutor.ExecuteResultAsync(
            Command.Create("ledger.post"),
            (_, _) =>
            {
                Interlocked.Increment(ref attempts);
                return Task.FromResult(Result.Fail(Problem.Create(HttpStatusCode.ServiceUnavailable)));
            },
            runtime);

        result.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.RequestTimeout);
        attempts.ShouldBe(1);
    }

    [Test]
    public async Task ExecuteResultAsync_WhenCallerCancelsDuringBackoff_PropagatesCancellationAndDoesNotRetry()
    {
        using var cancellationSource = new CancellationTokenSource();
        var attempts = 0;
        var runtime = CreateRuntime(options =>
        {
            options.Retry.Enabled = true;
            options.Retry.MaxRetries = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(10);
            options.Retry.MaxDelay = TimeSpan.FromSeconds(10);
            options.Retry.OnRetry = (_, _) =>
            {
                cancellationSource.Cancel();
                return ValueTask.CompletedTask;
            };
        });

        var action = async () => await CommandExecutor.ExecuteResultAsync(
            Command.Create("ledger.post"),
            (_, _) =>
            {
                Interlocked.Increment(ref attempts);
                return Task.FromResult(Result.Fail(Problem.Create(HttpStatusCode.ServiceUnavailable)));
            },
            runtime,
            cancellationSource.Token);

        await Should.ThrowAsync<OperationCanceledException>(action);
        attempts.ShouldBe(1);
    }

    [Test]
    public async Task ExecuteResultAsync_WithCommandRetryBudget_CapsTheGlobalRetryBudget()
    {
        var attempts = 0;
        var command = Command.Create("provider.call");
        command.Metadata = new CommandMetadata { MaxRetries = 1 };
        var runtime = CreateRuntime(options =>
        {
            options.Retry.Enabled = true;
            options.Retry.MaxRetries = 5;
        });

        var result = await CommandExecutor.ExecuteResultAsync(
            command,
            (_, _) =>
            {
                Interlocked.Increment(ref attempts);
                return Task.FromResult(Result.Fail(Problem.Create(HttpStatusCode.BadGateway)));
            },
            runtime);

        result.IsFailed.ShouldBeTrue();
        attempts.ShouldBe(2);
        command.Metadata.RetryCount.ShouldBe(1);
        result.Problem!.Extensions[ProblemConstants.ExtensionKeys.RetryAttempts].ShouldBe(2);
    }

    [Test]
    public async Task ExecuteAsync_WhenLimiterRejectsWithRetryAfterMetadata_UsesThatDelayForRetry()
    {
        var retryAfter = TimeSpan.FromMilliseconds(7);
        var retryDelay = TimeSpan.Zero;
        var handlerInvocations = 0;
        var limiter = new SequenceRateLimiter(
            CommandRateLimitLease.Rejected(
                Problem.Create(HttpStatusCode.TooManyRequests),
                metadata: new Dictionary<string, object?>
                {
                    [ProblemConstants.ExtensionKeys.RetryAfter] = retryAfter
                }),
            CommandRateLimitLease.Acquired());
        var runtime = CreateRuntime(
            options =>
            {
                options.Retry.Enabled = true;
                options.Retry.MaxRetries = 1;
                options.Retry.Delay = TimeSpan.FromSeconds(1);
                options.Retry.OnRetry = (retryEvent, _) =>
                {
                    retryDelay = retryEvent.Delay;
                    return ValueTask.CompletedTask;
                };
            },
            rateLimiter: limiter);

        var result = await CommandExecutor.ExecuteAsync(
            Command.Create("provider.call"),
            (_, _) => Task.FromResult(Interlocked.Increment(ref handlerInvocations)),
            runtime);

        result.Value.ShouldBe(1);
        retryDelay.ShouldBe(retryAfter);
        limiter.Acquisitions.ShouldBe(2);
    }

    [Test]
    [Arguments(-1D)]
    [Arguments(double.NaN)]
    public async Task ExecuteResultAsync_WithMalformedRetryAfter_DoesNotCrashExecution(double retryAfterSeconds)
    {
        var attempts = 0;
        var runtime = CreateRuntime(options =>
        {
            options.Retry.Enabled = true;
            options.Retry.MaxRetries = 1;
            options.Retry.Delay = TimeSpan.Zero;
        });

        var result = await CommandExecutor.ExecuteResultAsync(
            Command.Create("provider.call"),
            (_, _) =>
            {
                if (Interlocked.Increment(ref attempts) > 1)
                {
                    return Task.FromResult(Result<int>.Succeed(8));
                }

                var problem = Problem.Create(HttpStatusCode.TooManyRequests);
                problem.Extensions[ProblemConstants.ExtensionKeys.RetryAfter] = retryAfterSeconds;
                return Task.FromResult(Result<int>.Fail(problem));
            },
            runtime);

        result.Value.ShouldBe(8);
        attempts.ShouldBe(2);
    }

    [Test]
    public async Task ExecuteResultAsync_WithRetry_ReacquiresAndDisposesAPermitForEveryAttempt()
    {
        var disposed = 0;
        var attempts = 0;
        var limiter = new SequenceRateLimiter(
            CommandRateLimitLease.Acquired(disposeAsync: DisposeLease),
            CommandRateLimitLease.Acquired(disposeAsync: DisposeLease));
        var runtime = CreateRuntime(
            options =>
            {
                options.Retry.Enabled = true;
                options.Retry.MaxRetries = 1;
            },
            rateLimiter: limiter);

        var result = await CommandExecutor.ExecuteResultAsync(
            Command.Create("provider.call"),
            (_, _) => Task.FromResult(Interlocked.Increment(ref attempts) == 1
                ? Result<int>.Fail(Problem.Create(HttpStatusCode.ServiceUnavailable))
                : Result<int>.Succeed(9)),
            runtime);

        result.Value.ShouldBe(9);
        limiter.Acquisitions.ShouldBe(2);
        disposed.ShouldBe(2);
        return;

        ValueTask DisposeLease()
        {
            Interlocked.Increment(ref disposed);
            return ValueTask.CompletedTask;
        }
    }

    [Test]
    public async Task ExecuteAsync_WhenHandlerThrows_DisposesPermitAndReturnsFailure()
    {
        var disposed = 0;
        var limiter = new SequenceRateLimiter(CommandRateLimitLease.Acquired(disposeAsync: () =>
        {
            Interlocked.Increment(ref disposed);
            return ValueTask.CompletedTask;
        }));
        var runtime = CreateRuntime(rateLimiter: limiter);

        var result = await CommandExecutor.ExecuteAsync(
            Command.Create("provider.call"),
            static (_, _) => Task.FromException<int>(new InvalidOperationException("socket closed")),
            runtime);

        result.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
        disposed.ShouldBe(1);
    }

    [Test]
    public async Task ExecuteResultAsync_WhenRetryIsDisabled_DoesNotInvokeRetryPoliciesOrExhaustionCallbacks()
    {
        var predicateCalls = 0;
        var exhaustedCalls = 0;
        var runtime = CreateRuntime(options =>
        {
            options.Retry.Enabled = false;
            options.Retry.ShouldRetry = _ =>
            {
                Interlocked.Increment(ref predicateCalls);
                return true;
            };
            options.Retry.OnRetriesExhausted = (_, _) =>
            {
                Interlocked.Increment(ref exhaustedCalls);
                return ValueTask.CompletedTask;
            };
        });

        var result = await CommandExecutor.ExecuteResultAsync(
            Command.Create(CommandExecutionTestConstants.RetryDisabled),
            static (_, _) => Task.FromResult(Result.Fail(Problem.Create(HttpStatusCode.ServiceUnavailable))),
            runtime);

        result.IsFailed.ShouldBeTrue();
        predicateCalls.ShouldBe(0);
        exhaustedCalls.ShouldBe(0);
        result.Problem!.Extensions.ShouldNotContainKey(ProblemConstants.ExtensionKeys.RetriesExhausted);
    }

    [Test]
    public async Task ExecuteResultAsync_WithExistingRetryCount_ConsumesOnlyRemainingBudget()
    {
        var attempts = 0;
        var command = Command.Create(CommandExecutionTestConstants.RetryResume);
        command.Metadata = new CommandMetadata { RetryCount = 1, MaxRetries = 3 };
        var runtime = CreateRuntime(options =>
        {
            options.Retry.Enabled = true;
            options.Retry.MaxRetries = 3;
        });

        var result = await CommandExecutor.ExecuteResultAsync(
            command,
            (_, _) => Task.FromResult(Interlocked.Increment(ref attempts) == 3
                ? Result.Succeed()
                : Result.Fail(Problem.Create(HttpStatusCode.ServiceUnavailable))),
            runtime);

        result.IsSuccess.ShouldBeTrue();
        attempts.ShouldBe(3);
        command.Metadata.RetryCount.ShouldBe(3);
    }

    [Test]
    public async Task ExecuteAsync_WhenLeaseDisposalThrows_PreservesSuccessAndDoesNotRepeatSideEffect()
    {
        var handlerCalls = 0;
        var limiter = new SequenceRateLimiter(CommandRateLimitLease.Acquired(disposeAsync: static () =>
            ValueTask.FromException(new IOException(CommandExecutionTestConstants.ReleaseFailed))));
        var runtime = CreateRuntime(options =>
        {
            options.Retry.Enabled = true;
            options.Retry.MaxRetries = 2;
        }, rateLimiter: limiter);

        var result = await CommandExecutor.ExecuteAsync(
            Command.Create(CommandExecutionTestConstants.LeaseCleanup),
            (_, _) => Task.FromResult(Interlocked.Increment(ref handlerCalls)),
            runtime);

        result.Value.ShouldBe(1);
        handlerCalls.ShouldBe(1);
        limiter.Acquisitions.ShouldBe(1);
    }

    [Test]
    public async Task ExecuteAsync_WhenAdmissionCallbackThrows_PreservesAdmissionOutcome()
    {
        var handlerCalls = 0;
        var limiter = new SequenceRateLimiter(CommandRateLimitLease.Acquired(wasQueued: true));
        var runtime = CreateRuntime(
            options => options.RateLimiter.OnQueued = static (_, _) =>
                ValueTask.FromException(new InvalidOperationException(CommandExecutionTestConstants.ObserverFailed)),
            rateLimiter: limiter);

        var result = await CommandExecutor.ExecuteAsync(
            Command.Create(CommandExecutionTestConstants.CallbackFailure),
            (_, _) => Task.FromResult(Interlocked.Increment(ref handlerCalls)),
            runtime);

        result.Value.ShouldBe(1);
        handlerCalls.ShouldBe(1);
    }

    [Test]
    public async Task ExecuteAsync_WhenPreHandlerRateLimitRejects_DoesNotCacheTemporaryFailure()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var store = CreateStore(cache);
        var limiter = new SequenceRateLimiter(
            CommandRateLimitLease.Rejected(Problem.Create(HttpStatusCode.TooManyRequests)),
            CommandRateLimitLease.Acquired());
        var runtime = CreateRuntime(idempotencyStore: store, rateLimiter: limiter);
        var commandId = Guid.CreateVersion7();
        var handlerCalls = 0;

        var rejected = await CommandExecutor.ExecuteAsync(
            Command.Create(CommandExecutionTestConstants.AdmissionCache, commandId),
            (_, _) => Task.FromResult(Interlocked.Increment(ref handlerCalls)),
            runtime);
        var accepted = await CommandExecutor.ExecuteAsync(
            Command.Create(CommandExecutionTestConstants.AdmissionCache, commandId),
            (_, _) => Task.FromResult(Interlocked.Increment(ref handlerCalls)),
            runtime);

        rejected.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.TooManyRequests);
        accepted.Value.ShouldBe(1);
        handlerCalls.ShouldBe(1);
    }

    [Test]
    public async Task ExecuteAsync_WithShortClaimLease_RenewsOwnershipUntilHandlerCompletes()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var store = CreateStore(cache);
        var runtime = CreateRuntime(options =>
        {
            options.Idempotency.ClaimLease = TimeSpan.FromMilliseconds(300);
            options.Idempotency.DuplicatePollInterval = TimeSpan.FromMilliseconds(2);
        }, store);
        var commandId = Guid.CreateVersion7();
        var handlerCalls = 0;
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var owner = CommandExecutor.ExecuteAsync(
            Command.Create(CommandExecutionTestConstants.LeaseRenew, commandId),
            async (_, token) =>
            {
                Interlocked.Increment(ref handlerCalls);
                started.SetResult();
                await Task.Delay(900, token);
                return 7;
            },
            runtime);
        await started.Task;
        await Task.Delay(600);
        var duplicate = CommandExecutor.ExecuteAsync(
            Command.Create(CommandExecutionTestConstants.LeaseRenew, commandId),
            (_, _) => Task.FromResult(99),
            runtime);

        var results = await Task.WhenAll(owner, duplicate).WaitAsync(TimeSpan.FromSeconds(5));
        results[0].IsSuccess.ShouldBeTrue(string.Format(
            CommandExecutionTestConstants.OwnerFailureFormat,
            results[0].Problem?.Title,
            results[0].Problem?.Detail));
        results[1].IsSuccess.ShouldBeTrue(string.Format(
            CommandExecutionTestConstants.DuplicateFailureFormat,
            results[1].Problem?.Title,
            results[1].Problem?.Detail));
        results.ShouldAllBe(static result => result.Value == 7);
        handlerCalls.ShouldBe(1);
    }

    [Test]
    public async Task ExecuteAsync_WithSameExternalIdInDifferentTrustedScopes_DoesNotCrossReplay()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var store = CreateStore(cache);
        var tenantA = CreateRuntime(
            options => options.Idempotency.ScopeSelector = static _ => CommandExecutionTestConstants.TenantA,
            store);
        var tenantB = CreateRuntime(
            options => options.Idempotency.ScopeSelector = static _ => CommandExecutionTestConstants.TenantB,
            store);
        var commandId = Guid.CreateVersion7();

        var first = await CommandExecutor.ExecuteAsync(
            Command.Create(CommandExecutionTestConstants.PaymentCapture, commandId),
            static (_, _) => Task.FromResult(10),
            tenantA);
        var second = await CommandExecutor.ExecuteAsync(
            Command.Create(CommandExecutionTestConstants.PaymentCapture, commandId),
            static (_, _) => Task.FromResult(20),
            tenantB);

        first.Value.ShouldBe(10);
        second.Value.ShouldBe(20);
    }

    [Test]
    public async Task ExecuteAsync_WithSameScopedIdButDifferentRequestFingerprint_FailsClosed()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var store = CreateStore(cache);
        var requestA = CreateRuntime(
            options => options.Idempotency.FingerprintSelector = static _ => CommandExecutionTestConstants.PayloadA,
            store);
        var requestB = CreateRuntime(
            options => options.Idempotency.FingerprintSelector = static _ => CommandExecutionTestConstants.PayloadB,
            store);
        var commandId = Guid.CreateVersion7();
        var calls = 0;

        (await CommandExecutor.ExecuteAsync(
            Command.Create(CommandExecutionTestConstants.PaymentCapture, commandId),
            (_, _) => Task.FromResult(Interlocked.Increment(ref calls)),
            requestA)).Value.ShouldBe(1);
        var conflict = await CommandExecutor.ExecuteAsync(
            Command.Create(CommandExecutionTestConstants.PaymentCapture, commandId),
            (_, _) => Task.FromResult(Interlocked.Increment(ref calls)),
            requestB);

        conflict.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.Conflict);
        calls.ShouldBe(1);
    }

    [Test]
    public async Task ExecuteResultAsync_WithAttemptTimeout_RetriesCooperativeHandler()
    {
        var attempts = 0;
        var runtime = CreateRuntime(options =>
        {
            options.Timeout.Enabled = true;
            options.Timeout.AttemptTimeout = TimeSpan.FromMilliseconds(20);
            options.Retry.Enabled = true;
            options.Retry.MaxRetries = 1;
        });

        var result = await CommandExecutor.ExecuteResultAsync(
            Command.Create(CommandExecutionTestConstants.AttemptTimeout),
            async (_, token) =>
            {
                if (Interlocked.Increment(ref attempts) == 1)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), token);
                }

                return Result.Succeed();
            },
            runtime);

        result.IsSuccess.ShouldBeTrue();
        attempts.ShouldBe(2);
    }

    [Test]
    public async Task ExecuteResultAsync_WithJsonRetryAfter_UsesTypedAuthoritativeDelay()
    {
        var observed = TimeSpan.MinValue;
        var attempts = 0;
        using var document = JsonDocument.Parse(CommandExecutionTestConstants.ZeroJson);
        var runtime = CreateRuntime(options =>
        {
            options.Retry.Enabled = true;
            options.Retry.MaxRetries = 1;
            options.Retry.Delay = TimeSpan.FromSeconds(1);
            options.Retry.OnRetry = (retryEvent, _) =>
            {
                observed = retryEvent.Delay;
                return ValueTask.CompletedTask;
            };
        });

        var result = await CommandExecutor.ExecuteResultAsync(
            Command.Create(CommandExecutionTestConstants.JsonRetryAfter),
            (_, _) =>
            {
                if (Interlocked.Increment(ref attempts) > 1)
                {
                    return Task.FromResult(Result.Succeed());
                }

                var problem = Problem.Create(HttpStatusCode.TooManyRequests);
                problem.Extensions[ProblemConstants.ExtensionKeys.RetryAfter] = document.RootElement.Clone();
                return Task.FromResult(Result.Fail(problem));
            },
            runtime);

        result.IsSuccess.ShouldBeTrue();
        observed.ShouldBe(TimeSpan.Zero);
    }

    [Test]
    public async Task ExecuteResultAsync_WhenRetryAfterExceedsSafetyMaximum_DoesNotRetryEarly()
    {
        var attempts = 0;
        var runtime = CreateRuntime(options =>
        {
            options.Retry.Enabled = true;
            options.Retry.MaxRetries = 3;
            options.Retry.MaxRetryAfter = TimeSpan.FromMilliseconds(10);
        });

        var result = await CommandExecutor.ExecuteResultAsync(
            Command.Create(CommandExecutionTestConstants.RetryAfterMaximum),
            (_, _) =>
            {
                Interlocked.Increment(ref attempts);
                var problem = Problem.Create(HttpStatusCode.TooManyRequests);
                problem.Extensions[ProblemConstants.ExtensionKeys.RetryAfter] = TimeSpan.FromMinutes(1);
                return Task.FromResult(Result.Fail(problem));
            },
            runtime);

        attempts.ShouldBe(1);
        result.Problem!.Extensions[ProblemConstants.ExtensionKeys.RetryAfterExceedsMaximum].ShouldBe(true);
    }

    [Test]
    public async Task ExecuteResultAsync_WithJitter_CapsFinalDelayAfterRandomization()
    {
        var observed = TimeSpan.Zero;
        var attempts = 0;
        var runtime = CreateRuntime(options =>
        {
            options.Retry.Enabled = true;
            options.Retry.MaxRetries = 1;
            options.Retry.Delay = TimeSpan.FromSeconds(1);
            options.Retry.MaxDelay = TimeSpan.FromMilliseconds(5);
            options.Retry.UseJitter = true;
            options.Retry.Randomizer = static () => 1D;
            options.Retry.OnRetry = (retryEvent, _) =>
            {
                observed = retryEvent.Delay;
                return ValueTask.CompletedTask;
            };
        });

        var result = await CommandExecutor.ExecuteResultAsync(
            Command.Create(CommandExecutionTestConstants.JitterCap),
            (_, _) => Task.FromResult(Interlocked.Increment(ref attempts) == 1
                ? Result.Fail(Problem.Create(HttpStatusCode.ServiceUnavailable))
                : Result.Succeed()),
            runtime);

        result.IsSuccess.ShouldBeTrue();
        observed.ShouldBeLessThanOrEqualTo(TimeSpan.FromMilliseconds(5));
    }

    [Test]
    public async Task ExecuteAsync_WithCooperativeTimeout_AcceptsLateSuccessFromNonCooperativeHandler()
    {
        var runtime = CreateRuntime(options =>
        {
            options.Timeout.Enabled = true;
            options.Timeout.AttemptTimeout = TimeSpan.FromMilliseconds(10);
        });

        var result = await CommandExecutor.ExecuteAsync(
            Command.Create(CommandExecutionTestConstants.AttemptNoncooperative),
            static async (_, _) =>
            {
                await Task.Delay(30);
                return 5;
            },
            runtime);

        result.Value.ShouldBe(5);
    }

    [Test]
    public async Task ExecuteAsync_WithInvalidDynamicTotalTimeout_FailsAsInfrastructureWithoutInvokingHandler()
    {
        var invoked = false;
        var runtime = CreateRuntime(options =>
        {
            options.Timeout.Enabled = true;
            options.Timeout.TotalTimeoutGenerator = static _ => TimeSpan.Zero;
        });

        var result = await CommandExecutor.ExecuteAsync(
            Command.Create(CommandExecutionTestConstants.TimeoutInvalid),
            (_, _) =>
            {
                invoked = true;
                return Task.FromResult(1);
            },
            runtime);

        result.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
        invoked.ShouldBeFalse();
    }

    [Test]
    public async Task ExecuteResultAsync_WhenRetryCallbackThrows_PreservesRetryAndFinalSuccess()
    {
        var attempts = 0;
        var runtime = CreateRuntime(options =>
        {
            options.Retry.Enabled = true;
            options.Retry.MaxRetries = 1;
            options.Retry.OnRetry = static (_, _) =>
                ValueTask.FromException(new InvalidOperationException(CommandExecutionTestConstants.ObserverFailed));
        });

        var result = await CommandExecutor.ExecuteResultAsync(
            Command.Create(CommandExecutionTestConstants.RetryCallback),
            (_, _) => Task.FromResult(Interlocked.Increment(ref attempts) == 1
                ? Result.Fail(Problem.Create(HttpStatusCode.ServiceUnavailable))
                : Result.Succeed()),
            runtime);

        result.IsSuccess.ShouldBeTrue();
        attempts.ShouldBe(2);
    }

    [Test]
    public async Task CircuitBreaker_WhenThresholdReached_RejectsThenClosesAfterSuccessfulProbe()
    {
        var runtime = CreateRuntime(options =>
        {
            options.CircuitBreaker.Enabled = true;
            options.CircuitBreaker.MinimumThroughput = 2;
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromMilliseconds(30);
            options.CircuitBreaker.PartitionKeySelector = static command => command.CommandType;
        });
        var handlerCalls = 0;
        var command = Command.Create(CommandExecutionTestConstants.DependencyCall);

        for (var index = 0; index < 2; index++)
        {
            await CommandExecutor.ExecuteResultAsync(
                command,
                (_, _) =>
                {
                    Interlocked.Increment(ref handlerCalls);
                    return Task.FromResult(Result.Fail(Problem.Create(HttpStatusCode.ServiceUnavailable)));
                },
                runtime);
        }

        var rejected = await CommandExecutor.ExecuteResultAsync(
            command,
            (_, _) =>
            {
                Interlocked.Increment(ref handlerCalls);
                return Task.FromResult(Result.Succeed());
            },
            runtime);
        rejected.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.ServiceUnavailable);
        handlerCalls.ShouldBe(2);

        await Task.Delay(40);
        var probe = await CommandExecutor.ExecuteResultAsync(
            command,
            (_, _) =>
            {
                Interlocked.Increment(ref handlerCalls);
                return Task.FromResult(Result.Succeed());
            },
            runtime);

        probe.IsSuccess.ShouldBeTrue();
        handlerCalls.ShouldBe(3);
        ((ICommandCircuitBreakerStateProvider)runtime.CircuitBreaker!).GetState(
                CommandExecutionTestConstants.DependencyCall)
            .ShouldBe(CommandCircuitState.Closed);
    }

    [Test]
    public async Task CircuitBreaker_WhenStaleClosedAttemptFinishesDuringHalfOpen_DoesNotReplaceProbe()
    {
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            MinimumThroughput = 1,
            FailureRatio = 1D,
            BreakDuration = TimeSpan.FromMilliseconds(20),
            PartitionKeySelector = static command => command.CommandType
        };
        var breaker = new PartitionedCommandCircuitBreaker(options);
        var command = Command.Create(CommandExecutionTestConstants.DependencyCall);
        var staleClosedLease = await breaker.AcquireAsync(command);
        var openingLease = await breaker.AcquireAsync(command);

        await breaker.RecordAsync(
            command,
            openingLease,
            Result.Fail(Problem.Create(HttpStatusCode.ServiceUnavailable)));
        await Task.Delay(30);
        var probeLease = await breaker.AcquireAsync(command);

        await breaker.RecordAsync(command, staleClosedLease, Result.Succeed());

        var state = (ICommandCircuitBreakerStateProvider)breaker;
        state.GetState(CommandExecutionTestConstants.DependencyCall).ShouldBe(CommandCircuitState.HalfOpen);

        await breaker.RecordAsync(command, probeLease, Result.Succeed());
        state.GetState(CommandExecutionTestConstants.DependencyCall).ShouldBe(CommandCircuitState.Closed);

        await breaker.RecordAsync(
            command,
            probeLease,
            Result.Fail(Problem.Create(HttpStatusCode.ServiceUnavailable)));
        state.GetState(CommandExecutionTestConstants.DependencyCall).ShouldBe(CommandCircuitState.Closed);
    }

    [Test]
    public async Task CircuitBreaker_ManualIsolationAndReset_ControlAdmission()
    {
        var options = new CommandExecutionOptions();
        options.Timeout.Enabled = false;
        options.Idempotency.Enabled = false;
        options.CircuitBreaker.Enabled = true;
        options.CircuitBreaker.PartitionKeySelector = static command => command.CommandType;
        var runtime = new CommandExecutionRuntime(options);
        var state = (ICommandCircuitBreakerStateProvider)runtime.CircuitBreaker!;
        await state.IsolateAsync(CommandExecutionTestConstants.ManualPartition);
        var calls = 0;

        var rejected = await CommandExecutor.ExecuteAsync(
            Command.Create(CommandExecutionTestConstants.ManualPartition),
            (_, _) => Task.FromResult(Interlocked.Increment(ref calls)),
            runtime);
        await state.ResetAsync(CommandExecutionTestConstants.ManualPartition);
        var accepted = await CommandExecutor.ExecuteAsync(
            Command.Create(CommandExecutionTestConstants.ManualPartition),
            (_, _) => Task.FromResult(Interlocked.Increment(ref calls)),
            runtime);

        rejected.IsFailed.ShouldBeTrue();
        accepted.Value.ShouldBe(1);
        calls.ShouldBe(1);
    }

    [Test]
    public async Task ExecuteAsync_WhenCommandLifetimeExpired_SkipsHandler()
    {
        var command = Command.Create(CommandExecutionTestConstants.ExpiredCommand);
        command.Timestamp = DateTime.UtcNow.AddMinutes(-2);
        command.Metadata = new CommandMetadata { TimeToLiveSeconds = 30 };
        var invoked = false;

        var result = await CommandExecutor.ExecuteAsync(
            command,
            (_, _) =>
            {
                invoked = true;
                return Task.FromResult(1);
            },
            CreateRuntime());

        result.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.Gone);
        invoked.ShouldBeFalse();
    }

    [Test]
    public async Task PartitionedRateLimiter_WhenQueuedCommandTimesOut_RemovesWaiterAndDoesNotLeakPermit()
    {
        var partitioned = PartitionedRateLimiter.Create<ICommand, string>(command =>
            RateLimitPartition.GetConcurrencyLimiter(
                command.CommandType,
                static _ => new ConcurrencyLimiterOptions
                {
                    PermitLimit = 1,
                    QueueLimit = 1,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }));
        await using var limiter = new PartitionedCommandRateLimiter(partitioned);
        var ownerRuntime = CreateRuntime(rateLimiter: limiter);
        var queuedRuntime = CreateRuntime(
            options =>
            {
                options.Timeout.Enabled = true;
                options.Timeout.TotalTimeout = TimeSpan.FromMilliseconds(30);
            },
            rateLimiter: limiter);
        var ownerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseOwner = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var queuedHandlerInvoked = false;

        var owner = CommandExecutor.ExecuteAsync(
            Command.Create("archive.export"),
            async (_, token) =>
            {
                ownerStarted.SetResult();
                await releaseOwner.Task.WaitAsync(token);
                return 1;
            },
            ownerRuntime);
        await ownerStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var timedOut = await CommandExecutor.ExecuteAsync(
            Command.Create("archive.export"),
            (_, _) =>
            {
                queuedHandlerInvoked = true;
                return Task.FromResult(2);
            },
            queuedRuntime);

        timedOut.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.RequestTimeout);
        queuedHandlerInvoked.ShouldBeFalse();

        releaseOwner.SetResult();
        (await owner.WaitAsync(TimeSpan.FromSeconds(5))).Value.ShouldBe(1);

        var next = await CommandExecutor.ExecuteAsync(
            Command.Create("archive.export"),
            static (_, _) => Task.FromResult(3),
            ownerRuntime);
        next.Value.ShouldBe(3);
    }

    [Test]
    public async Task PartitionedRateLimiter_WhenSecondCommandQueues_ReleasesPermitAndRunsBothCommands()
    {
        var partitioned = PartitionedRateLimiter.Create<ICommand, string>(command =>
            RateLimitPartition.GetConcurrencyLimiter(
                command.CommandType,
                static _ => new ConcurrencyLimiterOptions
                {
                    PermitLimit = 1,
                    QueueLimit = 1,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }));
        await using var limiter = new PartitionedCommandRateLimiter(partitioned);
        var queued = 0;
        var runtime = CreateRuntime(
            options => options.RateLimiter.OnQueued = (_, _) =>
            {
                Interlocked.Increment(ref queued);
                return ValueTask.CompletedTask;
            },
            rateLimiter: limiter);
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var first = CommandExecutor.ExecuteAsync(
            Command.Create("document.render"),
            async (_, token) =>
            {
                firstStarted.SetResult();
                await releaseFirst.Task.WaitAsync(token);
                return 1;
            },
            runtime);
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var second = CommandExecutor.ExecuteAsync(
            Command.Create("document.render"),
            (_, _) =>
            {
                secondStarted.SetResult();
                return Task.FromResult(2);
            },
            runtime);
        await Task.Delay(30);
        secondStarted.Task.IsCompleted.ShouldBeFalse();

        releaseFirst.SetResult();
        var results = await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));

        results[0].Value.ShouldBe(1);
        results[1].Value.ShouldBe(2);
        queued.ShouldBe(1);

        var third = await CommandExecutor.ExecuteAsync(
            Command.Create("document.render"),
            static (_, _) => Task.FromResult(3),
            runtime);
        third.Value.ShouldBe(3);
    }

    [Test]
    public async Task PartitionedRateLimiter_ConcurrencyFactoryHonorsCommandPermitCost()
    {
        await using var limiter = PartitionedCommandRateLimiter.CreateConcurrency(
            static _ => CommandExecutionTestConstants.TenantA,
            permitLimit: 2,
            permitCountSelector: static _ => 2);

        var first = await limiter.AcquireAsync(Command.Create(CommandExecutionTestConstants.BatchLarge));
        await using var rejected = await limiter.AcquireAsync(Command.Create(CommandExecutionTestConstants.BatchLarge));
        first.IsAcquired.ShouldBeTrue();
        rejected.IsAcquired.ShouldBeFalse();

        await first.DisposeAsync();
        await using var afterRelease = await limiter.AcquireAsync(
            Command.Create(CommandExecutionTestConstants.BatchLarge));
        afterRelease.IsAcquired.ShouldBeTrue();
    }

    private static MemoryCacheCommandIdempotencyStore CreateStore(IMemoryCache cache)
    {
        return new MemoryCacheCommandIdempotencyStore(
            cache,
            NullLogger<MemoryCacheCommandIdempotencyStore>.Instance);
    }

    private static CommandExecutionRuntime CreateRuntime(
        Action<CommandExecutionOptions>? configure = null,
        ICommandIdempotencyStore? idempotencyStore = null,
        ICommandRateLimiter? rateLimiter = null)
    {
        var options = new CommandExecutionOptions();
        options.Timeout.Enabled = false;
        options.Retry.Delay = TimeSpan.Zero;
        options.Retry.UseJitter = false;
        options.Idempotency.ScopeSelector = static _ => CommandExecutionTestConstants.TestsScope;
        options.Idempotency.FingerprintSelector = static _ => CommandExecutionTestConstants.RequestV1;
        configure?.Invoke(options);
        return new CommandExecutionRuntime(options, idempotencyStore, rateLimiter);
    }

    private sealed class SequenceRateLimiter(params CommandRateLimitLease[] leases) : ICommandRateLimiter
    {
        private readonly Queue<CommandRateLimitLease> _leases = new(leases);

        public int Acquisitions { get; private set; }

        public ValueTask<CommandRateLimitLease> AcquireAsync(
            ICommand command,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Acquisitions++;
            return ValueTask.FromResult(_leases.Dequeue());
        }
    }
}
