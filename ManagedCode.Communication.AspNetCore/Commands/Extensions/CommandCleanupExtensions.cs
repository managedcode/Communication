using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Logging;

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
        this ICommandIdempotencyStore store,
        TimeSpan? completedCommandMaxAge = null,
        TimeSpan? failedCommandMaxAge = null,
        TimeSpan? inProgressCommandMaxAge = null,
        CancellationToken cancellationToken = default)
    {
        completedCommandMaxAge ??= TimeSpan.FromHours(24);
        failedCommandMaxAge ??= TimeSpan.FromHours(1);
        inProgressCommandMaxAge ??= TimeSpan.FromMinutes(30);

        var totalCleaned = 0;

        // Clean up completed commands (keep longer for caching)
        totalCleaned += await store.CleanupCommandsByStatusAsync(
            CommandExecutionStatus.Completed, 
            completedCommandMaxAge.Value, 
            cancellationToken);

        // Clean up failed commands (clean faster to retry)
        totalCleaned += await store.CleanupCommandsByStatusAsync(
            CommandExecutionStatus.Failed, 
            failedCommandMaxAge.Value, 
            cancellationToken);

        // Clean up stuck in-progress commands (potential zombies)
        totalCleaned += await store.CleanupCommandsByStatusAsync(
            CommandExecutionStatus.InProgress, 
            inProgressCommandMaxAge.Value, 
            cancellationToken);

        return totalCleaned;
    }

    /// <summary>
    /// Get health metrics for monitoring
    /// </summary>
    public static async Task<CommandStoreHealthMetrics> GetHealthMetricsAsync(
        this ICommandIdempotencyStore store,
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
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}

/// <summary>
/// Health metrics for command store monitoring
/// </summary>
public record CommandStoreHealthMetrics
{
    public int TotalCommands { get; init; }
    public int CompletedCommands { get; init; }
    public int InProgressCommands { get; init; }
    public int FailedCommands { get; init; }
    public int ProcessingCommands { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    
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
    private readonly ICommandIdempotencyStore _store;
    private readonly ILogger<CommandCleanupBackgroundService> _logger;
    private readonly TimeSpan _cleanupInterval;
    private readonly CommandCleanupOptions _options;

    public CommandCleanupBackgroundService(
        ICommandIdempotencyStore store,
        ILogger<CommandCleanupBackgroundService> logger,
        CommandCleanupOptions? options = null)
    {
        _store = store;
        _logger = logger;
        _options = options ?? new CommandCleanupOptions();
        _cleanupInterval = _options.CleanupInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LoggerCenter.LogCleanupServiceStarted(_logger, _cleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cleanedCount = await _store.AutoCleanupAsync(
                    _options.CompletedCommandMaxAge,
                    _options.FailedCommandMaxAge,
                    _options.InProgressCommandMaxAge,
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
    /// How long to keep failed commands before allowing cleanup
    /// </summary>
    public TimeSpan FailedCommandMaxAge { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// How long before in-progress commands are considered stuck
    /// </summary>
    public TimeSpan InProgressCommandMaxAge { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Whether to log health metrics during cleanup
    /// </summary>
    public bool LogHealthMetrics { get; set; } = true;
}