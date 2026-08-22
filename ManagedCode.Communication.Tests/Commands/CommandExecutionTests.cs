using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Commands.Execution;
using ManagedCode.Communication.Commands.Extensions;
using ManagedCode.Communication.Commands.Stores;
using ManagedCode.Communication.Constants;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ManagedCode.Communication.Tests.Commands;

public sealed class CommandExecutionTests
{
    [Test]
    public async Task CommandExecutor_WithTaskValue_WrapsResult()
    {
        var command = Command.Create("payment.capture");

        var result = await CommandExecutor.ExecuteAsync(
            command,
            static (_, _) => Task.FromResult(42),
            CreateRuntime());

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Test]
    public async Task CommandExecutor_WithValueTaskValue_WrapsResult()
    {
        var command = Command.Create("payment.capture");

        var result = await CommandExecutor.ExecuteAsync(
            command,
            static (_, _) => ValueTask.FromResult(42),
            CreateRuntime());

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Test]
    public async Task AddCommandExecution_RegistersDependencyInjectedExecutor()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCommandExecution(options => options.Timeout.Enabled = false);
        await using var serviceProvider = services.BuildServiceProvider();
        var executor = serviceProvider.GetRequiredService<ICommandExecutor>();

        var result = await executor.ExecuteValueAsync(
            Command.Create("payment.capture"),
            static (_, _) => Task.FromResult(42));

