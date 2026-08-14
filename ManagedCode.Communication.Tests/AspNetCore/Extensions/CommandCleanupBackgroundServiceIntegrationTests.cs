using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Communication.AspNetCore.Extensions;
using ManagedCode.Communication.Commands;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace ManagedCode.Communication.Tests.AspNetCore.Extensions;

public class CommandCleanupBackgroundServiceIntegrationTests
{
    [Fact]
    public async Task CommandCleanupBackgroundService_PerformsCleanupAndHealthLogging()
    {
        var store = new TrackingCleanupStore();

        await using var app = await CreateAppAsync(store, options =>
        {
            options.CleanupInterval = TimeSpan.FromMilliseconds(10);
            options.LogHealthMetrics = true;
        });

        await store.WaitForCleanupCyclesAsync(1);
        await store.WaitForHealthMetricsAsync();

        store.CleanupCalls.ShouldContainKey(CommandExecutionStatus.Completed);
        store.CleanupCalls.ShouldContainKey(CommandExecutionStatus.Failed);
        store.CleanupCalls.ShouldContainKey(CommandExecutionStatus.InProgress);
        store.HealthMetricsQueries.ShouldBeGreaterThan(0);

        await app.StopAsync();
    }

    [Fact]
    public async Task CommandCleanupBackgroundService_ContinuesAfterCleanupFailure()
    {
        var store = new TrackingCleanupStore(throwOnFirstCompletedCleanup: true);

        await using var app = await CreateAppAsync(store, options =>
        {
            options.CleanupInterval = TimeSpan.FromMilliseconds(10);
            options.LogHealthMetrics = false;
        });

        await store.WaitForCleanupCyclesAsync(2);
        store.CleanupFailures.ShouldBe(1);

        await app.StopAsync();
    }

    private static async Task<WebApplication> CreateAppAsync(
        TrackingCleanupStore store,
        Action<CommandCleanupOptions> configureCleanup)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddSingleton<ICommandIdempotencyStore>(store);

        var options = new CommandCleanupOptions();
        configureCleanup(options);
        builder.Services.AddSingleton(options);
        builder.Services.AddHostedService<CommandCleanupBackgroundService>();

        var app = builder.Build();
        app.MapGet("/health", () => "ok");
        await app.StartAsync();

        return app;
    }

    private sealed class TrackingCleanupStore : ICommandIdempotencyStore
    {
        private readonly bool _throwOnFirstCompletedCleanup;
        private readonly object _sync = new();
        private bool _completed;
        private int _cleanupCycle = 0;
        private readonly TaskCompletionSource<int> _firstIterationTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<int> _secondIterationTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _healthMetricsTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TrackingCleanupStore(bool throwOnFirstCompletedCleanup = false)
        {
            _throwOnFirstCompletedCleanup = throwOnFirstCompletedCleanup;
        }

        public Dictionary<CommandExecutionStatus, int> CleanupCalls { get; } = new();
        public int CleanupFailures { get; private set; }
        public int HealthMetricsQueries { get; private set; }

        public Task<int> CleanupExpiredCommandsAsync(System.TimeSpan maxAge, System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<int> CleanupCommandsByStatusAsync(CommandExecutionStatus status, System.TimeSpan maxAge, System.Threading.CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                if (!CleanupCalls.TryGetValue(status, out var count))
                {
                    count = 0;
                }

                count++;
                CleanupCalls[status] = count;

                if (status == CommandExecutionStatus.Completed && !_completed)
                {
                    if (_throwOnFirstCompletedCleanup)
                    {
                        _completed = true;
                        CleanupFailures++;
                        throw new InvalidOperationException("Temporary cleanup failure");
                    }

                    if (CleanupCalls[CommandExecutionStatus.Completed] == 1)
                    {
                        _firstIterationTcs.TrySetResult(_cleanupCycle + 1);
                    }
                }

                if (status == CommandExecutionStatus.Completed &&
                    (CleanupCalls.TryGetValue(CommandExecutionStatus.Completed, out var completed) ? completed : 0) >= 2)
                {
                    _cleanupCycle = 1;
                    _secondIterationTcs.TrySetResult(_cleanupCycle);
                }

                return Task.FromResult(1);
            }
        }

        public Task<Dictionary<CommandExecutionStatus, int>> GetCommandCountByStatusAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            HealthMetricsQueries++;
            _healthMetricsTcs.TrySetResult(true);

            return Task.FromResult(new Dictionary<CommandExecutionStatus, int>
            {
                [CommandExecutionStatus.Completed] = 1,
                [CommandExecutionStatus.InProgress] = 2,
                [CommandExecutionStatus.Failed] = 3,
                [CommandExecutionStatus.Processing] = 0
            });
        }

        public Task<(CommandExecutionStatus currentStatus, bool wasSet)> GetAndSetStatusAsync(string commandId, CommandExecutionStatus newStatus, System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult<(CommandExecutionStatus, bool)>((CommandExecutionStatus.NotFound, true));

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

        public Task<Dictionary<string, CommandExecutionStatus>> GetMultipleStatusAsync(IEnumerable<string> commandIds, System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult(new Dictionary<string, CommandExecutionStatus>());

        public Task<Dictionary<string, T?>> GetMultipleResultsAsync<T>(IEnumerable<string> commandIds, System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult(new Dictionary<string, T?>());

        public async Task WaitForCleanupCyclesAsync(int requiredCompletedCycles)
        {
            if (requiredCompletedCycles <= 1)
            {
                await _firstIterationTcs.Task.WaitAsync(System.TimeSpan.FromSeconds(3));
                return;
            }

            await _secondIterationTcs.Task.WaitAsync(System.TimeSpan.FromSeconds(3));
        }

        public async Task WaitForHealthMetricsAsync()
        {
            await _healthMetricsTcs.Task.WaitAsync(System.TimeSpan.FromSeconds(3));
        }

        public void Dispose()
        {
        }
    }
}
