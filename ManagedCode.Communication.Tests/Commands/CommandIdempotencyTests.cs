using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Commands.Extensions;
using ManagedCode.Communication.Commands.Stores;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using ManagedCode.Communication.Tests.TestHelpers;

namespace ManagedCode.Communication.Tests.Commands;

public class CommandIdempotencyTests
{
    [Fact]
    public void ServiceCollectionExtensions_AddCommandIdempotency_RegistersMemoryCacheStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCommandIdempotency();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var store = serviceProvider.GetService<ICommandIdempotencyStore>();
        
        store.ShouldBeOfType<MemoryCacheCommandIdempotencyStore>();
    }

    [Fact]
    public void ServiceCollectionExtensions_AddCommandIdempotency_WithCustomType_RegistersCustomStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();

        // Act
        services.AddCommandIdempotency<TestCommandIdempotencyStore>();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var store = serviceProvider.GetService<ICommandIdempotencyStore>();
        
        store.ShouldBeOfType<TestCommandIdempotencyStore>();
    }

    [Fact]
    public void ServiceCollectionExtensions_AddCommandIdempotency_WithInstance_RegistersInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        var customStore = new TestCommandIdempotencyStore();

        // Act
        services.AddCommandIdempotency(customStore);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var store = serviceProvider.GetService<ICommandIdempotencyStore>();
        
        store.ShouldBeSameAs(customStore);
    }
}

public class MemoryCacheCommandIdempotencyStoreTests : IDisposable
{
    private readonly MemoryCacheCommandIdempotencyStore _store;
    private readonly IMemoryCache _memoryCache;

    public MemoryCacheCommandIdempotencyStoreTests()
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();
        
