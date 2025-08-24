using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Commands.Extensions;
using ManagedCode.Communication.Commands.Stores;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

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
        
        store.Should().BeOfType<MemoryCacheCommandIdempotencyStore>();
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
        
        store.Should().BeOfType<TestCommandIdempotencyStore>();
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
        
        store.Should().BeSameAs(customStore);
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
        status.Should().Be(CommandExecutionStatus.NotFound);
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
        status.Should().Be(CommandExecutionStatus.InProgress);
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
        result.Should().Be(expectedResult);
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
        
        status.Should().Be(CommandExecutionStatus.NotFound);
        result.Should().BeNull();
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
        result.Should().BeTrue();
        var status = await _store.GetCommandStatusAsync(commandId);
        status.Should().Be(CommandExecutionStatus.Completed);
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
        result.Should().BeFalse();
        var status = await _store.GetCommandStatusAsync(commandId);
        status.Should().Be(CommandExecutionStatus.InProgress); // Unchanged
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
        currentStatus.Should().Be(CommandExecutionStatus.InProgress);
        wasSet.Should().BeTrue();
        
        var newStatus = await _store.GetCommandStatusAsync(commandId);
        newStatus.Should().Be(CommandExecutionStatus.Completed);
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
        statuses.Should().HaveCount(3);
        statuses["cmd1"].Should().Be(CommandExecutionStatus.Completed);
        statuses["cmd2"].Should().Be(CommandExecutionStatus.InProgress);
        statuses["cmd3"].Should().Be(CommandExecutionStatus.NotFound);
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
        results.Should().HaveCount(3);
        results["cmd1"].Should().Be("result1");
        results["cmd2"].Should().Be("result2");
        results["cmd3"].Should().BeNull();
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
        result.Should().Be(expectedResult);
        executionCount.Should().Be(1);
        
        var status = await _store.GetCommandStatusAsync(commandId);
        status.Should().Be(CommandExecutionStatus.Completed);
        
        var storedResult = await _store.GetCommandResultAsync<string>(commandId);
        storedResult.Should().Be(expectedResult);
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
        result.Should().Be(expectedResult);
        executionCount.Should().Be(1); // Should not execute second time
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

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");

        var status = await _store.GetCommandStatusAsync(commandId);
        status.Should().Be(CommandExecutionStatus.Failed);
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
        results.Should().HaveCount(3);
        results["batch-cmd-1"].Should().Be("result-1");
        results["batch-cmd-2"].Should().Be("result-2");
        results["batch-cmd-3"].Should().Be("result-3");
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
        hasResult.Should().BeTrue();
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task TryGetCachedResultAsync_WhenResultDoesNotExist_ReturnsNoResult()
    {
        // Arrange
        const string commandId = "test-cached-2";

        // Act
        var (hasResult, result) = await _store.TryGetCachedResultAsync<string>(commandId);

        // Assert
        hasResult.Should().BeFalse();
        result.Should().BeNull();
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