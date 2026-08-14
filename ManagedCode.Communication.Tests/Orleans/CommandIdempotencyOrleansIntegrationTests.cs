using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Orleans.Grains;
using ManagedCode.Communication.Orleans.Stores;
using ManagedCode.Communication.Tests.Orleans.Fixtures;
using Orleans;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.Orleans;

public class CommandIdempotencyOrleansIntegrationTests : IClassFixture<OrleansClusterFixture>
{
    private readonly IGrainFactory _grainFactory;
    private readonly OrleansCommandIdempotencyStore _store;

    public CommandIdempotencyOrleansIntegrationTests(OrleansClusterFixture fixture)
    {
        _grainFactory = fixture.Cluster.GrainFactory;
        _store = new OrleansCommandIdempotencyStore(_grainFactory);
    }

    [Fact]
    public async Task Grain_StartAndCompleteLifecycle_ResetsForRetry()
    {
        var commandId = Guid.NewGuid().ToString();
        var grain = _grainFactory.GetGrain<ICommandIdempotencyGrain>(commandId);

        (await grain.GetStatusAsync()).ShouldBe(CommandExecutionStatus.NotFound);

        (await grain.TryStartProcessingAsync()).ShouldBeTrue();
        (await grain.GetStatusAsync()).ShouldBe(CommandExecutionStatus.Processing);

        (await grain.TryStartProcessingAsync()).ShouldBeFalse();
        (await grain.GetStatusAsync()).ShouldBe(CommandExecutionStatus.Processing);

        var initialResult = await grain.TryGetResultAsync();
        initialResult.success.ShouldBeFalse();
        initialResult.result.ShouldBeNull();

        await grain.MarkCompletedAsync("done");
        (await grain.GetStatusAsync()).ShouldBe(CommandExecutionStatus.Completed);

        var completedResult = await grain.TryGetResultAsync();
        completedResult.success.ShouldBeTrue();
        completedResult.result.ShouldBe("done");

        (await grain.TryStartProcessingAsync()).ShouldBeFalse();

        await grain.MarkFailedAsync("retry-allowed-reset");
        (await grain.GetStatusAsync()).ShouldBe(CommandExecutionStatus.Failed);

        (await grain.TryStartProcessingAsync()).ShouldBeTrue();
        (await grain.GetStatusAsync()).ShouldBe(CommandExecutionStatus.Processing);

        await grain.MarkFailedAsync("failure");
        (await grain.GetStatusAsync()).ShouldBe(CommandExecutionStatus.Failed);

        var finalAfterFail = await grain.TryGetResultAsync();
        finalAfterFail.success.ShouldBeFalse();

        await grain.ClearAsync();
        (await grain.GetStatusAsync()).ShouldBe(CommandExecutionStatus.NotFound);
    }

    [Fact]
    public async Task Grain_TrySetStatusAsync_TransitionsBetweenStatuses()
    {
        var grain = _grainFactory.GetGrain<ICommandIdempotencyGrain>(Guid.NewGuid().ToString());

        var startedFromNotFound = await grain.TrySetStatusAsync(CommandExecutionStatus.NotFound, CommandExecutionStatus.InProgress);
        startedFromNotFound.ShouldBeTrue();
        (await grain.GetStatusAsync()).ShouldBe(CommandExecutionStatus.Processing);

        var completedFromWrongExpected = await grain.TrySetStatusAsync(CommandExecutionStatus.NotFound, CommandExecutionStatus.Completed);
        completedFromWrongExpected.ShouldBeFalse();

        var failedFromProcessing = await grain.TrySetStatusAsync(CommandExecutionStatus.Processing, CommandExecutionStatus.Failed);
        failedFromProcessing.ShouldBeTrue();
        (await grain.GetStatusAsync()).ShouldBe(CommandExecutionStatus.Failed);

        var completedFromFailed = await grain.TrySetStatusAsync(CommandExecutionStatus.Failed, CommandExecutionStatus.Completed);
        completedFromFailed.ShouldBeTrue();
        (await grain.GetStatusAsync()).ShouldBe(CommandExecutionStatus.Completed);

        var resetToNotStarted = await grain.TrySetStatusAsync(CommandExecutionStatus.Completed, CommandExecutionStatus.NotStarted);
        resetToNotStarted.ShouldBeTrue();
        (await grain.GetStatusAsync()).ShouldBe(CommandExecutionStatus.NotStarted);
    }