        _memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
        var logger = serviceProvider.GetRequiredService<ILogger<MemoryCacheCommandIdempotencyStore>>();
        _store = new MemoryCacheCommandIdempotencyStore(_memoryCache, logger);
    }

    [Fact]
    public async Task GetCommandStatusAsync_NewCommand_ReturnsNotFound()
    {
        // Act
        var status = await _store.GetCommandStatusAsync("test-command-1");

        // Assert
        status.ShouldBe(CommandExecutionStatus.NotFound);
    }

    [Fact]
    public async Task SetCommandStatusAsync_SetsStatus()
    {
        // Arrange
        const string commandId = "test-command-2";

        // Act
        await _store.SetCommandStatusAsync(commandId, CommandExecutionStatus.InProgress);
        var status = await _store.GetCommandStatusAsync(commandId);

        // Assert
        status.ShouldBe(CommandExecutionStatus.InProgress);
    }

    [Fact]
    public async Task SetCommandResultAsync_StoresResult()
    {
        // Arrange
        const string commandId = "test-command-3";
        const string expectedResult = "test-result";

        // Act
        await _store.SetCommandResultAsync(commandId, expectedResult);
        var result = await _store.GetCommandResultAsync<string>(commandId);

        // Assert
        result.ShouldBe(expectedResult);
    }

    [Fact]
    public async Task RemoveCommandAsync_RemovesCommand()
    {
        // Arrange
        const string commandId = "test-command-4";
        await _store.SetCommandStatusAsync(commandId, CommandExecutionStatus.Completed);
        await _store.SetCommandResultAsync(commandId, "result");

        // Act
        await _store.RemoveCommandAsync(commandId);

        // Assert
        var status = await _store.GetCommandStatusAsync(commandId);
        var result = await _store.GetCommandResultAsync<string>(commandId);
        
        status.ShouldBe(CommandExecutionStatus.NotFound);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task TrySetCommandStatusAsync_WhenExpectedMatches_SetsStatusAndReturnsTrue()
    {
        // Arrange
        const string commandId = "test-command-5";
        await _store.SetCommandStatusAsync(commandId, CommandExecutionStatus.InProgress);

        // Act
        var result = await _store.TrySetCommandStatusAsync(commandId, CommandExecutionStatus.InProgress, CommandExecutionStatus.Completed);

        // Assert
        result.ShouldBeTrue();
        var status = await _store.GetCommandStatusAsync(commandId);
        status.ShouldBe(CommandExecutionStatus.Completed);
    }

    [Fact]
    public async Task TrySetCommandStatusAsync_WhenExpectedDoesNotMatch_DoesNotSetStatusAndReturnsFalse()
    {
        // Arrange
        const string commandId = "test-command-6";
        await _store.SetCommandStatusAsync(commandId, CommandExecutionStatus.InProgress);

        // Act
        var result = await _store.TrySetCommandStatusAsync(commandId, CommandExecutionStatus.Completed, CommandExecutionStatus.Failed);

        // Assert
        result.ShouldBeFalse();
        var status = await _store.GetCommandStatusAsync(commandId);
        status.ShouldBe(CommandExecutionStatus.InProgress); // Unchanged
    }

    [Fact]
    public async Task GetAndSetStatusAsync_ReturnsCurrentStatusAndSetsNew()
    {
        // Arrange
        const string commandId = "test-command-7";
        await _store.SetCommandStatusAsync(commandId, CommandExecutionStatus.InProgress);

        // Act
        var (currentStatus, wasSet) = await _store.GetAndSetStatusAsync(commandId, CommandExecutionStatus.Completed);

        // Assert
        currentStatus.ShouldBe(CommandExecutionStatus.InProgress);
        wasSet.ShouldBeTrue();
        
        var newStatus = await _store.GetCommandStatusAsync(commandId);
        newStatus.ShouldBe(CommandExecutionStatus.Completed);
    }

    [Fact]
    public async Task GetMultipleStatusAsync_ReturnsStatusForMultipleCommands()
    {
        // Arrange
        await _store.SetCommandStatusAsync("cmd1", CommandExecutionStatus.Completed);
        await _store.SetCommandStatusAsync("cmd2", CommandExecutionStatus.InProgress);
        
        var commandIds = new[] { "cmd1", "cmd2", "cmd3" };

        // Act
        var statuses = await _store.GetMultipleStatusAsync(commandIds);

        // Assert
        statuses.ShouldHaveCount(3);
        statuses["cmd1"].ShouldBe(CommandExecutionStatus.Completed);
        statuses["cmd2"].ShouldBe(CommandExecutionStatus.InProgress);
        statuses["cmd3"].ShouldBe(CommandExecutionStatus.NotFound);
    }

    [Fact]
    public async Task GetMultipleResultsAsync_ReturnsResultsForMultipleCommands()
    {
        // Arrange
        await _store.SetCommandResultAsync("cmd1", "result1");
        await _store.SetCommandResultAsync("cmd2", "result2");
        
        var commandIds = new[] { "cmd1", "cmd2", "cmd3" };

        // Act
        var results = await _store.GetMultipleResultsAsync<string>(commandIds);

        // Assert
        results.ShouldHaveCount(3);
        results["cmd1"].ShouldBe("result1");
        results["cmd2"].ShouldBe("result2");
        results["cmd3"].ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteIdempotentAsync_FirstExecution_ExecutesOperationAndStoresResult()
    {
        // Arrange
        const string commandId = "test-command-execute-1";
        var executionCount = 0;
        const string expectedResult = "operation-result";

        // Act
        var result = await _store.ExecuteIdempotentAsync(commandId, async () =>
        {
            executionCount++;
            await Task.Delay(10); // Simulate async work
            return expectedResult;
        });

        // Assert
        result.ShouldBe(expectedResult);
        executionCount.ShouldBe(1);
        
        var status = await _store.GetCommandStatusAsync(commandId);
        status.ShouldBe(CommandExecutionStatus.Completed);
        
        var storedResult = await _store.GetCommandResultAsync<string>(commandId);
        storedResult.ShouldBe(expectedResult);
    }

    [Fact]
    public async Task ExecuteIdempotentAsync_SecondExecution_ReturnsStoredResultWithoutReexecuting()
    {
        // Arrange
        const string commandId = "test-command-execute-2";
        var executionCount = 0;
        const string expectedResult = "operation-result";

        var operation = async () =>
        {
            executionCount++;
            await Task.Delay(10);
            return expectedResult;
        };

        // First execution
        await _store.ExecuteIdempotentAsync(commandId, operation);

        // Act - Second execution
        var result = await _store.ExecuteIdempotentAsync(commandId, operation);

        // Assert
        result.ShouldBe(expectedResult);
        executionCount.ShouldBe(1); // Should not execute second time
    }

    [Fact]
    public async Task ExecuteIdempotentAsync_WhenOperationReturnsNull_CachesNullResult()
    {
        // Arrange
        const string commandId = "test-command-null-result";
        var executionCount = 0;

        var operation = new Func<Task<string?>>(() =>
        {
            executionCount++;
            return Task.FromResult<string?>(null);
        });

        // Act
        var first = await _store.ExecuteIdempotentAsync(commandId, operation);
        var second = await _store.ExecuteIdempotentAsync(commandId, operation);

        // Assert
        first.ShouldBeNull();
        second.ShouldBeNull();
        executionCount.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteIdempotentAsync_WhenOperationReturnsDefaultStructValue_CachesResult()
    {
        // Arrange
        const string commandId = "test-command-default-struct";
        var executionCount = 0;

        var operation = new Func<Task<int>>(() =>
        {
            executionCount++;
            return Task.FromResult(0);
        });

        // Act
        var first = await _store.ExecuteIdempotentAsync(commandId, operation);
        var second = await _store.ExecuteIdempotentAsync(commandId, operation);

        // Assert
        first.ShouldBe(0);
        second.ShouldBe(0);
        executionCount.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteIdempotentAsync_WhenConcurrentCallersShareCommand_WaitsForSingleExecution()
    {
        // Arrange
        const string commandId = "concurrent-single-execution";
        var operationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var operationCompletion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondaryOperationInvoked = false;
        var executionCount = 0;

        var firstCall = _store.ExecuteIdempotentAsync(commandId, async () =>
        {
            Interlocked.Increment(ref executionCount);
            operationStarted.TrySetResult();
            return await operationCompletion.Task;
        });

        await operationStarted.Task; // Ensure the first invocation has claimed execution

        var secondCall = _store.ExecuteIdempotentAsync(commandId, () =>
        {
            secondaryOperationInvoked = true;
            return Task.FromResult("should-not-run");
        });

        operationCompletion.TrySetResult("shared-result");

        var results = await Task.WhenAll(firstCall, secondCall);

        executionCount.ShouldBe(1);
        secondaryOperationInvoked.ShouldBeFalse();
        results.ShouldBe(new[] { "shared-result", "shared-result" });
    }

    [Fact]
    public async Task ExecuteIdempotentAsync_WhenPrimaryExecutionFails_ConcurrentCallerReceivesFailure()
    {
        // Arrange
        const string commandId = "concurrent-failure";
        var startSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var failingCall = _store.ExecuteIdempotentAsync<string>(commandId, async () =>
        {
            startSignal.TrySetResult();
            await Task.Delay(20);
            throw new InvalidOperationException("boom");
        });

        await startSignal.Task;

        var waitingCall = _store.ExecuteIdempotentAsync(commandId, () => Task.FromResult("should-not-run"));

        var waitingException = await Should.ThrowAsync<InvalidOperationException>(() => waitingCall);
        waitingException.Message.ShouldBe("Command concurrent-failure failed during execution");

        var primaryException = await Should.ThrowAsync<InvalidOperationException>(() => failingCall);
        primaryException.Message.ShouldBe("boom");

        var status = await _store.GetCommandStatusAsync(commandId);
        status.ShouldBe(CommandExecutionStatus.Failed);
    }

    [Fact]
    public async Task ExecuteIdempotentAsync_WithDifferentCommandIds_DoesNotSerializeExecution()
    {
        // Arrange
        const string commandId1 = "parallel-command-1";
        const string commandId2 = "parallel-command-2";

        // Act
        var stopwatch = Stopwatch.StartNew();

        var task1 = _store.ExecuteIdempotentAsync(commandId1, async () =>
        {
            await Task.Delay(150);
            return "result-1";
        });

        var task2 = _store.ExecuteIdempotentAsync(commandId2, async () =>
        {
            await Task.Delay(150);
            return "result-2";
        });

        await Task.WhenAll(task1, task2);
        stopwatch.Stop();

        // Assert
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromMilliseconds(260));
        (await task1).ShouldBe("result-1");
        (await task2).ShouldBe("result-2");
    }

    [Fact]
    public async Task ExecuteIdempotentAsync_WhenOperationFails_MarksCommandAsFailedAndRethrowsException()
    {
        // Arrange
        const string commandId = "test-command-fail-1";
        var expectedException = new InvalidOperationException("Test exception");

        // Act & Assert
        var act = async () => await _store.ExecuteIdempotentAsync<string>(commandId, async () =>
        {
            await Task.Delay(10);
            throw expectedException;
        });

        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldBe("Test exception");

        var status = await _store.GetCommandStatusAsync(commandId);
        status.ShouldBe(CommandExecutionStatus.Failed);
    }

    [Fact]
    public async Task ExecuteBatchIdempotentAsync_ExecutesMultipleOperations()
    {
        // Arrange
        var operations = new (string commandId, Func<Task<string>> operation)[]
        {
            ("batch-cmd-1", () => Task.FromResult("result-1")),
            ("batch-cmd-2", () => Task.FromResult("result-2")),
            ("batch-cmd-3", () => Task.FromResult("result-3"))
        };

        // Act
        var results = await _store.ExecuteBatchIdempotentAsync<string>(operations);

        // Assert
        results.ShouldHaveCount(3);
        results["batch-cmd-1"].ShouldBe("result-1");
        results["batch-cmd-2"].ShouldBe("result-2");
        results["batch-cmd-3"].ShouldBe("result-3");
    }

    [Fact]
    public async Task ExecuteBatchIdempotentAsync_WhenCommandIsAlreadyCompleted_ReusesCachedResult()
    {
        // Arrange
        const string cachedCommandId = "batch-cached-1";
        const string pendingCommandId = "batch-cached-2";
        await _store.SetCommandStatusAsync(cachedCommandId, CommandExecutionStatus.Completed);
        await _store.SetCommandResultAsync(cachedCommandId, "cached-value");

        var invoked = false;

        var operations = new (string commandId, Func<Task<string>> operation)[]
        {
            (cachedCommandId, () =>
            {
                invoked = true;
                return Task.FromResult("should-not-run");
            }),
            (pendingCommandId, () => Task.FromResult("fresh-value"))
        };

        // Act
        var results = await _store.ExecuteBatchIdempotentAsync<string>(operations);

        // Assert
        invoked.ShouldBeFalse();
        results[cachedCommandId].ShouldBe("cached-value");
        results[pendingCommandId].ShouldBe("fresh-value");
    }

    [Fact]
    public async Task ExecuteBatchIdempotentAsync_WhenCachedResultIsNull_PreservesNull()
    {
        // Arrange
        const string cachedCommandId = "batch-cached-null";
        await _store.SetCommandStatusAsync(cachedCommandId, CommandExecutionStatus.Completed);
        await _store.SetCommandResultAsync<string?>(cachedCommandId, default!);

        var executed = false;

        var operations = new (string commandId, Func<Task<string?>> operation)[]
        {
            (cachedCommandId, () =>
            {
                executed = true;
                return Task.FromResult<string?>("should-not-execute");
            })
        };

        // Act
        var results = await _store.ExecuteBatchIdempotentAsync<string?>(operations);

        // Assert
        executed.ShouldBeFalse();
        results.ShouldHaveSingleItem();
        results[cachedCommandId].ShouldBeNull();
    }

    [Fact]
    public async Task TryGetCachedResultAsync_WhenResultExists_ReturnsResult()
    {
        // Arrange
        const string commandId = "test-cached-1";
        const string expectedResult = "cached-result";
        
        await _store.SetCommandStatusAsync(commandId, CommandExecutionStatus.Completed);
        await _store.SetCommandResultAsync(commandId, expectedResult);

        // Act
        var (hasResult, result) = await _store.TryGetCachedResultAsync<string>(commandId);

        // Assert
        hasResult.ShouldBeTrue();
        result.ShouldBe(expectedResult);
    }

    [Fact]
    public async Task TryGetCachedResultAsync_WhenResultDoesNotExist_ReturnsNoResult()
    {
        // Arrange
        const string commandId = "test-cached-2";

        // Act
        var (hasResult, result) = await _store.TryGetCachedResultAsync<string>(commandId);

        // Assert
        hasResult.ShouldBeFalse();
        result.ShouldBeNull();
    }

    [Fact]
    public async Task TryGetCachedResultAsync_WhenResultIsNull_ReturnsHasResultTrue()
    {
        // Arrange
        const string commandId = "test-cached-null";
        await _store.SetCommandStatusAsync(commandId, CommandExecutionStatus.Completed);
        await _store.SetCommandResultAsync<string?>(commandId, default!);

        // Act
        var (hasResult, result) = await _store.TryGetCachedResultAsync<string?>(commandId);

        // Assert
        hasResult.ShouldBeTrue();
        result.ShouldBeNull();
    }

    public void Dispose()
    {
        _store?.Dispose();
        _memoryCache?.Dispose();
    }
}

// Test implementation of ICommandIdempotencyStore for testing DI registration
public class TestCommandIdempotencyStore : ICommandIdempotencyStore
{
    public Task<CommandExecutionStatus> GetCommandStatusAsync(string commandId, System.Threading.CancellationToken cancellationToken = default)
        => Task.FromResult(CommandExecutionStatus.NotFound);

    public Task SetCommandStatusAsync(string commandId, CommandExecutionStatus status, System.Threading.CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<T?> GetCommandResultAsync<T>(string commandId, System.Threading.CancellationToken cancellationToken = default)
        => Task.FromResult<T?>(default);

    public Task SetCommandResultAsync<T>(string commandId, T result, System.Threading.CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveCommandAsync(string commandId, System.Threading.CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<bool> TrySetCommandStatusAsync(string commandId, CommandExecutionStatus expectedStatus, CommandExecutionStatus newStatus, System.Threading.CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<(CommandExecutionStatus currentStatus, bool wasSet)> GetAndSetStatusAsync(string commandId, CommandExecutionStatus newStatus, System.Threading.CancellationToken cancellationToken = default)
        => Task.FromResult((CommandExecutionStatus.NotFound, false));

    public Task<Dictionary<string, CommandExecutionStatus>> GetMultipleStatusAsync(IEnumerable<string> commandIds, System.Threading.CancellationToken cancellationToken = default)
        => Task.FromResult(new Dictionary<string, CommandExecutionStatus>());

    public Task<Dictionary<string, T?>> GetMultipleResultsAsync<T>(IEnumerable<string> commandIds, System.Threading.CancellationToken cancellationToken = default)
        => Task.FromResult(new Dictionary<string, T?>());

    public Task<int> CleanupExpiredCommandsAsync(TimeSpan maxAge, System.Threading.CancellationToken cancellationToken = default)
        => Task.FromResult(0);

    public Task<int> CleanupCommandsByStatusAsync(CommandExecutionStatus status, TimeSpan maxAge, System.Threading.CancellationToken cancellationToken = default)
        => Task.FromResult(0);

    public Task<Dictionary<CommandExecutionStatus, int>> GetCommandCountByStatusAsync(System.Threading.CancellationToken cancellationToken = default)
        => Task.FromResult(new Dictionary<CommandExecutionStatus, int>());
}
