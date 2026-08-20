using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Commands.Execution;
using ManagedCode.Communication.Commands.Stores;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ManagedCode.Communication.Tests.Commands;

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
            options.Timeout.Timeout = TimeSpan.FromMilliseconds(30);
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
        result.Problem!.Extensions["retryAttempts"].ShouldBe(2);
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
                metadata: new Dictionary<string, object?> { ["retryAfter"] = retryAfter }),
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
                problem.Extensions["retryAfter"] = retryAfterSeconds;
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
                options.Timeout.Timeout = TimeSpan.FromMilliseconds(30);
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
