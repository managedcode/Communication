using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.AspNetCore.Extensions;
using ManagedCode.Communication.Commands;
using Shouldly;

namespace ManagedCode.Communication.Tests.AspNetCore.Extensions;

public class CommandCleanupExtensionsTests
{
    [Test]
    public async Task AutoCleanupAsync_CleansOnlyCompletedOutcomes()
    {
        var store = new TrackingCommandIdempotencyStore
        {
            CompletedCleanupResult = 2
        };

        var result = await CommandCleanupExtensions.AutoCleanupAsync(store);

        result.ShouldBe(2);
        store.CleanupCalls.ShouldBe([TimeSpan.FromHours(24)]);
    }

    [Test]
    public async Task GetHealthMetricsAsync_ReturnsCommandCountsAndRatios()
    {
        var store = new TrackingCommandIdempotencyStore
        {
            HealthMetrics = new Dictionary<CommandExecutionStatus, int>
            {
                [CommandExecutionStatus.Completed] = 6,
                [CommandExecutionStatus.InProgress] = 2,
                [CommandExecutionStatus.Failed] = 1,
                [CommandExecutionStatus.Processing] = 1
            }
        };

        var metrics = await CommandCleanupExtensions.GetHealthMetricsAsync(store);

        metrics.TotalCommands.ShouldBe(10);
        metrics.CompletedCommands.ShouldBe(6);
        metrics.InProgressCommands.ShouldBe(2);
        metrics.FailedCommands.ShouldBe(1);
        metrics.ProcessingCommands.ShouldBe(1);
        metrics.StuckCommandsPercentage.ShouldBe(20);
        metrics.FailureRate.ShouldBe(10);
    }

    [Test]
    public async Task GetHealthMetricsAsync_WhenNoCommands_ReturnsZeroRates()
    {
        var store = new TrackingCommandIdempotencyStore
        {
            HealthMetrics = new Dictionary<CommandExecutionStatus, int>()
        };

        var metrics = await CommandCleanupExtensions.GetHealthMetricsAsync(store);

        metrics.TotalCommands.ShouldBe(0);
        metrics.CompletedCommands.ShouldBe(0);
        metrics.InProgressCommands.ShouldBe(0);
        metrics.FailedCommands.ShouldBe(0);
        metrics.ProcessingCommands.ShouldBe(0);
        metrics.StuckCommandsPercentage.ShouldBe(0);
        metrics.FailureRate.ShouldBe(0);
    }

    private sealed class TrackingCommandIdempotencyStore : ICommandIdempotencyStore, ICommandIdempotencyMaintenance
    {
        public Task<CommandIdempotencyAcquireResult<T>> TryAcquireAsync<T>(CommandIdempotencyDescriptor descriptor, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> TryCompleteAsync<T>(CommandIdempotencyClaim claim, T outcome, TimeSpan retention, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> TryRenewAsync(CommandIdempotencyClaim claim, TimeSpan lease, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> TryMarkIndeterminateAsync(CommandIdempotencyClaim claim, Problem problem, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> TryReleaseAsync(CommandIdempotencyClaim claim, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public readonly List<TimeSpan> CleanupCalls = new();
        public Dictionary<CommandExecutionStatus, int> HealthMetrics { get; set; } = new();
        public int CompletedCleanupResult { get; init; }

        public Task<int> CleanupCompletedCommandsAsync(
            TimeSpan maxAge,
            CancellationToken cancellationToken = default)
        {
            CleanupCalls.Add(maxAge);
            return Task.FromResult(CompletedCleanupResult);
        }

        public Task<Dictionary<CommandExecutionStatus, int>> GetCommandCountByStatusAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult(HealthMetrics);
    }
}
