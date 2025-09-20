# Command Idempotency Store Improvements

## Overview

The `ICommandIdempotencyStore` interface and its implementations have been significantly improved to address concurrency issues, performance bottlenecks, and memory management concerns.

## Problems Solved

### ✅ 1. Race Conditions Fixed

**Problem**: Race conditions between checking status and setting status
**Solution**: Added atomic operations

```csharp
// NEW: Atomic compare-and-swap operations
Task<bool> TrySetCommandStatusAsync(string commandId, CommandExecutionStatus expectedStatus, CommandExecutionStatus newStatus);
Task<(CommandExecutionStatus currentStatus, bool wasSet)> GetAndSetStatusAsync(string commandId, CommandExecutionStatus newStatus);
```

**Usage in Extensions**:
```csharp
// OLD: Race condition prone
var status = await store.GetCommandStatusAsync(commandId);
await store.SetCommandStatusAsync(commandId, CommandExecutionStatus.InProgress);

// NEW: Atomic operation
var (currentStatus, wasSet) = await store.GetAndSetStatusAsync(commandId, CommandExecutionStatus.InProgress);
```

### ✅ 2. Batch Operations Added

**Problem**: No batching support - each command processed separately
**Solution**: Batch operations for better performance

```csharp
// NEW: Batch operations
Task<Dictionary<string, CommandExecutionStatus>> GetMultipleStatusAsync(IEnumerable<string> commandIds);
Task<Dictionary<string, T?>> GetMultipleResultsAsync<T>(IEnumerable<string> commandIds);

// NEW: Batch execution extension
Task<Dictionary<string, T>> ExecuteBatchIdempotentAsync<T>(
    IEnumerable<(string commandId, Func<Task<T>> operation)> operations);
```

**Usage Example**:
```csharp
var operations = new[]
{
    ("cmd1", () => ProcessOrder1()),
    ("cmd2", () => ProcessOrder2()),
    ("cmd3", () => ProcessOrder3())
};

var results = await store.ExecuteBatchIdempotentAsync(operations);
```

### ✅ 3. Memory Leak Prevention

**Problem**: No automatic cleanup of old commands
**Solution**: Comprehensive cleanup system

```csharp
// NEW: Cleanup operations
Task<int> CleanupExpiredCommandsAsync(TimeSpan maxAge);
Task<int> CleanupCommandsByStatusAsync(CommandExecutionStatus status, TimeSpan maxAge);
Task<Dictionary<CommandExecutionStatus, int>> GetCommandCountByStatusAsync();
```

**Automatic Cleanup Service**:
```csharp
// NEW: Background cleanup service
services.AddCommandIdempotency<InMemoryCommandIdempotencyStore>(options =>
{
    options.CleanupInterval = TimeSpan.FromMinutes(10);
    options.CompletedCommandMaxAge = TimeSpan.FromHours(24);
    options.FailedCommandMaxAge = TimeSpan.FromHours(1);
    options.InProgressCommandMaxAge = TimeSpan.FromMinutes(30);
});
```

### ✅ 4. Simplified Implementation

**Problem**: Complex retry logic and polling
**Solution**: Simplified with better defaults

```csharp
// NEW: Improved retry with jitter
public static async Task<T> ExecuteIdempotentWithRetryAsync<T>(
    this ICommandIdempotencyStore store,
    string commandId,
    Func<Task<T>> operation,
    int maxRetries = 3,
    TimeSpan? baseDelay = null)
{
    // Exponential backoff with jitter to prevent thundering herd
    var delay = TimeSpan.FromMilliseconds(
        baseDelay.Value.TotalMilliseconds * Math.Pow(2, retryCount - 1) * 
        (0.8 + Random.Shared.NextDouble() * 0.4)); // Jitter: 80%-120%
}
```

**Adaptive Polling**:
```csharp
// NEW: Adaptive polling - starts fast, slows down
private static async Task<T> WaitForCompletionAsync<T>(...)
{
    var pollInterval = TimeSpan.FromMilliseconds(10); // Start fast
    const int maxInterval = 1000; // Max 1 second
    
    // Exponential backoff for polling
    pollInterval = TimeSpan.FromMilliseconds(
        Math.Min(pollInterval.TotalMilliseconds * 1.5, maxInterval));
}
```

## New Features

### 🎯 Health Monitoring

```csharp
var metrics = await store.GetHealthMetricsAsync();
Console.WriteLine($"Total: {metrics.TotalCommands}, Failed: {metrics.FailureRate:F1}%");
```

### 🎯 Easy Service Registration

```csharp
// Simple registration with automatic cleanup
services.AddCommandIdempotency<InMemoryCommandIdempotencyStore>();

// Custom cleanup configuration
services.AddCommandIdempotency<RedisCommandIdempotencyStore>(options =>
{
    options.CompletedCommandMaxAge = TimeSpan.FromHours(48);
    options.LogHealthMetrics = true;
});
```

### 🎯 Orleans Integration Enhancements

The Orleans implementation now supports all new operations:
- Atomic operations leveraging Orleans grain concurrency model
- Batch operations using Task.WhenAll for parallel grain calls
- Automatic cleanup (no-op since Orleans handles grain lifecycle)

## Performance Improvements

### Before:
- Race conditions causing duplicate executions
- Individual calls for each command check
- No cleanup - memory grows indefinitely  
- 5-minute polling timeout (too long)
- Fixed retry intervals causing thundering herd

### After:
- ✅ Atomic operations prevent race conditions
- ✅ Batch operations reduce round trips
- ✅ Automatic cleanup prevents memory leaks
- ✅ 30-second polling timeout (more reasonable)
- ✅ Exponential backoff with jitter prevents thundering herd
- ✅ Adaptive polling (starts fast, slows down)

## Breaking Changes

### ❌ None - Fully Backward Compatible

All existing code continues to work without changes. New features are additive.

## Usage Examples

### Basic Usage (Unchanged)
```csharp
var result = await store.ExecuteIdempotentAsync("cmd-123", async () =>
{
    return await ProcessPayment();
});
```

### New Batch Processing
```csharp
var batchOperations = orders.Select(order => 
    (order.Id, () => ProcessOrder(order)));

var results = await store.ExecuteBatchIdempotentAsync(batchOperations);
```

### Health Monitoring
```csharp
var metrics = await store.GetHealthMetricsAsync();
if (metrics.StuckCommandsPercentage > 10)
{
    logger.LogWarning("High percentage of stuck commands: {Percentage}%", 
        metrics.StuckCommandsPercentage);
}
```

### Manual Cleanup
```csharp
// Clean up commands older than 1 hour
var cleanedCount = await store.AutoCleanupAsync(
    completedCommandMaxAge: TimeSpan.FromHours(1),
    failedCommandMaxAge: TimeSpan.FromMinutes(30));
```

## Recommendations

1. **Use automatic cleanup** for production deployments
2. **Monitor health metrics** to detect issues early
3. **Use batch operations** when processing multiple commands
4. **Configure appropriate timeout values** based on your operations
5. **Consider Orleans implementation** for distributed scenarios

## Migration Path

1. ✅ **No immediate action required** - everything works as before
2. ✅ **Add cleanup service** when convenient:
   ```csharp
   services.AddCommandIdempotency<YourStore>();
   ```
3. ✅ **Use batch operations** for new high-volume scenarios
4. ✅ **Monitor health metrics** for operational insights

The improvements provide a production-ready, scalable command idempotency solution while maintaining full backward compatibility.