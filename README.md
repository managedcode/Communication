# ManagedCode.Communication

Result pattern for .NET that replaces exceptions with type-safe return values. Features railway-oriented programming, ASP.NET Core integration, RFC 7807 Problem Details, and built-in pagination. Designed for production systems requiring explicit error handling without the overhead of throwing exceptions.

[![NuGet](https://img.shields.io/nuget/v/ManagedCode.Communication.svg)](https://www.nuget.org/packages/ManagedCode.Communication/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0)](https://dotnet.microsoft.com/)

## Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Installation](#installation)
- [Logging Configuration](#logging-configuration)
- [Core Concepts](#core-concepts)
- [Quick Start](#quick-start)
- [API Reference](#api-reference)
- [Railway-Oriented Programming](#railway-oriented-programming)
- [Command Pattern and Idempotency](#command-pattern-and-idempotency)
- [Error Handling Patterns](#error-handling-patterns)
- [Integration Guides](#integration-guides)
- [Performance](#performance)
- [Comparison](#comparison)
- [Best Practices](#best-practices)
- [Examples](#examples)
- [Migration Guide](#migration-guide)

## Overview

ManagedCode.Communication brings functional error handling to .NET through the Result pattern. Instead of throwing exceptions, methods return Result types that explicitly indicate success or failure. This approach eliminates hidden control flow, improves performance, and makes error handling a first-class concern in your codebase.

### Why Result Pattern?

Traditional exception handling has several drawbacks:

- **Performance overhead**: Throwing exceptions is expensive
- **Hidden control flow**: Exceptions create invisible exit points in your code
- **Unclear contracts**: Methods don't explicitly declare what errors they might produce
- **Testing complexity**: Exception paths require separate test scenarios

The Result pattern solves these issues by:

- **Explicit error handling**: Errors are part of the method signature
- **Performance**: No exception throwing overhead
- **Composability**: Chain operations using railway-oriented programming
- **Type safety**: Compiler ensures error handling
- **Testability**: All paths are explicit and easy to test

## Key Features

### 🎯 Core Result Types

- **`Result`**: Represents success/failure without a value
- **`Result<T>`**: Represents success with value `T` or failure
- **`CollectionResult<T>`**: Represents collections with built-in pagination
- **`Problem`**: RFC 7807 compliant error details

### 🚂 Railway-Oriented Programming

Complete set of functional combinators for composing operations:

- `Map`: Transform success values
- `Bind` / `Then`: Chain Result-returning operations
- `Tap` / `Do`: Execute side effects
- `Match`: Pattern matching on success/failure
- `Compensate`: Recovery from failures
- `Merge` / `Combine`: Aggregate multiple results

### 🌐 Framework Integration

- **ASP.NET Core**: Automatic HTTP response mapping
- **SignalR**: Hub filters for real-time error handling
- **Microsoft Orleans**: Grain call filters and surrogates
- **Command Pattern**: Built-in command infrastructure with idempotency

### 🛡️ Error Types

Pre-defined error categories with appropriate HTTP status codes:

- Validation errors (400 Bad Request)
- Not Found (404)
- Unauthorized (401)
- Forbidden (403)
- Internal Server Error (500)
- Custom enum-based errors

## Installation

### Package Manager Console

```powershell
# Core library
Install-Package ManagedCode.Communication

# ASP.NET Core integration
Install-Package ManagedCode.Communication.AspNetCore

# Orleans integration
Install-Package ManagedCode.Communication.Orleans
```

### .NET CLI

```bash
# Core library
dotnet add package ManagedCode.Communication

# ASP.NET Core integration
dotnet add package ManagedCode.Communication.AspNetCore

# Orleans integration
dotnet add package ManagedCode.Communication.Orleans
```

### PackageReference

```xml
<PackageReference Include="ManagedCode.Communication" Version="9.6.0" />
<PackageReference Include="ManagedCode.Communication.AspNetCore" Version="9.6.0" />
<PackageReference Include="ManagedCode.Communication.Orleans" Version="9.6.0" />
```

## Logging Configuration

The library includes integrated logging for error scenarios. Configure logging to capture detailed error information:

### ASP.NET Core Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add your logging configuration
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Register other services
builder.Services.AddControllers();

// Configure Communication library - this enables automatic error logging
builder.Services.ConfigureCommunication();

var app = builder.Build();
```

### Console Application Setup

```csharp
var services = new ServiceCollection();

// Add logging
services.AddLogging(builder => 
{
    builder.AddConsole()
           .SetMinimumLevel(LogLevel.Information);
});

// Configure Communication library
services.ConfigureCommunication();

var serviceProvider = services.BuildServiceProvider();
```

The library automatically logs errors in Result factory methods (`From`, `Try`, etc.) with detailed context including file names, line numbers, and method names for easier debugging.

## Core Concepts

### Result Type

The `Result` type represents an operation that can either succeed or fail:

```csharp
public struct Result
{
    public bool IsSuccess { get; }
    public Problem? Problem { get; }
}
```

### Result Type with Value

The generic `Result<T>` includes a value on success:

```csharp
public struct Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Problem? Problem { get; }
}
```

### Problem Type

Implements RFC 7807 Problem Details for HTTP APIs:

```csharp
public class Problem
{
    public string Type { get; set; }
    public string Title { get; set; }
    public int StatusCode { get; set; }
    public string Detail { get; set; }
    public Dictionary<string, object> Extensions { get; set; }
}
```

## Quick Start

### Basic Usage

```csharp
using ManagedCode.Communication;

// Creating Results
var success = Result.Succeed();
var failure = Result.Fail("Operation failed");

// Results with values
var userResult = Result<User>.Succeed(new User { Id = 1, Name = "John" });
var notFound = Result<User>.FailNotFound("User not found");

// Validation errors
var invalid = Result.FailValidation(
    ("email", "Email is required"),
    ("age", "Age must be positive")
);

// From exceptions
try
{
    // risky operation
}
catch (Exception ex)
{
    var error = Result.Fail(ex);
}
```

### Checking Result State

```csharp
if (result.IsSuccess)
{
    // Handle success
}

if (result.IsFailed)
{
    // Handle failure
}

if (result.IsInvalid)
{
    // Handle validation errors
}

// Pattern matching
result.Match(
    onSuccess: () => Console.WriteLine("Success!"),
    onFailure: problem => Console.WriteLine($"Failed: {problem.Detail}")
);
```

## API Reference

### Result Creation Methods

#### Success Methods

```csharp
// Basic success
Result.Succeed()
Result<T>.Succeed(T value)
CollectionResult<T>.Succeed(T[] items, int pageNumber, int pageSize, int totalItems)

// From operations
Result.From(Action action)
Result<T>.From(Func<T> func)
Result<T>.From(Task<T> task)

// Try pattern with exception catching
Result.Try(Action action)
Result<T>.Try(Func<T> func)
```

#### Failure Methods

```csharp
// Basic failures
Result.Fail()
Result.Fail(string title)
Result.Fail(string title, string detail)
Result.Fail(Problem problem)
Result.Fail(Exception exception)

// HTTP status failures
Result.FailNotFound(string detail)
Result.FailUnauthorized(string detail)
Result.FailForbidden(string detail)

// Validation failures
Result.FailValidation(params (string field, string message)[] errors)
Result.Invalid(string message)
Result.Invalid(string field, string message)

// Enum-based failures
Result.Fail<TEnum>(TEnum errorCode) where TEnum : Enum
```

### Transformation Methods

```csharp
// Map: Transform the value
Result<int> ageResult = userResult.Map(user => user.Age);

// Bind: Chain operations that return Results
Result<Order> orderResult = userResult
    .Bind(user => GetUserCart(user.Id))
    .Bind(cart => CreateOrder(cart));

// Tap: Execute side effects
Result<User> result = userResult
    .Tap(user => _logger.LogInfo($"Processing user {user.Id}"))
    .Tap(user => _cache.Set(user.Id, user));
```

### Validation Methods

```csharp
// Ensure: Add validation
Result<User> validUser = userResult
    .Ensure(user => user.Age >= 18, Problem.Create("User must be 18+"))
    .Ensure(user => user.Email.Contains("@"), Problem.Create("Invalid email"));

// Where: Filter with predicate
Result<User> filtered = userResult
    .Where(user => user.IsActive, "User is not active");

// FailIf: Conditional failure
Result<Order> order = orderResult
    .FailIf(o => o.Total <= 0, "Order total must be positive");

// OkIf: Must satisfy condition
Result<Payment> payment = paymentResult
    .OkIf(p => p.IsAuthorized, "Payment not authorized");
```

## Railway-Oriented Programming

Railway-oriented programming treats operations as a series of tracks where success continues on the main track and failures switch to an error track:

### Basic Chaining

```csharp
public Result<Order> ProcessOrder(int userId)
{
    return Result.From(() => GetUser(userId))
        .Then(user => ValidateUser(user))
        .Then(user => GetUserCart(user.Id))
        .Then(cart => ValidateCart(cart))
        .Then(cart => CreateOrder(cart))
        .Then(order => ProcessPayment(order))
        .Then(order => SendConfirmation(order));
}
```

### Async Operations

```csharp
public async Task<Result<Order>> ProcessOrderAsync(int userId)
{
    return await Result.From(() => GetUserAsync(userId))
        .ThenAsync(user => ValidateUserAsync(user))
        .ThenAsync(user => GetUserCartAsync(user.Id))
        .ThenAsync(cart => CreateOrderAsync(cart))
        .ThenAsync(order => ProcessPaymentAsync(order))
        .ThenAsync(order => SendConfirmationAsync(order));
}
```

### Error Recovery

```csharp
var result = await GetPrimaryService()
    .CompensateAsync(async error => 
    {
        _logger.LogWarning($"Primary service failed: {error.Detail}");
        return await GetFallbackService();
    })
    .CompensateWith(defaultValue); // Final fallback
```

### Combining Multiple Results

```csharp
// Merge: Stop at first failure
var result = Result.Merge(
    ValidateName(name),
    ValidateEmail(email),
    ValidateAge(age)
);

// MergeAll: Collect all failures
var result = Result.MergeAll(
    ValidateName(name),
    ValidateEmail(email),
    ValidateAge(age)
); // Returns all validation errors

// Combine: Aggregate values
var combined = Result.Combine(
    GetUserProfile(),
    GetUserSettings(),
    GetUserPermissions()
); // Returns CollectionResult<T>
```

## Command Pattern and Idempotency

### Command Infrastructure

The library includes built-in support for command pattern with distributed idempotency:

```csharp
// Basic command
public class CreateOrderCommand : Command<Order>
{
    public CreateOrderCommand(string orderId, Order order) 
        : base(orderId, "CreateOrder")
    {
        Value = order;
        UserId = "user123";
        CorrelationId = Guid.NewGuid().ToString();
    }
}

// Command with metadata
var command = new Command("command-id", "ProcessPayment")
{
    UserId = "user123",
    SessionId = "session456",
    CorrelationId = "correlation789",
    CausationId = "parent-command-id",
    TraceId = Activity.Current?.TraceId.ToString(),
    SpanId = Activity.Current?.SpanId.ToString()
};
```

### Idempotent Command Execution

#### ASP.NET Core Idempotency

```csharp
// Register idempotency store
builder.Services.AddSingleton<ICommandIdempotencyStore, InMemoryCommandIdempotencyStore>();
// Or use Orleans-based store
builder.Services.AddSingleton<ICommandIdempotencyStore, OrleansCommandIdempotencyStore>();

// Service with idempotent operations
public class PaymentService
{
    private readonly ICommandIdempotencyStore _idempotencyStore;
    
    public async Task<Result<Payment>> ProcessPaymentAsync(ProcessPaymentCommand command)
    {
        // Automatic idempotency - returns cached result if already executed
        return await _idempotencyStore.ExecuteIdempotentAsync(
            command.Id,
            async () =>
            {
                // This code runs only once per command ID
                var payment = await _paymentGateway.ChargeAsync(command.Amount);
                await _repository.SavePaymentAsync(payment);
                return Result<Payment>.Succeed(payment);
            },
            command.Metadata
        );
    }
}
```

#### Orleans-Based Idempotency

```csharp
// Automatic idempotency with Orleans grains
public class OrderGrain : Grain, IOrderGrain
{
    private readonly ICommandIdempotencyStore _idempotencyStore;
    
    public async Task<Result<Order>> CreateOrderAsync(CreateOrderCommand command)
    {
        // Uses ICommandIdempotencyGrain internally for distributed coordination
        return await _idempotencyStore.ExecuteIdempotentAsync(
            command.Id,
            async () =>
            {
                // Guaranteed to execute only once across the cluster
                var order = new Order { /* ... */ };
                await SaveOrderAsync(order);
                return Result<Order>.Succeed(order);
            }
        );
    }
}
```

### Command Execution Status

```csharp
public enum CommandExecutionStatus
{
    NotStarted,    // Command hasn't been processed
    Processing,    // Currently being processed
    Completed,     // Successfully completed
    Failed,        // Processing failed
    Expired        // Result expired from cache
}

// Check command status
var status = await _idempotencyStore.GetCommandStatusAsync("command-id");
if (status == CommandExecutionStatus.Completed)
{
    var result = await _idempotencyStore.GetCommandResultAsync<Order>("command-id");
}
```

## Error Handling Patterns

### Validation Pattern

```csharp
public Result<User> CreateUser(CreateUserDto dto)
{
    // Collect all validation errors
    var errors = new List<(string field, string message)>();
    
    if (string.IsNullOrEmpty(dto.Email))
        errors.Add(("email", "Email is required"));
    
    if (!dto.Email.Contains("@"))
        errors.Add(("email", "Invalid email format"));
    
    if (dto.Age < 0)
        errors.Add(("age", "Age must be positive"));
    
    if (dto.Age < 18)
        errors.Add(("age", "Must be 18 or older"));
    
    if (errors.Any())
        return Result<User>.FailValidation(errors.ToArray());
    
    var user = new User { /* ... */ };
    return Result<User>.Succeed(user);
}
```

### Repository Pattern with Entity Framework

```csharp
public class UserRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserRepository> _logger;
    
    public async Task<Result<User>> GetByIdAsync(int id)
    {
        try
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
            
            if (user == null)
                return Result<User>.FailNotFound($"User {id} not found");
            
            return Result<User>.Succeed(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error getting user {UserId}", id);
            return Result<User>.Fail(ex);
        }
    }
    
    public async Task<CollectionResult<User>> GetPagedAsync(
        int page, 
        int pageSize,
        Expression<Func<User, bool>>? filter = null,
        Expression<Func<User, object>>? orderBy = null)
    {
        try
        {
            // Build query with IQueryable for efficient SQL generation
            IQueryable<User> query = _context.Users.AsNoTracking();
            
            // Apply filter if provided
            if (filter != null)
                query = query.Where(filter);
            
            // Apply ordering
            query = orderBy != null 
                ? query.OrderBy(orderBy) 
                : query.OrderBy(u => u.Id);
            
            // Get total count - generates COUNT(*) SQL query
            var totalItems = await query.CountAsync();
            
            if (totalItems == 0)
                return CollectionResult<User>.Succeed(Array.Empty<User>(), page, pageSize, 0);
            
            // Get page of data - generates SQL with OFFSET and FETCH
            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToArrayAsync();
            
            return CollectionResult<User>.Succeed(users, page, pageSize, totalItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error in GetPagedAsync");
            return CollectionResult<User>.Fail(ex);
        }
    }
    
    // Example with complex query
    public async Task<CollectionResult<UserDto>> SearchUsersAsync(
        string searchTerm,
        int page,
        int pageSize)
    {
        try
        {
            var query = _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive)
                .Where(u => EF.Functions.Like(u.Name, $"%{searchTerm}%") ||
                           EF.Functions.Like(u.Email, $"%{searchTerm}%"));
            
            // Count before projection for efficiency
            var totalItems = await query.CountAsync();
            
            // Project to DTO and paginate - single SQL query
            var users = await query
                .OrderBy(u => u.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    LastLoginDate = u.LastLoginDate
                })
                .ToArrayAsync();
            
            return CollectionResult<UserDto>.Succeed(users, page, pageSize, totalItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed for term: {SearchTerm}", searchTerm);
            return CollectionResult<UserDto>.Fail(ex);
        }
    }
}
```

### Service Layer Pattern

```csharp
public class OrderService
{
    public async Task<Result<Order>> CreateOrderAsync(CreateOrderDto dto)
    {
        // Validate input
        var validationResult = ValidateOrderDto(dto);
        if (validationResult.IsFailed)
            return validationResult;
        
        // Get user
        var userResult = await _userRepo.GetByIdAsync(dto.UserId);
        if (userResult.IsFailed)
            return Result<Order>.Fail(userResult.Problem);
        
        // Check permissions
        var user = userResult.Value;
        if (!user.CanCreateOrders)
            return Result<Order>.FailForbidden("User cannot create orders");
        
        // Create order
        return await Result.Try(async () =>
        {
            var order = new Order
            {
                UserId = user.Id,
                Items = dto.Items,
                Total = CalculateTotal(dto.Items)
            };
            
            await _orderRepo.SaveAsync(order);
            return order;
        });
    }
}
```

## Integration Guides

### ASP.NET Core Integration

#### Installation and Setup

```csharp
// 1. Install NuGet package
// dotnet add package ManagedCode.Communication.AspNetCore

// 2. Program.cs configuration
var builder = WebApplication.CreateBuilder(args);

// Method 1: Simple configuration with auto-detection of environment
builder.AddCommunication(); // ShowErrorDetails = IsDevelopment

// Method 2: Custom configuration
builder.Services.AddCommunication(options =>
{
    options.ShowErrorDetails = true; // Show detailed error messages in responses
});

// 3. Add filters to MVC controllers (ORDER MATTERS!)
builder.Services.AddControllers(options =>
{
    options.AddCommunicationFilters();
    // Filters are applied in this order:
    // 1. CommunicationModelValidationFilter - Catches validation errors first
    // 2. ResultToActionResultFilter - Converts Result to HTTP response
    // 3. CommunicationExceptionFilter - Catches any unhandled exceptions last
});

// 4. Optional: Add filters to SignalR hubs
builder.Services.AddSignalR(options =>
{
    options.AddCommunicationFilters();
});

var app = builder.Build();
```

#### Filter Execution Order

The order of filters is important for proper error handling:

| Order | Filter | Purpose | When It Runs |
|-------|--------|---------|--------------|
| 1 | `CommunicationModelValidationFilter` | Converts ModelState errors to `Result.FailValidation` | Before action execution if model is invalid |
| 2 | `ResultToActionResultFilter` | Maps `Result<T>` return values to HTTP responses | After action execution |
| 3 | `CommunicationExceptionFilter` | Catches unhandled exceptions, returns Problem Details | On any exception |

⚠️ **Important**: The filters must be registered using `AddCommunicationFilters()` to ensure correct ordering. Manual registration may cause unexpected behavior.

#### Controller Implementation

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(User), 200)]
    [ProducesResponseType(typeof(Problem), 404)]
    public async Task<Result<User>> GetUser(int id)
    {
        return await _userService.GetUserAsync(id);
    }
    
    [HttpPost]
    [ProducesResponseType(typeof(User), 201)]
    [ProducesResponseType(typeof(Problem), 400)]
    public async Task<Result<User>> CreateUser([FromBody] CreateUserDto dto)
    {
        return await _userService.CreateUserAsync(dto);
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(CollectionResult<User>), 200)]
    public async Task<CollectionResult<User>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        return await _userService.GetUsersAsync(page, pageSize);
    }
}
```

#### Automatic HTTP Response Mapping

The library automatically converts Result types to appropriate HTTP responses:

| Result State | HTTP Status | Response Body |
|-------------|-------------|---------------|
| `Result.Succeed()` | 204 No Content | Empty |
| `Result<T>.Succeed(value)` | 200 OK | `value` |
| `Result.FailValidation(...)` | 400 Bad Request | Problem Details |
| `Result.FailUnauthorized()` | 401 Unauthorized | Problem Details |
| `Result.FailForbidden()` | 403 Forbidden | Problem Details |
| `Result.FailNotFound()` | 404 Not Found | Problem Details |
| `Result.Fail(...)` | 500 Internal Server Error | Problem Details |

### SignalR Integration

```csharp
public class ChatHub : Hub
{
    public async Task<Result<MessageDto>> SendMessage(string user, string message)
    {
        if (string.IsNullOrEmpty(message))
            return Result<MessageDto>.FailValidation(("message", "Message cannot be empty"));
        
        var messageDto = new MessageDto
        {
            User = user,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
        
        await Clients.All.SendAsync("ReceiveMessage", user, message);
        return Result<MessageDto>.Succeed(messageDto);
    }
    
    public async Task<Result> JoinGroup(string groupName)
    {
        if (string.IsNullOrEmpty(groupName))
            return Result.FailValidation(("groupName", "Group name is required"));
        
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        return Result.Succeed();
    }
}
```

### Microsoft Orleans Integration

#### Setup

```csharp
// Silo configuration
var builder = Host.CreateDefaultBuilder(args)
    .UseOrleans(silo =>
    {
        silo.UseLocalhostClustering()
            .UseOrleansCommunication(); // Required for Result serialization
    });

// Client configuration  
var clientBuilder = Host.CreateDefaultBuilder(args)
    .UseOrleansClient(client =>
    {
        client.UseOrleansCommunication(); // Required for Result serialization
    });
```

That's it! The `UseOrleansCommunication()` extension automatically configures:
- Serialization for all Result types across grain boundaries
- Proper handling of Problem Details in distributed calls
- Support for CollectionResult with pagination

#### Grain Implementation

```csharp
public interface IUserGrain : IGrainWithStringKey
{
    Task<Result<UserState>> GetStateAsync();
    Task<Result> UpdateProfileAsync(UpdateProfileDto dto);
    Task<CollectionResult<Activity>> GetActivitiesAsync(int page, int pageSize);
}

public class UserGrain : Grain, IUserGrain
{
    private readonly IPersistentState<UserState> _state;
    
    public UserGrain([PersistentState("user")] IPersistentState<UserState> state)
    {
        _state = state;
    }
    
    public Task<Result<UserState>> GetStateAsync()
    {
        if (!_state.RecordExists)
            return Task.FromResult(Result<UserState>.FailNotFound("User not found"));
        
        return Task.FromResult(Result<UserState>.Succeed(_state.State));
    }
    
    public async Task<Result> UpdateProfileAsync(UpdateProfileDto dto)
    {
        if (!_state.RecordExists)
            return Result.FailNotFound("User not found");
        
        // Validate
        if (string.IsNullOrEmpty(dto.DisplayName))
            return Result.FailValidation(("displayName", "Display name is required"));
        
        // Update state
        _state.State.DisplayName = dto.DisplayName;
        _state.State.Bio = dto.Bio;
        _state.State.UpdatedAt = DateTime.UtcNow;
        
        await _state.WriteStateAsync();
        return Result.Succeed();
    }
    
    public async Task<CollectionResult<Activity>> GetActivitiesAsync(int page, int pageSize)
    {
        if (!_state.RecordExists)
            return CollectionResult<Activity>.FailNotFound("User not found");
        
        // For real data, use a repository with Entity Framework
        var repository = GrainFactory.GetGrain<IActivityRepositoryGrain>(0);
        return await repository.GetUserActivitiesAsync(this.GetPrimaryKeyString(), page, pageSize);
    }
}
```

## Performance

### Best Practices

1. **Use structs**: `Result` and `Result<T>` are value types (structs) to avoid heap allocation
2. **Avoid boxing**: Use generic methods to prevent boxing of value types
3. **Chain operations**: Use railway-oriented programming to avoid intermediate variables
4. **Async properly**: Use `ConfigureAwait(false)` in library code
5. **Cache problems**: Reuse common Problem instances for frequent errors

## Testing

The repository uses xUnit with [Shouldly](https://github.com/shouldly/shouldly) for assertions. Shared matchers such as `ShouldBeEquivalentTo` and `AssertProblem()` live in `ManagedCode.Communication.Tests/TestHelpers`, keeping tests fluent without FluentAssertions.

- Run the full suite: `dotnet test ManagedCode.Communication.Tests/ManagedCode.Communication.Tests.csproj`
- Generate lcov coverage: `dotnet test ManagedCode.Communication.Tests/ManagedCode.Communication.Tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=lcov`

Execution helpers (`Result.From`, `Result<T>.From`, task/value-task shims) and the command metadata extensions now have direct tests, pushing the core assembly above 80% line coverage. Mirror those patterns when adding APIs—exercise both success and failure paths and prefer invoking the public fluent surface instead of internal helpers.

## Comparison

### Comparison with Other Libraries

| Feature | ManagedCode.Communication | FluentResults | CSharpFunctionalExtensions | ErrorOr |
|---------|--------------------------|---------------|---------------------------|---------|
| **Multiple Errors** | ✅ Yes | ✅ Yes | ❌ No | ✅ Yes |
| **Railway-Oriented** | ✅ Full | ✅ Full | ✅ Full | ⚠️ Limited |
| **HTTP Integration** | ✅ Built-in | ❌ No | ⚠️ Extension | ❌ No |
| **Orleans Support** | ✅ Built-in | ❌ No | ❌ No | ❌ No |
| **SignalR Support** | ✅ Built-in | ❌ No | ❌ No | ❌ No |
| **RFC 7807** | ✅ Full | ❌ No | ❌ No | ❌ No |
| **Pagination** | ✅ Built-in | ❌ No | ❌ No | ❌ No |
| **Command Pattern** | ✅ Built-in | ❌ No | ❌ No | ❌ No |
| **Performance** | ✅ Struct-based | ❌ Class-based | ✅ Struct-based | ✅ Struct-based |
| **Async Support** | ✅ Full | ✅ Full | ✅ Full | ✅ Full |

### When to Use ManagedCode.Communication

Choose this library when you need:

- **Full-stack integration**: ASP.NET Core + SignalR + Orleans
- **Standardized errors**: RFC 7807 Problem Details
- **Pagination**: Built-in collection results with paging
- **Command pattern**: Command infrastructure with idempotency
- **Performance**: Struct-based implementation for minimal overhead

## Best Practices

### DO ✅

```csharp
// DO: Use Result for operations that can fail
public Result<User> GetUser(int id)
{
    var user = _repository.FindById(id);
    return user != null 
        ? Result<User>.Succeed(user)
        : Result<User>.FailNotFound($"User {id} not found");
}

// DO: Chain operations using railway-oriented programming
public Result<Order> ProcessOrder(OrderDto dto)
{
    return ValidateOrder(dto)
        .Then(CreateOrder)
        .Then(CalculateTotals)
        .Then(ApplyDiscounts)
        .Then(SaveOrder);
}

// DO: Provide specific error information
public Result ValidateEmail(string email)
{
    if (string.IsNullOrEmpty(email))
        return Result.FailValidation(("email", "Email is required"));
    
    if (!email.Contains("@"))
        return Result.FailValidation(("email", "Invalid email format"));
    
    return Result.Succeed();
}

// DO: Use CollectionResult for paginated data
public CollectionResult<Product> GetProducts(int page, int pageSize)
{
    var products = _repository.GetPaged(page, pageSize);
    var total = _repository.Count();
    return CollectionResult<Product>.Succeed(products, page, pageSize, total);
}
```

### DON'T ❌

```csharp
// DON'T: Throw exceptions from Result-returning methods
public Result<User> GetUser(int id)
{
    if (id <= 0)
        throw new ArgumentException("Invalid ID"); // ❌ Don't throw
    
    // Instead:
    if (id <= 0)
        return Result<User>.FailValidation(("id", "ID must be positive")); // ✅
}

// DON'T: Ignore Result values
var result = UpdateUser(user); // ❌ Result ignored
DoSomethingElse();

// Instead:
var result = UpdateUser(user);
if (result.IsFailed)
    return result; // ✅ Handle the failure

// DON'T: Mix Result and exceptions
public async Task<User> GetUserMixed(int id)
{
    var result = await GetUserAsync(id);
    if (result.IsFailed)
        throw new Exception(result.Problem.Detail); // ❌ Mixing patterns
    
    return result.Value;
}

// DON'T: Create generic error messages
return Result.Fail("Error"); // ❌ Too vague

// Instead:
return Result.Fail("User creation failed", "Email already exists"); // ✅
```

## Examples

### Complete Web API Example

```csharp
// Domain Model
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

// Service Interface
public interface IProductService
{
    Task<Result<Product>> GetByIdAsync(int id);
    Task<Result<Product>> CreateAsync(CreateProductDto dto);
    Task<Result> UpdateStockAsync(int id, int quantity);
    Task<CollectionResult<Product>> SearchAsync(string query, int page, int pageSize);
}

// Service Implementation
public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ProductService> _logger;
    
    public async Task<Result<Product>> GetByIdAsync(int id)
    {
        return await Result.Try(async () =>
        {
            var product = await _repository.FindByIdAsync(id);
            return product ?? throw new KeyNotFoundException($"Product {id} not found");
        })
        .CompensateAsync(async error =>
        {
            _logger.LogWarning("Product {Id} not found, checking archive", id);
            var archived = await _repository.FindInArchiveAsync(id);
            return archived != null
                ? Result<Product>.Succeed(archived)
                : Result<Product>.FailNotFound($"Product {id} not found");
        });
    }
    
    public async Task<Result<Product>> CreateAsync(CreateProductDto dto)
    {
        // Validation
        var validationResult = await ValidateProductDto(dto);
        if (validationResult.IsFailed)
            return Result<Product>.Fail(validationResult.Problem);
        
        // Check for duplicates
        var existing = await _repository.FindByNameAsync(dto.Name);
        if (existing != null)
            return Result<Product>.Fail("Duplicate product", 
                $"Product with name '{dto.Name}' already exists");
        
        // Create product
        var product = new Product
        {
            Name = dto.Name,
            Price = dto.Price,
            Stock = dto.InitialStock
        };
        
        await _repository.AddAsync(product);
        await _repository.SaveChangesAsync();
        
        return Result<Product>.Succeed(product);
    }
    
    public async Task<Result> UpdateStockAsync(int id, int quantity)
    {
        return await GetByIdAsync(id)
            .Then(product =>
            {
                if (product.Stock + quantity < 0)
                    return Result.Fail("Insufficient stock", 
                        $"Cannot reduce stock by {Math.Abs(quantity)}. Current stock: {product.Stock}");
                
                product.Stock += quantity;
                return Result.Succeed();
            })
            .ThenAsync(async () =>
            {
                await _repository.SaveChangesAsync();
                return Result.Succeed();
            });
    }
    
    public async Task<CollectionResult<Product>> SearchAsync(string query, int page, int pageSize)
    {
        try
        {
            var (products, total) = await _repository.SearchAsync(query, page, pageSize);
            return CollectionResult<Product>.Succeed(products, page, pageSize, total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed for query: {Query}", query);
            return CollectionResult<Product>.Fail(ex);
        }
    }
    
    private async Task<Result> ValidateProductDto(CreateProductDto dto)
    {
        var errors = new List<(string field, string message)>();
        
        if (string.IsNullOrWhiteSpace(dto.Name))
            errors.Add(("name", "Product name is required"));
        else if (dto.Name.Length > 100)
            errors.Add(("name", "Product name must be 100 characters or less"));
        
        if (dto.Price <= 0)
            errors.Add(("price", "Price must be greater than zero"));
        
        if (dto.InitialStock < 0)
            errors.Add(("initialStock", "Initial stock cannot be negative"));
        
        // Async validation
        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            var categoryExists = await _repository.CategoryExistsAsync(dto.CategoryId);
            if (!categoryExists)
                errors.Add(("categoryId", "Invalid category"));
        }
        
        return errors.Any() 
            ? Result.FailValidation(errors.ToArray())
            : Result.Succeed();
    }
}

// Controller
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    
    [HttpGet("{id}")]
    public async Task<Result<Product>> Get(int id)
    {
        return await _productService.GetByIdAsync(id);
    }
    
    [HttpPost]
    public async Task<Result<Product>> Create([FromBody] CreateProductDto dto)
    {
        return await _productService.CreateAsync(dto);
    }
    
    [HttpPatch("{id}/stock")]
    public async Task<Result> UpdateStock(int id, [FromBody] UpdateStockDto dto)
    {
        return await _productService.UpdateStockAsync(id, dto.Quantity);
    }
    
    [HttpGet("search")]
    public async Task<CollectionResult<Product>> Search(
        [FromQuery] string q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        return await _productService.SearchAsync(q, page, pageSize);
    }
}
```

### Complex Business Logic Example

```csharp
public class OrderProcessingService
{
    public async Task<Result<Order>> ProcessOrderAsync(ProcessOrderCommand command)
    {
        // Complete order processing pipeline
        return await Result
            // Validate command
            .From(() => ValidateCommand(command))
            
            // Load user
            .ThenAsync(async () => await _userRepository.GetByIdAsync(command.UserId))
            
            // Check user permissions
            .Then(user => user.CanPlaceOrders 
                ? Result<User>.Succeed(user)
                : Result<User>.FailForbidden("User cannot place orders"))
            
            // Verify user credit
            .ThenAsync(async user => await _creditService.CheckCreditAsync(user.Id))
            .Then(creditResult => creditResult.AvailableCredit >= command.TotalAmount
                ? Result.Succeed()
                : Result.Fail("Insufficient credit"))
            
            // Check inventory
            .ThenAsync(async () => await CheckInventoryAsync(command.Items))
            
            // Reserve inventory
            .ThenAsync(async () => await ReserveInventoryAsync(command.Items))
            
            // Create order
            .ThenAsync(async () => await CreateOrderAsync(command))
            
            // Process payment
            .ThenAsync(async order => await ProcessPaymentAsync(order, command.PaymentMethod))
            
            // Send confirmation
            .ThenAsync(async order => await SendOrderConfirmationAsync(order))
            
            // Handle any failures
            .CompensateAsync(async problem =>
            {
                _logger.LogError("Order processing failed: {Problem}", problem.Detail);
                
                // Rollback inventory reservation
                await ReleaseInventoryAsync(command.Items);
                
                // Notify user
                await _notificationService.NotifyOrderFailedAsync(command.UserId, problem.Detail);
                
                return Result<Order>.Fail(problem);
            });
    }
    
    private async Task<Result> CheckInventoryAsync(List<OrderItem> items)
    {
        var unavailable = new List<string>();
        
        foreach (var item in items)
        {
            var stock = await _inventoryService.GetStockAsync(item.ProductId);
            if (stock < item.Quantity)
            {
                unavailable.Add($"{item.ProductName}: requested {item.Quantity}, available {stock}");
            }
        }
        
        return unavailable.Any()
            ? Result.Fail("Insufficient inventory", string.Join("; ", unavailable))
            : Result.Succeed();
    }
}
```

## Migration Guide

### Migrating from Exceptions

#### Before (Exception-based)

```csharp
public User GetUser(int id)
{
    if (id <= 0)
        throw new ArgumentException("Invalid ID");
    
    var user = _repository.FindById(id);
    if (user == null)
        throw new NotFoundException($"User {id} not found");
    
    if (!user.IsActive)
        throw new InvalidOperationException("User is not active");
    
    return user;
}

// Usage
try
{
    var user = GetUser(id);
    // Process user
}
catch (ArgumentException ex)
{
    // Handle validation error
}
catch (NotFoundException ex)
{
    // Handle not found
}
catch (Exception ex)
{
    // Handle other errors
}
```

#### After (Result-based)

```csharp
public Result<User> GetUser(int id)
{
    if (id <= 0)
        return Result<User>.FailValidation(("id", "ID must be positive"));
    
    var user = _repository.FindById(id);
    if (user == null)
        return Result<User>.FailNotFound($"User {id} not found");
    
    if (!user.IsActive)
        return Result<User>.Fail("User inactive", "User account is not active");
    
    return Result<User>.Succeed(user);
}

// Usage
var result = GetUser(id);
result.Match(
    onSuccess: user => { /* Process user */ },
    onFailure: problem =>
    {
        if (result.IsInvalid)
        {
            // Handle validation error
        }
        else if (problem.StatusCode == 404)
        {
            // Handle not found
        }
        else
        {
            // Handle other errors
        }
    }
);
```

### Gradual Migration Strategy

1. **Start with new code**: Implement Result pattern in new features
2. **Wrap existing methods**: Use `Result.Try()` to wrap exception-throwing code
3. **Update interfaces**: Change return types from `T` to `Result<T>`
4. **Convert controllers**: Update API endpoints to return Result types
5. **Remove try-catch blocks**: Replace with Result pattern handling

```csharp
// Step 1: Wrap existing code
public Result<User> GetUserSafe(int id)
{
    return Result.Try(() => GetUserUnsafe(id));
}

// Step 2: Gradually refactor internals
public Result<User> GetUserRefactored(int id)
{
    // Refactored implementation without exceptions
}

// Step 3: Update consumers
public async Task<IActionResult> GetUser(int id)
{
    var result = await _service.GetUserRefactored(id);
    return result.Match(
        onSuccess: user => Ok(user),
        onFailure: problem => Problem(problem)
    );
}
```

## Contributing

Contributions are welcome! Fork the repository and submit a pull request.

### Development Setup

```bash
# Clone the repository
git clone https://github.com/managed-code-hub/Communication.git

# Build the solution
dotnet build

# Run tests
dotnet test

# Run benchmarks
dotnet run -c Release --project benchmarks/ManagedCode.Communication.Benchmarks
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- **Issues**: [GitHub Issues](https://github.com/managed-code-hub/Communication/issues)
- **Source Code**: [GitHub Repository](https://github.com/managed-code-hub/Communication)

## Acknowledgments

- Inspired by F# and Rust Result types
- Railway-oriented programming concepts
- RFC 7807 Problem Details for HTTP APIs
- Built for seamless integration with Microsoft Orleans
- Optimized for ASP.NET Core applications
