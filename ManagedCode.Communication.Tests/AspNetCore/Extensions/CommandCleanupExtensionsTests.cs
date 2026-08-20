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
    public async Task AutoCleanupAsync_UsesDefaultAgesAndAggregatesResults()
    {
        var store = new TrackingCommandIdempotencyStore
        {
            CompletedCleanupResult = 2,
            FailedCleanupResult = 3,
            InProgressCleanupResult = 4
        };

        var result = await CommandCleanupExtensions.AutoCleanupAsync(store);

        result.ShouldBe(9);
        store.CleanupCalls.Count.ShouldBe(3);
        store.CleanupCalls[0].Status.ShouldBe(CommandExecutionStatus.Completed);
        store.CleanupCalls[0].MaxAge.ShouldBe(TimeSpan.FromHours(24));
        store.CleanupCalls[1].Status.ShouldBe(CommandExecutionStatus.Failed);
        store.CleanupCalls[1].MaxAge.ShouldBe(TimeSpan.FromHours(1));
        store.CleanupCalls[2].Status.ShouldBe(CommandExecutionStatus.InProgress);
        store.CleanupCalls[2].MaxAge.ShouldBe(TimeSpan.FromMinutes(30));
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

    private sealed class TrackingCommandIdempotencyStore : ICommandIdempotencyStore
    {
        private readonly Dictionary<string, CommandExecutionStatus> _statuses = new();

        public readonly List<(CommandExecutionStatus Status, TimeSpan MaxAge)> CleanupCalls = new();
        public Dictionary<CommandExecutionStatus, int> HealthMetrics { get; set; } = new();
        public int CompletedCleanupResult { get; init; }
        public int FailedCleanupResult { get; init; }
        public int InProgressCleanupResult { get; init; }

        public Task<CommandExecutionStatus> GetCommandStatusAsync(string commandId, CancellationToken cancellationToken = default)
            => Task.FromResult(_statuses.GetValueOrDefault(commandId, CommandExecutionStatus.NotFound));

        public Task SetCommandStatusAsync(string commandId, CommandExecutionStatus status, CancellationToken cancellationToken = default)
        {
            _statuses[commandId] = status;
            return Task.CompletedTask;
        }

        public Task<T?> GetCommandResultAsync<T>(string commandId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task SetCommandResultAsync<T>(string commandId, T result, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveCommandAsync(string commandId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<bool> TrySetCommandStatusAsync(
            string commandId,
            CommandExecutionStatus expectedStatus,
            CommandExecutionStatus newStatus,
            CancellationToken cancellationToken = default)
        {
            var current = _statuses.GetValueOrDefault(commandId, CommandExecutionStatus.NotFound);

            if (current == expectedStatus)
            {
                _statuses[commandId] = newStatus;
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<(CommandExecutionStatus currentStatus, bool wasSet)> GetAndSetStatusAsync(
            string commandId,
            CommandExecutionStatus newStatus,
            CancellationToken cancellationToken = default)
        {
            var current = _statuses.GetValueOrDefault(commandId, CommandExecutionStatus.NotFound);
            _statuses[commandId] = newStatus;
            return Task.FromResult((current, true));
        }

        public Task<Dictionary<string, CommandExecutionStatus>> GetMultipleStatusAsync(
            IEnumerable<string> commandIds,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new Dictionary<string, CommandExecutionStatus>());

        public Task<Dictionary<string, T?>> GetMultipleResultsAsync<T>(
            IEnumerable<string> commandIds,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<int> CleanupExpiredCommandsAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<int> CleanupCommandsByStatusAsync(
            CommandExecutionStatus status,
            TimeSpan maxAge,
            CancellationToken cancellationToken = default)
        {
            CleanupCalls.Add((status, maxAge));

            return status switch
            {
                CommandExecutionStatus.Completed => Task.FromResult(CompletedCleanupResult),
                CommandExecutionStatus.Failed => Task.FromResult(FailedCleanupResult),
                CommandExecutionStatus.InProgress => Task.FromResult(InProgressCleanupResult),
                _ => Task.FromResult(0)
            };
        }

        public Task<Dictionary<CommandExecutionStatus, int>> GetCommandCountByStatusAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult(HealthMetrics);

        public void Dispose() { }
    }
}