        result.Value.ShouldBe(42);
    }

    [Test]
    public async Task ResultExecuteAsync_WithResultHandler_PreservesProblem()
    {
        var command = Command.Create("payment.capture");
        var problem = Problem.Create("declined", "The payment was declined.", HttpStatusCode.Conflict);

        var result = await Result<int>.ExecuteAsync(
            command,
            (_, _) => Task.FromResult(Result<int>.Fail(problem)),
            CreateRuntime());

        result.IsFailed.ShouldBeTrue();
        result.Problem.ShouldBeSameAs(problem);
    }

    [Test]
    public async Task CommandExecutorExecuteAsync_WithResultHandler_DoesNotNestResult()
    {
        var command = Command.Create("payment.capture");
        var problem = Problem.Create("declined", "The payment was declined.", HttpStatusCode.Conflict);

        Result<int> result = await CommandExecutor.ExecuteAsync(
            command,
            (_, _) => Task.FromResult(Result<int>.Fail(problem)),
            CreateRuntime());

        result.Problem.ShouldBeSameAs(problem);
    }

    [Test]
    public async Task ResultExecuteAsync_WithValueTaskResult_PreservesValue()
    {
        var command = Command.Create("payment.capture");

        var result = await Result<int>.ExecuteAsync(
            command,
            static (_, _) => ValueTask.FromResult(Result<int>.Succeed(84)),
            CreateRuntime());

        result.Value.ShouldBe(84);
    }

    [Test]
    public async Task ExecuteResultAsync_WithTransientProblems_RetriesUntilSuccess()
    {
        var attempt = 0;
        var runtime = CreateRuntime(options =>
        {
            options.Retry.Enabled = true;
            options.Retry.MaxRetries = 2;
        });

        var result = await CommandExecutor.ExecuteResultAsync(
            Command.Create("inventory.reserve"),
            (_, _) =>
            {
                attempt++;
                return Task.FromResult(attempt < 3
                    ? Result<int>.Fail(Problem.Create(HttpStatusCode.ServiceUnavailable))
                    : Result<int>.Succeed(7));
            },
            runtime);

        attempt.ShouldBe(3);
        result.Value.ShouldBe(7);
    }

    [Test]
    public async Task ExecuteResultAsync_WithNonRetryableProblem_DoesNotRetry()
    {
        var attempt = 0;
        var runtime = CreateRuntime(options => options.Retry.Enabled = true);

        var result = await CommandExecutor.ExecuteResultAsync(
            Command.Create("inventory.reserve"),
            (_, _) =>
            {
                attempt++;
                return Task.FromResult(Result<int>.FailBadRequest());
            },
            runtime);

        attempt.ShouldBe(1);
        result.IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task ExecuteAsync_WithCallerCancellation_DoesNotRetry()
    {
        var attempt = 0;
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var runtime = CreateRuntime(options => options.Retry.Enabled = true);

        var action = async () => await CommandExecutor.ExecuteAsync(
            Command.Create("inventory.reserve"),
            (_, token) =>
            {
                attempt++;
                return Task.FromCanceled<int>(token);
            },
            runtime,
            cancellationSource.Token);

        await Should.ThrowAsync<OperationCanceledException>(action);
        attempt.ShouldBe(0);
    }

    [Test]
    public async Task ExecuteAsync_WhenTimeoutExpires_ReturnsRequestTimeout()
    {
        var runtime = CreateRuntime(options =>
        {
            options.Timeout.Enabled = true;
            options.Timeout.TotalTimeout = TimeSpan.FromMilliseconds(20);
        });

        var result = await CommandExecutor.ExecuteAsync(
            Command.Create("report.generate"),
            static async (_, token) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return 1;
            },
            runtime);

        result.IsFailed.ShouldBeTrue();
        result.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.RequestTimeout);
    }

    [Test]
    public async Task ExecuteAsync_WithIdempotencyStore_ExecutesHandlerOnce()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var store = new MemoryCacheCommandIdempotencyStore(
            cache,
            NullLogger<MemoryCacheCommandIdempotencyStore>.Instance);
        var runtime = CreateRuntime(idempotencyStore: store);
        var command = Command.Create("email.send");
        var executions = 0;

        var first = await CommandExecutor.ExecuteAsync(
            command,
            (_, _) => Task.FromResult(Interlocked.Increment(ref executions)),
            runtime);
        var second = await CommandExecutor.ExecuteAsync(
            command,
            (_, _) => Task.FromResult(Interlocked.Increment(ref executions)),
            runtime);

        executions.ShouldBe(1);
        first.Value.ShouldBe(1);
        second.Value.ShouldBe(1);
    }

    [Test]
    public async Task ExecuteAsync_AfterTimedOutHandlerStarts_BlocksAutomaticDuplicateAsIndeterminate()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var store = new MemoryCacheCommandIdempotencyStore(
            cache,
            NullLogger<MemoryCacheCommandIdempotencyStore>.Instance);
        var runtime = CreateRuntime(
            options =>
            {
                options.Timeout.Enabled = true;
                options.Timeout.TotalTimeout = TimeSpan.FromSeconds(1);
            },
            store);
        var command = Command.Create("email.send");
        var handlerInvocations = 0;
        var handlerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var timedOutTask = CommandExecutor.ExecuteAsync(
            command,
            async (_, token) =>
            {
                Interlocked.Increment(ref handlerInvocations);
                handlerStarted.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return 1;
            },
            runtime);
        await handlerStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var timedOut = await timedOutTask.WaitAsync(TimeSpan.FromSeconds(5));
        var duplicate = await CommandExecutor.ExecuteAsync(
            command,
            (_, _) => Task.FromResult(Interlocked.Increment(ref handlerInvocations)),
            runtime);

        timedOut.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.RequestTimeout);
        duplicate.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.Conflict);
        duplicate.Problem.Title.ShouldBe(ProblemConstants.CommandExecutionTitles.IndeterminateCommandOutcome);
        handlerInvocations.ShouldBe(1);
    }

    [Test]
    public async Task ExecuteAsync_WhenRateLimiterRejects_ReturnsTooManyRequestsAndSkipsHandler()
    {
        var limiter = new StubRateLimiter(CommandRateLimitLease.Rejected(
            Problem.Create(HttpStatusCode.TooManyRequests)));
        var rejected = 0;
        var runtime = CreateRuntime(
            options => options.RateLimiter.OnRejected = (_, _) =>
            {
                rejected++;
                return ValueTask.CompletedTask;
            },
            rateLimiter: limiter);
        var handlerInvoked = false;

        var result = await CommandExecutor.ExecuteAsync(
            Command.Create("search.execute"),
            (_, _) =>
            {
                handlerInvoked = true;
                return Task.FromResult(1);
            },
            runtime);

        result.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.TooManyRequests);
        handlerInvoked.ShouldBeFalse();
        limiter.Acquisitions.ShouldBe(1);
        rejected.ShouldBe(1);
    }

    [Test]
    public async Task PartitionedRateLimiter_WithExhaustedWindow_RejectsSecondCommand()
    {
        await using var limiter = PartitionedCommandRateLimiter.CreateFixedWindow(
            static command => command.CommandType,
            permitLimit: 1,
            window: TimeSpan.FromMinutes(1));
        var runtime = CreateRuntime(rateLimiter: limiter);

        var first = await CommandExecutor.ExecuteAsync(
            Command.Create("search.execute"),
            static (_, _) => Task.FromResult(1),
            runtime);
        var second = await CommandExecutor.ExecuteAsync(
            Command.Create("search.execute"),
            static (_, _) => Task.FromResult(2),
            runtime);

        first.IsSuccess.ShouldBeTrue();
        second.Problem!.StatusCode.ShouldBe((int)HttpStatusCode.TooManyRequests);
    }

    [Test]
    public async Task ExecuteAsync_WithQueuedLease_ReportsQueueAndDisposesLease()
    {
        var queued = 0;
        var disposed = 0;
        var limiter = new StubRateLimiter(CommandRateLimitLease.Acquired(
            wasQueued: true,
            disposeAsync: () =>
            {
                disposed++;
                return ValueTask.CompletedTask;
            }));
        var runtime = CreateRuntime(
            options => options.RateLimiter.OnQueued = (_, _) =>
            {
                queued++;
                return ValueTask.CompletedTask;
            },
            rateLimiter: limiter);

        var result = await CommandExecutor.ExecuteAsync(
            Command.Create("queued.command"),
            static (_, _) => Task.FromResult(1),
            runtime);

        result.IsSuccess.ShouldBeTrue();
        queued.ShouldBe(1);
        disposed.ShouldBe(1);
    }

    [Test]
    public async Task CommandRateLimitLease_DisposeAsync_ReleasesOnlyOnce()
    {
        var disposed = 0;
        var lease = CommandRateLimitLease.Acquired(disposeAsync: () =>
        {
            disposed++;
            return ValueTask.CompletedTask;
        });

        await lease.DisposeAsync();
        await lease.DisposeAsync();

        disposed.ShouldBe(1);
    }

    [Test]
    public async Task ExecuteResultAsync_WhenRetriesExhausted_ReportsCallbackAndProblemMetadata()
    {
        CommandRetryEvent? exhausted = null;
        var runtime = CreateRuntime(options =>
        {
            options.Retry.Enabled = true;
            options.Retry.MaxRetries = 1;
            options.Retry.OnRetriesExhausted = (retryEvent, _) =>
            {
                exhausted = retryEvent;
                return ValueTask.CompletedTask;
            };
        });

        var result = await CommandExecutor.ExecuteResultAsync(
            Command.Create("always.fail"),
            static (_, _) => Task.FromResult(Result.Fail(Problem.Create(HttpStatusCode.ServiceUnavailable))),
            runtime);

        exhausted.ShouldNotBeNull();
        exhausted!.Attempt.ShouldBe(2);
        result.Problem!.Extensions[ProblemConstants.ExtensionKeys.RetryAttempts].ShouldBe(2);
        result.Problem.Extensions[ProblemConstants.ExtensionKeys.RetriesExhausted].ShouldBe(true);
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

    private sealed class StubRateLimiter(CommandRateLimitLease lease) : ICommandRateLimiter
    {
        public int Acquisitions { get; private set; }

        public ValueTask<CommandRateLimitLease> AcquireAsync(
            ICommand command,
            CancellationToken cancellationToken = default)
        {
            Acquisitions++;
            return ValueTask.FromResult(lease);
        }
    }
}
