using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ManagedCode.Communication.Logging;

namespace ManagedCode.Communication.Commands.Stores;

/// <summary>
/// Memory cache-based implementation of command idempotency store.
/// Suitable for single-instance applications and development environments.
/// </summary>
public class MemoryCacheCommandIdempotencyStore : ICommandIdempotencyStore, IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheCommandIdempotencyStore> _logger;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _commandTimestamps;
    private readonly object _lockObject = new();
    private bool _disposed;

    public MemoryCacheCommandIdempotencyStore(
        IMemoryCache memoryCache, 
        ILogger<MemoryCacheCommandIdempotencyStore> logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _commandTimestamps = new ConcurrentDictionary<string, DateTimeOffset>();
    }

    public Task<CommandExecutionStatus> GetCommandStatusAsync(string commandId, CancellationToken cancellationToken = default)
    {
        var statusKey = GetStatusKey(commandId);
        var status = _memoryCache.Get<CommandExecutionStatus?>(statusKey) ?? CommandExecutionStatus.NotFound;
        return Task.FromResult(status);
    }

    public Task SetCommandStatusAsync(string commandId, CommandExecutionStatus status, CancellationToken cancellationToken = default)
    {
        var statusKey = GetStatusKey(commandId);
        var options = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(24), // Keep for 24 hours
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7) // Hard limit 7 days
        };

        _memoryCache.Set(statusKey, status, options);
        _commandTimestamps[commandId] = DateTimeOffset.UtcNow;
        
        return Task.CompletedTask;
    }

    public Task<T?> GetCommandResultAsync<T>(string commandId, CancellationToken cancellationToken = default)
    {
        var resultKey = GetResultKey(commandId);
        var result = _memoryCache.Get<T>(resultKey);
        return Task.FromResult(result);
    }

    public Task SetCommandResultAsync<T>(string commandId, T result, CancellationToken cancellationToken = default)
    {
        var resultKey = GetResultKey(commandId);
        var options = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(24),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
        };

        _memoryCache.Set(resultKey, result, options);
        return Task.CompletedTask;
    }

    public Task RemoveCommandAsync(string commandId, CancellationToken cancellationToken = default)
    {
        var statusKey = GetStatusKey(commandId);
        var resultKey = GetResultKey(commandId);
        
        _memoryCache.Remove(statusKey);
        _memoryCache.Remove(resultKey);
        _commandTimestamps.TryRemove(commandId, out _);
        
        return Task.CompletedTask;
    }

    // Atomic operations
    public Task<bool> TrySetCommandStatusAsync(string commandId, CommandExecutionStatus expectedStatus, CommandExecutionStatus newStatus, CancellationToken cancellationToken = default)
    {
        lock (_lockObject)
        {
            var currentStatus = _memoryCache.Get<CommandExecutionStatus?>(GetStatusKey(commandId)) ?? CommandExecutionStatus.NotFound;
            
            if (currentStatus == expectedStatus)
            {
                SetCommandStatusAsync(commandId, newStatus, cancellationToken);
                return Task.FromResult(true);
            }
            
            return Task.FromResult(false);
        }
    }

    public Task<(CommandExecutionStatus currentStatus, bool wasSet)> GetAndSetStatusAsync(string commandId, CommandExecutionStatus newStatus, CancellationToken cancellationToken = default)
    {
        lock (_lockObject)
        {
            var statusKey = GetStatusKey(commandId);
            var currentStatus = _memoryCache.Get<CommandExecutionStatus?>(statusKey) ?? CommandExecutionStatus.NotFound;
            
            // Set new status
            SetCommandStatusAsync(commandId, newStatus, cancellationToken);
            
            return Task.FromResult((currentStatus, true));
        }
    }

    // Batch operations
    public Task<Dictionary<string, CommandExecutionStatus>> GetMultipleStatusAsync(IEnumerable<string> commandIds, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, CommandExecutionStatus>();
        
        foreach (var commandId in commandIds)
        {
            var statusKey = GetStatusKey(commandId);
            var status = _memoryCache.Get<CommandExecutionStatus?>(statusKey) ?? CommandExecutionStatus.NotFound;
            result[commandId] = status;
        }
        
        return Task.FromResult(result);
    }

    public Task<Dictionary<string, T?>> GetMultipleResultsAsync<T>(IEnumerable<string> commandIds, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, T?>();
        
        foreach (var commandId in commandIds)
        {
            var resultKey = GetResultKey(commandId);
            var value = _memoryCache.Get<T>(resultKey);
            result[commandId] = value;
        }
        
        return Task.FromResult(result);
    }

    // Cleanup operations
    public Task<int> CleanupExpiredCommandsAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTimeOffset.UtcNow.Subtract(maxAge);
        var expiredCommands = _commandTimestamps
            .Where(kvp => kvp.Value < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        var cleanedCount = 0;
        foreach (var commandId in expiredCommands)
        {
            _memoryCache.Remove(GetStatusKey(commandId));
            _memoryCache.Remove(GetResultKey(commandId));
            _commandTimestamps.TryRemove(commandId, out _);
            cleanedCount++;
        }

        if (cleanedCount > 0)
        {
            LoggerCenter.LogCommandCleanupExpired(_logger, cleanedCount, maxAge);
        }

        return Task.FromResult(cleanedCount);
    }

    public Task<int> CleanupCommandsByStatusAsync(CommandExecutionStatus status, TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTimeOffset.UtcNow.Subtract(maxAge);
        var cleanedCount = 0;

        var commandsToCheck = _commandTimestamps
            .Where(kvp => kvp.Value < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var commandId in commandsToCheck)
        {
            var statusKey = GetStatusKey(commandId);
            var currentStatus = _memoryCache.Get<CommandExecutionStatus?>(statusKey);
            
            if (currentStatus == status)
            {
                _memoryCache.Remove(statusKey);
                _memoryCache.Remove(GetResultKey(commandId));
                _commandTimestamps.TryRemove(commandId, out _);
                cleanedCount++;
            }
        }

        if (cleanedCount > 0)
        {
            LoggerCenter.LogCommandCleanupByStatus(_logger, cleanedCount, status, maxAge);
        }

        return Task.FromResult(cleanedCount);
    }

    public Task<Dictionary<CommandExecutionStatus, int>> GetCommandCountByStatusAsync(CancellationToken cancellationToken = default)
    {
        var counts = new Dictionary<CommandExecutionStatus, int>();
        
        foreach (var commandId in _commandTimestamps.Keys.ToList())
        {
            var statusKey = GetStatusKey(commandId);
            var status = _memoryCache.Get<CommandExecutionStatus?>(statusKey);
            
            if (status.HasValue)
            {
                counts[status.Value] = counts.GetValueOrDefault(status.Value, 0) + 1;
            }
        }
        
        return Task.FromResult(counts);
    }

    private static string GetStatusKey(string commandId) => $"cmd_status_{commandId}";
    private static string GetResultKey(string commandId) => $"cmd_result_{commandId}";

    public void Dispose()
    {
        if (!_disposed)
        {
            _commandTimestamps.Clear();
            _disposed = true;
        }
    }
}