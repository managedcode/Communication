using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Commands.Extensions;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.AspNetCore.Extensions;

public class CommandIdempotencyExtensionsTests
{
    [Fact]
    public async Task ExecuteIdempotentAsync_ReturnsCompletedResultWithoutExecuting()
    {
        var store = new TestCommandIdempotencyStoreSimulator();
        store.SetStatus("cmd-success", CommandExecutionStatus.Completed);
        store.SetResult("cmd-success", "cached");

        var calls = 0;
        var result = await ManagedCode.Communication.Commands.Extensions.CommandIdempotencyExtensions.ExecuteIdempotentAsync(
            store,
            "cmd-success",
            () =>
        {
            calls++;
            return Task.FromResult("fresh-result");
        });

        calls.ShouldBe(0);
        result.ShouldBe("cached");
        store.SetCommandResultCalls.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteIdempotentAsync_ExecutesWhenNotFoundAndPersistsResult()
    {
        var store = new TestCommandIdempotencyStoreSimulator();

        var calls = 0;
        var result = await ManagedCode.Communication.Commands.Extensions.CommandIdempotencyExtensions.ExecuteIdempotentAsync(
            store,
            "cmd-run",
            async () =>
        {
            calls++;
            await Task.Delay(1);
            return "executed";
        });

        result.ShouldBe("executed");
        calls.ShouldBe(1);
        store.GetStatus("cmd-run").ShouldBe(CommandExecutionStatus.Completed);
        ((string?)store.GetResult("cmd-run")).ShouldBe("executed");
    }

    [Fact]
    public async Task ExecuteIdempotentAsync_WhenExecutionFails_MarksFailedAndRethrows()
    {
        var store = new TestCommandIdempotencyStoreSimulator();

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await ManagedCode.Communication.Commands.Extensions.CommandIdempotencyExtensions.ExecuteIdempotentAsync(
                store,
                "cmd-fail",
                () =>
            {
                return Task.FromException<string>(new InvalidOperationException("boom"));
            }));

        store.GetStatus("cmd-fail").ShouldBe(CommandExecutionStatus.Failed);
    }

