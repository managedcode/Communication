# Command Idempotency Store Refactoring

## Overview

The `ICommandIdempotencyStore` has been moved from `AspNetCore` to the main `ManagedCode.Communication` library with a default `IMemoryCache`-based implementation. This provides better separation of concerns and allows for easier usage across different types of applications.

## Key Changes

### ✅ 1. Interface Location
- **Before**: `ManagedCode.Communication.AspNetCore.ICommandIdempotencyStore`
- **After**: `ManagedCode.Communication.Commands.ICommandIdempotencyStore`

### ✅ 2. Default Implementation
- **New**: `MemoryCacheCommandIdempotencyStore` - uses `IMemoryCache` for single-instance scenarios
- **Existing**: `OrleansCommandIdempotencyStore` - for distributed scenarios

### ✅ 3. Service Registration
- **Main Library**: Basic registration without cleanup
- **AspNetCore**: Advanced registration with background cleanup service

## Usage Examples

### Basic Usage (Main Library)

```csharp
// Register services
services.AddCommandIdempotency(); // Uses MemoryCache by default

// Use in your service
public class OrderService
{
    private readonly ICommandIdempotencyStore _store;
    
    public OrderService(ICommandIdempotencyStore store)
    {
        _store = store;
    }
    
    public async Task<OrderResult> ProcessOrderAsync(string orderId)
    {
        return await _store.ExecuteIdempotentAsync($"order-{orderId}", async () =>
        {
            // Your business logic here
            return await ProcessOrderInternally(orderId);
        });
    }
}
```

### Advanced Usage (AspNetCore with Cleanup)

```csharp
// Register with automatic cleanup
services.AddCommandIdempotency<MemoryCacheCommandIdempotencyStore>(options =>
{
    options.CompletedCommandMaxAge = TimeSpan.FromHours(48);
    options.FailedCommandMaxAge = TimeSpan.FromHours(1);
    options.LogHealthMetrics = true;
});
```

### Distributed Scenarios (Orleans)

```csharp
// In Orleans project
services.AddCommandIdempotency<OrleansCommandIdempotencyStore>();
```

### Batch Processing

```csharp
var operations = new[]
{
    ("order-1", () => ProcessOrder("order-1")),
    ("order-2", () => ProcessOrder("order-2")),
    ("order-3", () => ProcessOrder("order-3"))
};

var results = await store.ExecuteBatchIdempotentAsync(operations);
```

## Implementation Details

### MemoryCacheCommandIdempotencyStore Features

- **Thread-Safe**: Uses locks for atomic operations
- **Memory Efficient**: Automatic cache expiration
- **Monitoring**: Command timestamps tracking
- **Cleanup**: Manual and automatic cleanup support

### Key Methods

```csharp
// Basic operations
Task<CommandExecutionStatus> GetCommandStatusAsync(string commandId);
Task SetCommandStatusAsync(string commandId, CommandExecutionStatus status);
Task<T?> GetCommandResultAsync<T>(string commandId);
Task SetCommandResultAsync<T>(string commandId, T result);

// Atomic operations (race condition safe)
Task<bool> TrySetCommandStatusAsync(string commandId, CommandExecutionStatus expected, CommandExecutionStatus newStatus);
Task<(CommandExecutionStatus currentStatus, bool wasSet)> GetAndSetStatusAsync(string commandId, CommandExecutionStatus newStatus);

// Batch operations
Task<Dictionary<string, CommandExecutionStatus>> GetMultipleStatusAsync(IEnumerable<string> commandIds);
Task<Dictionary<string, T?>> GetMultipleResultsAsync<T>(IEnumerable<string> commandIds);

// Cleanup operations
Task<int> CleanupExpiredCommandsAsync(TimeSpan maxAge);
Task<int> CleanupCommandsByStatusAsync(CommandExecutionStatus status, TimeSpan maxAge);
```

## Benefits

### ✅ 1. Better Architecture
- Core interface in main library
- Implementation-specific extensions in separate packages
- Clear separation of concerns

### ✅ 2. Easier Testing
- Lightweight in-memory implementation for unit tests
- No external dependencies for basic scenarios

### ✅ 3. Flexible Deployment
- Single-instance apps: Use `MemoryCacheCommandIdempotencyStore`
- Distributed apps: Use `OrleansCommandIdempotencyStore`
- Custom scenarios: Implement your own `ICommandIdempotencyStore`

### ✅ 4. Backward Compatibility
- All existing extension methods work unchanged
- Same public API surface
- Gradual migration path

## Migration Path

### For Simple Applications
```csharp
// Old
services.AddCommandIdempotency<InMemoryCommandIdempotencyStore>();

// New
services.AddCommandIdempotency(); // Uses MemoryCache by default
```

### For AspNetCore Applications
```csharp
// Keep existing AspNetCore extensions for cleanup functionality
services.AddCommandIdempotency<MemoryCacheCommandIdempotencyStore>(options =>
{
    options.CleanupInterval = TimeSpan.FromMinutes(10);
});
```

### For Orleans Applications
```csharp
// No changes needed - Orleans implementation uses the moved interface
services.AddCommandIdempotency<OrleansCommandIdempotencyStore>();
```

## Summary

The refactoring provides:
- **Cleaner Architecture**: Core functionality in main library
- **Better Defaults**: Memory cache implementation for simple scenarios  
- **Maintained Features**: All advanced features still available in AspNetCore
- **Full Compatibility**: Existing code continues to work

This change makes the command idempotency pattern more accessible and easier to adopt across different types of .NET applications.