    [Fact]
    public async Task Store_BasicLifecycle_CoversCoreMethodsAndBatchReadPaths()
    {
        var inProgressCommand = Guid.NewGuid().ToString();

        (await _store.GetCommandStatusAsync(inProgressCommand)).ShouldBe(CommandExecutionStatus.NotFound);
        await _store.SetCommandStatusAsync(inProgressCommand, CommandExecutionStatus.Processing);
        (await _store.GetCommandStatusAsync(inProgressCommand)).ShouldBe(CommandExecutionStatus.Processing);

        await _store.SetCommandStatusAsync(inProgressCommand, CommandExecutionStatus.InProgress);
        (await _store.GetCommandStatusAsync(inProgressCommand)).ShouldBe(CommandExecutionStatus.Processing);

        var setToFailed = await _store.TrySetCommandStatusAsync(
            inProgressCommand,
            CommandExecutionStatus.Processing,
            CommandExecutionStatus.Failed);
        setToFailed.ShouldBeTrue();
        (await _store.GetCommandStatusAsync(inProgressCommand)).ShouldBe(CommandExecutionStatus.Failed);

        var (previousStatus, wasSet) = await _store.GetAndSetStatusAsync(inProgressCommand, CommandExecutionStatus.Completed);
        previousStatus.ShouldBe(CommandExecutionStatus.Failed);
        wasSet.ShouldBeTrue();
        (await _store.GetCommandStatusAsync(inProgressCommand)).ShouldBe(CommandExecutionStatus.Completed);

        await _store.SetCommandStatusAsync(inProgressCommand, CommandExecutionStatus.NotStarted);
        (await _store.GetCommandStatusAsync(inProgressCommand)).ShouldBe(CommandExecutionStatus.Completed);

        await _store.SetCommandStatusAsync(inProgressCommand, CommandExecutionStatus.NotFound);
        (await _store.GetCommandStatusAsync(inProgressCommand)).ShouldBe(CommandExecutionStatus.Completed);

        await _store.SetCommandResultAsync(inProgressCommand, "value-1");
        (await _store.GetCommandResultAsync<string>(inProgressCommand)).ShouldBe("value-1");

        var legacyCommandId = Guid.NewGuid();
        await _store.MarkCompletedAsync(legacyCommandId, 99);
        (await _store.GetStatusAsync(legacyCommandId)).ShouldBe(CommandExecutionStatus.Completed);

        var completedWithResult = await _store.TryGetResultAsync<int>(legacyCommandId);
        completedWithResult.Item1.ShouldBeTrue();
        completedWithResult.Item2.ShouldBe(99);

        var commandWithoutResult = Guid.NewGuid().ToString();
        await _store.SetCommandStatusAsync(commandWithoutResult, CommandExecutionStatus.Completed);
        (await _store.GetCommandStatusAsync(commandWithoutResult)).ShouldBe(CommandExecutionStatus.Completed);
        (await _store.GetCommandResultAsync<string>(commandWithoutResult)).ShouldBeNull();
    }

    [Fact]
    public async Task Store_BatchHelpers_ReturnExpectedMaps()
    {
        var command1 = Guid.NewGuid().ToString();
        var command2 = Guid.NewGuid().ToString();
        var command3 = Guid.NewGuid().ToString();

        await _store.SetCommandStatusAsync(command1, CommandExecutionStatus.Processing);
        await _store.SetCommandStatusAsync(command2, CommandExecutionStatus.Failed);

        var statuses = await _store.GetMultipleStatusAsync(new[] { command1, command2, command3 });
        statuses.Count.ShouldBe(3);
        statuses[command1].ShouldBe(CommandExecutionStatus.Processing);
        statuses[command2].ShouldBe(CommandExecutionStatus.Failed);
        statuses[command3].ShouldBe(CommandExecutionStatus.NotFound);

        await _store.SetCommandResultAsync(command1, "first");
        await _store.SetCommandStatusAsync(command2, CommandExecutionStatus.Completed);

        var results = await _store.GetMultipleResultsAsync<string>(new[] { command1, command2, command3 });
        results.Count.ShouldBe(3);
        results[command1].ShouldBe("first");
        results[command2].ShouldBeNull();
        results[command3].ShouldBeNull();
    }

    [Fact]
    public async Task Store_MigrationStyleMethods_CoverTypeMismatchAndFailureFallback()
    {
        var commandId = Guid.NewGuid();

        await _store.MarkCompletedAsync(commandId, "legacy-result");
        var typedMatch = await _store.TryGetResultAsync<string>(commandId);
        typedMatch.Item1.ShouldBeTrue();
        typedMatch.Item2.ShouldBe("legacy-result");

        var mismatch = await _store.TryGetResultAsync<Uri>(commandId);
        mismatch.Item1.ShouldBeTrue();
        mismatch.Item2.ShouldBeNull();

        await _store.MarkFailedAsync(commandId, "oops");
        (await _store.GetStatusAsync(commandId)).ShouldBe(CommandExecutionStatus.Failed);

        var failedResult = await _store.TryGetResultAsync<string>(commandId);
        failedResult.Item1.ShouldBeFalse();

        await _store.RemoveCommandAsync(commandId.ToString());
        (await _store.GetStatusAsync(commandId)).ShouldBe(CommandExecutionStatus.NotFound);
    }

    [Fact]
    public async Task Store_NoOpCleanupAndCounts_ReturnDefaults()
    {
        var expiry = await _store.CleanupExpiredCommandsAsync(TimeSpan.FromMinutes(10));
        expiry.ShouldBe(0);

        var byStatus = await _store.CleanupCommandsByStatusAsync(CommandExecutionStatus.Completed, TimeSpan.FromMinutes(10));
        byStatus.ShouldBe(0);

        var counts = await _store.GetCommandCountByStatusAsync();
        counts.ShouldBeEmpty();
    }
}
