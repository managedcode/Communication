using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication.AspNetCore.Extensions;

/// <summary>
/// Extension methods for command cleanup operations
/// </summary>
public static class CommandCleanupExtensions
{
    /// <summary>
    /// Perform automatic cleanup of expired commands
    /// </summary>
    public static async Task<int> AutoCleanupAsync(
        this ICommandIdempotencyMaintenance store,
        TimeSpan? completedCommandMaxAge = null,
        CancellationToken cancellationToken = default)
    {
        completedCommandMaxAge ??= TimeSpan.FromHours(24);
        return await store.CleanupCompletedCommandsAsync(completedCommandMaxAge.Value, cancellationToken);
    }

    /// <summary>
    /// Get health metrics for monitoring
    /// </summary>
    public static async Task<CommandStoreHealthMetrics> GetHealthMetricsAsync(
        this ICommandIdempotencyMaintenance store,
        CancellationToken cancellationToken = default)
    {
        var counts = await store.GetCommandCountByStatusAsync(cancellationToken);

        return new CommandStoreHealthMetrics
        {
            TotalCommands = counts.Values.Sum(),
            CompletedCommands = counts.GetValueOrDefault(CommandExecutionStatus.Completed, 0),
            InProgressCommands = counts.GetValueOrDefault(CommandExecutionStatus.InProgress, 0),
            FailedCommands = counts.GetValueOrDefault(CommandExecutionStatus.Failed, 0),
            ProcessingCommands = counts.GetValueOrDefault(CommandExecutionStatus.Processing, 0),
            IndeterminateCommands = counts.GetValueOrDefault(CommandExecutionStatus.Indeterminate, 0),
            Timestamp = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Health metrics for command store monitoring
/// </summary>
public record CommandStoreHealthMetrics
{
    /// <summary>
    ///     Total number of tracked commands.
    /// </summary>
    public int TotalCommands { get; init; }
    /// <summary>
    ///     Number of commands that finished successfully.
    /// </summary>
    public int CompletedCommands { get; init; }
    /// <summary>
    ///     Number of commands currently claimed.
    /// </summary>
    public int InProgressCommands { get; init; }
    /// <summary>
    ///     Number of commands that failed.
    /// </summary>
    public int FailedCommands { get; init; }
    /// <summary>
    ///     Number of commands being processed.
    /// </summary>
    public int ProcessingCommands { get; init; }
    /// <summary>
    ///     Number of commands requiring explicit operational resolution because their side-effect outcome is unknown.
    /// </summary>
    public int IndeterminateCommands { get; init; }
    /// <summary>
    ///     When the snapshot was taken (UTC).
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Percentage of commands that are stuck in progress (potential issue)
    /// </summary>
    public double StuckCommandsPercentage =>
        TotalCommands > 0 ? (double)InProgressCommands / TotalCommands * 100 : 0;

    /// <summary>
    /// Percentage of commands that failed (error rate)
    /// </summary>
    public double FailureRate =>
        TotalCommands > 0 ? (double)FailedCommands / TotalCommands * 100 : 0;
}

/// <summary>
/// Background service for automatic command cleanup
/// </summary>
public class CommandCleanupBackgroundService : BackgroundService
{
    private readonly ICommandIdempotencyMaintenance _store;
    private readonly ILogger<CommandCleanupBackgroundService> _logger;
    private readonly TimeSpan _cleanupInterval;
    private readonly CommandCleanupOptions _options;

    /// <summary>
    ///     Creates the background service that prunes old idempotency records.
    /// </summary>
    public CommandCleanupBackgroundService(
        ICommandIdempotencyMaintenance store,
        ILogger<CommandCleanupBackgroundService> logger,
        CommandCleanupOptions? options = null)
    {
        _store = store;
        _logger = logger;
        _options = options ?? new CommandCleanupOptions();
        _cleanupInterval = _options.CleanupInterval;
    }

    /// <summary>
    ///     Runs cleanup passes until the host shuts down.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LoggerCenter.LogCleanupServiceStarted(_logger, _cleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cleanedCount = await _store.AutoCleanupAsync(
                    _options.CompletedCommandMaxAge,
                    stoppingToken);

                if (cleanedCount > 0)
                {
                    LoggerCenter.LogCleanupCompleted(_logger, cleanedCount);
                }

                // Log health metrics
                if (_options.LogHealthMetrics)
                {
                    var metrics = await _store.GetHealthMetricsAsync(stoppingToken);
                    LoggerCenter.LogHealthMetrics(_logger,
                        metrics.TotalCommands,
                        metrics.CompletedCommands,
                        metrics.FailedCommands,
                        metrics.InProgressCommands,
                        metrics.FailureRate / 100, // Convert to ratio for formatting
                        metrics.StuckCommandsPercentage / 100); // Convert to ratio for formatting
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                LoggerCenter.LogCleanupError(_logger, ex);
            }

            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        LoggerCenter.LogCleanupServiceStopped(_logger);
    }
}

/// <summary>
/// Configuration options for command cleanup
/// </summary>
public class CommandCleanupOptions
{
    /// <summary>
    /// How often to run cleanup
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// How long to keep completed commands (for caching)
    /// </summary>
    public TimeSpan CompletedCommandMaxAge { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Whether to log health metrics during cleanup
    /// </summary>
    public bool LogHealthMetrics { get; set; } = true;
}
