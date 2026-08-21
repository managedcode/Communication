using System;
using System.Collections.Generic;
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

namespace ManagedCode.Communication.Tests.AspNetCore.Extensions;

public class CommandCleanupBackgroundServiceIntegrationTests
{
    [Test]
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
        store.CleanupCalls.ShouldNotContainKey(CommandExecutionStatus.Failed);
        store.CleanupCalls.ShouldNotContainKey(CommandExecutionStatus.InProgress);
        store.HealthMetricsQueries.ShouldBeGreaterThan(0);

        await app.StopAsync();
    }

    [Test]
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
        builder.Services.AddSingleton<ICommandIdempotencyMaintenance>(store);

        var options = new CommandCleanupOptions();
        configureCleanup(options);
        builder.Services.AddSingleton(options);
        builder.Services.AddHostedService<CommandCleanupBackgroundService>();

        var app = builder.Build();
        app.MapGet("/health", () => "ok");
        await app.StartAsync();

        return app;
    }

    private sealed class TrackingCleanupStore : ICommandIdempotencyStore, ICommandIdempotencyMaintenance
    {
        public Task<CommandIdempotencyAcquireResult<T>> TryAcquireAsync<T>(CommandIdempotencyDescriptor descriptor, System.Threading.CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> TryCompleteAsync<T>(CommandIdempotencyClaim claim, T outcome, TimeSpan retention, System.Threading.CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> TryRenewAsync(CommandIdempotencyClaim claim, TimeSpan lease, System.Threading.CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> TryMarkIndeterminateAsync(CommandIdempotencyClaim claim, Problem problem, System.Threading.CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> TryReleaseAsync(CommandIdempotencyClaim claim, System.Threading.CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

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

        public Task<int> CleanupCompletedCommandsAsync(System.TimeSpan maxAge, System.Threading.CancellationToken cancellationToken = default)
        {
            const CommandExecutionStatus status = CommandExecutionStatus.Completed;
            lock (_sync)
            {
                if (!CleanupCalls.TryGetValue(status, out var count))
                {
                    count = 0;
                }

                count++;
                CleanupCalls[status] = count;

                if (!_completed)
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

                if ((CleanupCalls.TryGetValue(CommandExecutionStatus.Completed, out var completed) ? completed : 0) >= 2)
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
    }
}