    [Fact]
    public async Task ExecuteIdempotentAsync_WhenInProgressWaitsUntilCompleted()
    {
        var store = new TestCommandIdempotencyStoreSimulator();
        store.SetStatusSequence("cmd-wait", CommandExecutionStatus.InProgress, CommandExecutionStatus.Completed);
        store.SetResult("cmd-wait", "finished");

        var result = await ManagedCode.Communication.Commands.Extensions.CommandIdempotencyExtensions.ExecuteIdempotentAsync<string>(
            store,
            "cmd-wait",
            () =>
        {
            throw new Exception("should-not-run");
        });

        result.ShouldBe("finished");
        store.GetStatusCallCount("cmd-wait").ShouldBe(2);
        store.GetCommandResultCalls.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteIdempotentWithRetryAsync_RetriesAfterFailureAndReturnsResult()
    {
        var store = new TestCommandIdempotencyStoreSimulator();

        var attempts = 0;
        var result = await ManagedCode.Communication.Commands.Extensions.CommandIdempotencyExtensions.ExecuteIdempotentWithRetryAsync(
            store,
            "cmd-retry",
            () =>
            {
                attempts++;

                if (attempts == 1)
                {
                    return Task.FromException<string>(new InvalidOperationException("temporary"));
                }

                return Task.FromResult("success");
            },
            maxRetries: 1,
            baseDelay: TimeSpan.FromMilliseconds(1));

        attempts.ShouldBe(2);
        result.ShouldBe("success");
        store.GetStatus("cmd-retry").ShouldBe(CommandExecutionStatus.Completed);
    }

    [Fact]
    public async Task ExecuteBatchIdempotentAsync_ReturnsCachedResultsAndExecutesPending()
    {
        var store = new TestCommandIdempotencyStoreSimulator();
        store.SetStatus("cmd-cached", CommandExecutionStatus.Completed);
        store.SetResult("cmd-cached", "cached");

        var operations = new List<(string CommandId, Func<Task<string>> Operation)>
        {
            ("cmd-cached", () => Task.FromResult("wrong")),
            ("cmd-exec", () => Task.FromResult("executed"))
        };

        var results = await ManagedCode.Communication.Commands.Extensions.CommandIdempotencyExtensions.ExecuteBatchIdempotentAsync<string>(
            store,
            operations);

        results.Count.ShouldBe(2);
        results["cmd-cached"].ShouldBe("cached");
        results["cmd-exec"].ShouldBe("executed");
        store.SetCommandResultCalls.ShouldBe(1);
        store.GetCommandResultCalls.ShouldBe(0);
    }

    [Fact]
    public async Task TryGetCachedResultAsync_WhenCompleted_ReturnsValue()
    {
        var store = new TestCommandIdempotencyStoreSimulator();
        store.SetStatus("cmd-cached-2", CommandExecutionStatus.Completed);
        store.SetResult("cmd-cached-2", "done");

        var (hasResult, result) = await ManagedCode.Communication.Commands.Extensions.CommandIdempotencyExtensions.TryGetCachedResultAsync<string>(
            store,
            "cmd-cached-2");

        hasResult.ShouldBeTrue();
        result.ShouldBe("done");
    }

    [Fact]
    public async Task TryGetCachedResultAsync_WhenNotCompleted_ReturnsEmpty()
    {
        var store = new TestCommandIdempotencyStoreSimulator();

        var (hasResult, result) = await ManagedCode.Communication.Commands.Extensions.CommandIdempotencyExtensions.TryGetCachedResultAsync<string>(
            store,
            "cmd-none");

        hasResult.ShouldBeFalse();
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_ReturnsResultWhenWithinTimeout()
    {
        var store = new TestCommandIdempotencyStoreSimulator();

        var result = await ManagedCode.Communication.Commands.Extensions.CommandIdempotencyExtensions.ExecuteWithTimeoutAsync(
            store,
            "cmd-timeout",
            async () =>
            {
                await Task.Delay(5);
                return "quick";
            },
            TimeSpan.FromSeconds(1));

        result.ShouldBe("quick");
    }

    [Fact]
    public async Task ExecuteBatchIdempotentAsync_AllCompletedResults_DoNotExecutePendingOperations()
    {
        var store = new TestCommandIdempotencyStoreSimulator();
        store.SetStatus("cmd-one", CommandExecutionStatus.Completed);
        store.SetStatus("cmd-two", CommandExecutionStatus.Completed);
        store.SetResult("cmd-one", "cached-one");
        store.SetResult("cmd-two", "cached-two");

        var executed = 0;
        var operations = new List<(string CommandId, Func<Task<string>> Operation)>
        {
            ("cmd-one", () =>
            {
                executed++;
                return Task.FromResult("should-not-run-one");
            }),
            ("cmd-two", () =>
            {
                executed++;
                return Task.FromResult("should-not-run-two");
            })
        };

        var results = await ManagedCode.Communication.Commands.Extensions.CommandIdempotencyExtensions.ExecuteBatchIdempotentAsync<string>(
            store,
            operations);

        results.Count.ShouldBe(2);
        results["cmd-one"].ShouldBe("cached-one");
        results["cmd-two"].ShouldBe("cached-two");
        executed.ShouldBe(0);
        store.SetCommandResultCalls.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteIdempotentAsync_WhenWaitForCompletionHitsFailed_Throws()
    {
        var store = new TestCommandIdempotencyStoreSimulator();
        store.SetStatusSequence("cmd-wait-failed", CommandExecutionStatus.InProgress, CommandExecutionStatus.Failed);

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await ManagedCode.Communication.Commands.Extensions.CommandIdempotencyExtensions.ExecuteIdempotentAsync<string>(
                store,
                "cmd-wait-failed",
                () => Task.FromResult("should-not-run")));
    }

    [Fact]
    public async Task ExecuteIdempotentAsync_WhenWaitForCompletionHitsNotFound_Throws()
    {
        var store = new TestCommandIdempotencyStoreSimulator();
        store.SetStatusSequence("cmd-wait-missing", CommandExecutionStatus.InProgress, CommandExecutionStatus.NotFound);

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await ManagedCode.Communication.Commands.Extensions.CommandIdempotencyExtensions.ExecuteIdempotentAsync<string>(
                store,
                "cmd-wait-missing",
                () => Task.FromResult("should-not-run")));
    }

    [Fact]
    public async Task ExecuteIdempotentWithRetryAsync_WhenMaxRetriesExceeded_ThrowsLastException()
    {
        var store = new TestCommandIdempotencyStoreSimulator();
        var attempts = 0;

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            ManagedCode.Communication.Commands.Extensions.CommandIdempotencyExtensions.ExecuteIdempotentWithRetryAsync(
                store,
                "cmd-retry-exhausted",
                () =>
                {
                    attempts++;
                    return Task.FromException<string>(new InvalidOperationException($"attempt-{attempts}"));
                },
                maxRetries: 1,
                baseDelay: TimeSpan.FromMilliseconds(1)));

        attempts.ShouldBe(2);
        exception.Message.ShouldBe("attempt-2");
    }

    [Fact]
    public async Task ExecuteIdempotentWithRetryAsync_CancelledToken_StopsWithoutExecutingOperation()
    {
        var store = new TestCommandIdempotencyStoreSimulator();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var calls = 0;
        await Should.ThrowAsync<OperationCanceledException>(() =>
            ManagedCode.Communication.Commands.Extensions.CommandIdempotencyExtensions.ExecuteIdempotentWithRetryAsync(
                store,
                "cmd-cancelled",
                () =>
                {
                    calls++;
                    return Task.FromResult("should-not-run");
                },
                maxRetries: 10,
                baseDelay: TimeSpan.FromMilliseconds(1),
                cancellationToken: cancellation.Token));

        calls.ShouldBe(0);
    }

    private sealed class TestCommandIdempotencyStoreSimulator : ICommandIdempotencyStore
    {
        private readonly Dictionary<string, CommandExecutionStatus> _statuses = new();
        private readonly Dictionary<string, object?> _results = new();
        private readonly Dictionary<string, Queue<CommandExecutionStatus>> _statusSequences = new();
        private readonly Dictionary<CommandExecutionStatus, int> _cleanupByStatus = new();

        public int SetCommandResultCalls { get; private set; }
        public int GetCommandResultCalls { get; private set; }

        private readonly Dictionary<string, int> _getStatusCalls = new();

        public int GetStatusCallCount(string commandId)
        {
            return _getStatusCalls.GetValueOrDefault(commandId);
        }

        public CommandExecutionStatus GetStatus(string commandId)
        {
            return _statuses.GetValueOrDefault(commandId, CommandExecutionStatus.NotFound);
        }

        public object? GetResult(string commandId)
        {
            return _results.GetValueOrDefault(commandId);
        }

        public void SetStatus(string commandId, CommandExecutionStatus status)
        {
            _statuses[commandId] = status;
        }

        public void SetStatusSequence(string commandId, params CommandExecutionStatus[] statusSequence)
        {
            _statusSequences[commandId] = new Queue<CommandExecutionStatus>(statusSequence);
        }

        public void SetResult<T>(string commandId, T result)
        {
            _results[commandId] = result;
        }

        public Task<CommandExecutionStatus> GetCommandStatusAsync(string commandId, CancellationToken cancellationToken = default)
        {
            _getStatusCalls[commandId] = GetStatusCallCount(commandId) + 1;

            if (_statusSequences.TryGetValue(commandId, out var statusQueue) && statusQueue.Count > 0)
            {
                return Task.FromResult(statusQueue.Dequeue());
            }

            return Task.FromResult(GetStatus(commandId));
        }

        public Task SetCommandStatusAsync(string commandId, CommandExecutionStatus status, CancellationToken cancellationToken = default)
        {
            SetStatus(commandId, status);
            return Task.CompletedTask;
        }

        public Task<T?> GetCommandResultAsync<T>(string commandId, CancellationToken cancellationToken = default)
        {
            GetCommandResultCalls++;
            if (!_results.TryGetValue(commandId, out var value) || value is null)
            {
                return Task.FromResult(default(T));
            }

            return Task.FromResult((T?)value);
        }

        public Task SetCommandResultAsync<T>(string commandId, T result, CancellationToken cancellationToken = default)
        {
            SetCommandResultCalls++;
            SetResult(commandId, result);
            return Task.CompletedTask;
        }

        public Task RemoveCommandAsync(string commandId, CancellationToken cancellationToken = default)
        {
            _statuses.Remove(commandId);
            _results.Remove(commandId);
            return Task.CompletedTask;
        }

        public Task<bool> TrySetCommandStatusAsync(
            string commandId,
            CommandExecutionStatus expectedStatus,
            CommandExecutionStatus newStatus,
            CancellationToken cancellationToken = default)
        {
            var current = _statuses.GetValueOrDefault(commandId, CommandExecutionStatus.NotFound);

            if (current == expectedStatus)
            {
                SetStatus(commandId, newStatus);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<(CommandExecutionStatus currentStatus, bool wasSet)> GetAndSetStatusAsync(
            string commandId,
            CommandExecutionStatus newStatus,
            CancellationToken cancellationToken = default)
        {
            var currentStatus = _statuses.GetValueOrDefault(commandId, CommandExecutionStatus.NotFound);
            _statuses[commandId] = newStatus;
            return Task.FromResult((currentStatus, true));
        }

        public Task<Dictionary<string, CommandExecutionStatus>> GetMultipleStatusAsync(
            IEnumerable<string> commandIds,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(commandIds.ToDictionary(id => id, GetStatus));
        }

        public Task<Dictionary<string, T?>> GetMultipleResultsAsync<T>(
            IEnumerable<string> commandIds,
            CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<string, T?>();

            foreach (var id in commandIds)
            {
                if (_results.TryGetValue(id, out var value) && value is T cast)
                {
                    result[id] = cast;
                }
                else
                {
                    result[id] = default;
                }
            }

            return Task.FromResult(result);
        }

        public Task<int> CleanupExpiredCommandsAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<int> CleanupCommandsByStatusAsync(
            CommandExecutionStatus status,
            TimeSpan maxAge,
            CancellationToken cancellationToken = default)
        {
            var removed = _cleanupByStatus.TryGetValue(status, out var count)
                ? count
                : 0;

            return Task.FromResult(removed);
        }

        public Task<Dictionary<CommandExecutionStatus, int>> GetCommandCountByStatusAsync(
            CancellationToken cancellationToken = default)
        {
            var grouped = _statuses.GroupBy(pair => pair.Value)
                .ToDictionary(group => group.Key, group => group.Count());

            return Task.FromResult(grouped);
        }
    }
}
