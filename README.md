# ManagedCode.Communication

[![.NET](https://github.com/managedcode/Communication/actions/workflows/dotnet.yml/badge.svg)](https://github.com/managedcode/Communication/actions/workflows/dotnet.yml)
[![Coverage Status](https://coveralls.io/repos/github/managedcode/Communication/badge.svg?branch=main&service=github)](https://coveralls.io/github/managedcode/Communication?branch=main)
[![nuget](https://github.com/managedcode/Communication/actions/workflows/nuget.yml/badge.svg?branch=main)](https://github.com/managedcode/Communication/actions/workflows/nuget.yml)
[![CodeQL](https://github.com/managedcode/Communication/actions/workflows/codeql-analysis.yml/badge.svg?branch=main)](https://github.com/managedcode/Communication/actions/workflows/codeql-analysis.yml)
[![NuGet Package](https://img.shields.io/nuget/v/ManagedCode.Communication.svg)](https://www.nuget.org/packages/ManagedCode.Communication)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ManagedCode.Communication.svg)](https://www.nuget.org/packages/ManagedCode.Communication)

> A powerful .NET library implementing the Result pattern with RFC 7807 Problem Details support for C# and ASP.NET Core applications. Replace exceptions with type-safe Result objects, making your error handling more predictable, testable, and maintainable. Perfect for building robust APIs with standardized error responses.

## 🎯 Why ManagedCode.Communication?

Traditional exception-based error handling in .NET and C# applications can make code difficult to follow and test. The Communication library introduces a **Result pattern** implementation with **Problem Details (RFC 7807)** support that transforms how you handle operations that might fail in ASP.NET Core, Orleans, and other .NET applications:

- ✅ **No More Exceptions** - Replace try-catch blocks with elegant Result objects
- 🔍 **Explicit Error Handling** - Makes potential failures visible in method signatures
- 🧪 **Better Testability** - No need to test exception scenarios
- 🚀 **Improved Performance** - Avoid the overhead of throwing exceptions
- 📝 **Self-Documenting Code** - Method signatures clearly indicate possible failures
- 🌐 **RFC 7807 Compliant** - Standardized error responses for APIs
- 🔄 **Railway-Oriented Programming** - Functional programming style with Bind, Map, Tap, and Match methods for C#
- 🎭 **Exception Recovery** - Convert between exceptions and Results seamlessly

## 📦 Installation

```bash
# Core library
dotnet add package ManagedCode.Communication

# ASP.NET Core integration
dotnet add package ManagedCode.Communication.Extensions

# Orleans integration
dotnet add package ManagedCode.Communication.Orleans
```

## 🚀 Quick Start

### Basic Usage

```csharp
using ManagedCode.Communication;

// Simple success result
var success = Result.Succeed();
if (success.IsSuccess)
{
    Console.WriteLine("Operation succeeded!");
}

// Failure with Problem Details
var failure = Result.Fail("Operation failed", "Details about the failure");
if (failure.IsFailed)
{
    Console.WriteLine($"Error: {failure.Problem.Title} - {failure.Problem.Detail}");
}

// Different ways to create failures
var basicFail = Result.Fail(); // Simple failure
var withMessage = Result.Fail("Something went wrong");
var withDetails = Result.Fail("Operation failed", "Detailed error description");
var withStatus = Result.Fail("Not Found", "Resource does not exist", HttpStatusCode.NotFound);
var notFound = Result.FailNotFound("User not found");
var validation = Result.FailValidation(("field", "Field is required"));

// Try to get the problem details
if (failure.TryGetProblem(out var problem))
{
    Console.WriteLine($"Status: {problem.StatusCode}, Type: {problem.Type}");
}

// Throw exception if failed (when you need to integrate with exception-based code)
failure.ThrowIfFail(); // Throws ProblemException
```

### Generic Results with Values

```csharp
// Success with value
var userResult = Result<User>.Succeed(new User { Id = 1, Name = "John" });
if (userResult.IsSuccess)
{
    var user = userResult.Value; // Access the user object
    Console.WriteLine($"Found user: {user.Name}");
}

// Failure with Problem Details
var notFound = Result<User>.FailNotFound("User not found");
if (notFound.IsFailed)
{
    Console.WriteLine($"Error: {notFound.Problem.Title} (Status: {notFound.Problem.StatusCode})");
}

// Using Try pattern for exception-prone operations
var result = Result.Try(() => 
{
    return JsonSerializer.Deserialize<User>(jsonString);
});
```

### Collection Results

Perfect for paginated API responses:

```csharp
var products = await GetProductsAsync(page: 1, pageSize: 20);

var result = CollectionResult<Product>.Succeed(
    items: products,
    pageNumber: 1,
    pageSize: 20,
    totalItems: 150
);

// Access pagination info
Console.WriteLine($"Page {result.PageNumber} of {result.TotalPages}");
Console.WriteLine($"Showing {result.Collection.Count()} of {result.TotalItems} products");
```

### Problem Details (RFC 7807)

The library fully implements RFC 7807 Problem Details for standardized error responses:

```csharp
// Create a problem with all details
var problem = Problem.Create(
    type: "https://example.com/probs/out-of-credit",
    title: "You do not have enough credit",
    statusCode: 403,
    detail: "Your current balance is 30, but that costs 50.",
    instance: "/account/12345/msgs/abc"
);

// Add custom extensions
problem.Extensions["balance"] = 30;
problem.Extensions["accounts"] = new[] { "/account/12345", "/account/67890" };

// Convert to Result
var result = Result.Fail(problem);

// Create Problem from exception
var exception = new InvalidOperationException("Operation not allowed");
var problemFromException = Problem.FromException(exception);

// Create Problem from enum
public enum ApiError { InvalidInput, Unauthorized, RateLimitExceeded }
var problemFromEnum = Problem.FromEnum(ApiError.RateLimitExceeded, "Too many requests", 429);

// Validation problems
var validationResult = Result.FailValidation(
    ("email", "Email is required"),
    ("email", "Email format is invalid"),
    ("age", "Age must be greater than 18")
);

// Access validation errors
if (validationResult.Problem.GetValidationErrors() is var errors && errors != null)
{
    foreach (var error in errors)
    {
        Console.WriteLine($"{error.Key}: {string.Join(", ", error.Value)}");
    }
}
```

### Railway-Oriented Programming

Chain operations elegantly:

```csharp
var result = await GetUserAsync(userId)
    .BindAsync(user => ValidateUserAsync(user))
    .MapAsync(user => EnrichUserDataAsync(user))
    .TapAsync(user => LogUserAccessAsync(user))
    .Match(
        onSuccess: user => Ok(user),
        onFailure: problem => problem.StatusCode switch
        {
            404 => NotFound(problem),
            403 => Forbid(problem),
            _ => BadRequest(problem)
        }
    );
```

### Exception Interoperability

The library provides seamless conversion between .NET exceptions and Result types, making it easy to integrate with existing codebases:

```csharp
// Convert exception to Problem
var exception = new InvalidOperationException("Operation not allowed");
var problem = Problem.FromException(exception);

// Convert Problem back to exception
var reconstructedException = problem.ToException();
// If original was InvalidOperationException, it returns InvalidOperationException
// Otherwise returns ProblemException

// Use with Result
var result = Result.Fail(exception); // Automatically converts to Problem
result.ThrowIfFail(); // Throws the appropriate exception type
```

## 🌐 ASP.NET Core Integration

### Configure Services

```csharp
using ManagedCode.Communication.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Communication services
builder.Services.AddCommunication(options =>
{
    options.ShowErrorDetails = builder.Environment.IsDevelopment();
});

// Add MVC with Communication filters
builder.Services.AddControllers(options =>
{
    options.AddCommunicationFilters();
});

// Add SignalR with Communication filters
builder.Services.AddSignalR(options => 
{
    options.AddCommunicationHubFilter();
});

var app = builder.Build();

// Use Communication middleware for global error handling
app.UseCommunication();
```

### Controller Examples

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("{id}")]
    public async Task<Result<UserDto>> GetUser(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        
        if (user == null)
            return Result<UserDto>.FailNotFound($"User with ID {id} not found");
            
        return Result<UserDto>.Succeed(user.ToDto());
    }

    [HttpPost]
    public async Task<Result<UserDto>> CreateUser([FromBody] CreateUserDto dto)
    {
        // Model validation is handled automatically by filters
        var validationResult = await _userService.ValidateAsync(dto);
        if (validationResult.IsFailed)
            return validationResult;

        var user = await _userService.CreateAsync(dto);
        return Result<UserDto>.Succeed(user.ToDto());
    }

    [HttpGet]
    public async Task<CollectionResult<UserDto>> GetUsers(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        var (users, totalCount) = await _userService.GetPagedAsync(page, pageSize);
        
        return CollectionResult<UserDto>.Succeed(
            users.Select(u => u.ToDto()),
            page,
            pageSize,
            totalCount
        );
    }
}
```

### SignalR Hub Example

```csharp
public class NotificationHub : Hub
{
    private readonly INotificationService _notificationService;

    public NotificationHub(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<Result> SendNotification(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return Result.FailValidation(("message", "Message cannot be empty"));

        await _notificationService.BroadcastAsync(message);
        return Result.Succeed();
    }

    public async Task<Result<int>> GetUnreadCount()
    {
        var count = await _notificationService.GetUnreadCountAsync(Context.UserIdentifier);
        return Result<int>.Succeed(count);
    }
}
```

## 🎨 Advanced Features

### Custom Error Enums

Define domain-specific errors:

```csharp
public enum OrderError
{
    InsufficientInventory,
    PaymentFailed,
    ShippingNotAvailable
}

// Use with Result
var result = Result.Fail(
    OrderError.InsufficientInventory, 
    "Not enough items in stock"
);

// Check specific error
if (result.Problem?.HasErrorCode(OrderError.InsufficientInventory) == true)
{
    // Handle inventory error
}
```

### Problem and ProblemDetails Conversion

Seamless integration with ASP.NET Core's ProblemDetails:

```csharp
// Convert between Problem and ProblemDetails
Problem problem = Result.Fail("Error", "Details").Problem;
ProblemDetails problemDetails = problem.ToProblemDetails();

// Convert back
Problem convertedProblem = problemDetails.AsProblem();

// Create Result from ProblemDetails
Result result = problemDetails.ToFailedResult();
```

### Try Pattern for Exception Handling

Wrap exception-throwing code elegantly:

```csharp
// Synchronous
var result = Result.Try(() =>
{
    var config = JsonSerializer.Deserialize<Config>(json);
    ValidateConfig(config);
    return config;
});

// Asynchronous
var asyncResult = await Result.TryAsync(async () =>
{
    var data = await httpClient.GetStringAsync(url);
    return JsonSerializer.Deserialize<Data>(data);
}, HttpStatusCode.BadGateway);

// With specific value type
var parseResult = Result.Try<int>(() => int.Parse(userInput));
```

### Result Extensions and Chaining

```csharp
// Map successful results
var result = await GetUserAsync(id)
    .Map(user => user.ToDto())
    .Map(dto => new UserViewModel(dto));

// Bind operations (flatMap)
var finalResult = await GetUserAsync(userId)
    .BindAsync(user => ValidateUserAsync(user))
    .BindAsync(user => CreateOrderForUserAsync(user, orderDto))
    .MapAsync(order => order.ToDto());

// Tap for side effects
var resultWithLogging = await ProcessOrderAsync(orderId)
    .TapAsync(order => LogOrderProcessed(order))
    .TapAsync(order => SendNotificationAsync(order));

// Pattern matching
var message = result.Match(
    onSuccess: value => $"Success: {value}",
    onFailure: problem => $"Error {problem.StatusCode}: {problem.Detail}"
);
```

### Entity Framework Integration

```csharp
public async Task<Result<Customer>> GetCustomerAsync(int id)
{
    return await Result.TryAsync(async () =>
    {
        var customer = await _dbContext.Customers
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Id == id);
            
        return customer ?? Result<Customer>.FailNotFound($"Customer {id} not found");
    });
}

public async Task<Result> UpdateCustomerAsync(Customer customer)
{
    return await Result.TryAsync(async () =>
    {
        _dbContext.Customers.Update(customer);
        await _dbContext.SaveChangesAsync();
    }, HttpStatusCode.InternalServerError);
}
```

## 🏗️ Orleans Integration

```csharp
// Silo configuration
var builder = new HostBuilder()
    .UseOrleans(siloBuilder =>
    {
        siloBuilder.UseOrleansCommunication();
    });

// Client configuration
var client = new ClientBuilder()
    .UseOrleansCommunication()
    .Build();

// Grain implementation
public class UserGrain : Grain, IUserGrain
{
    public async Task<Result<UserData>> GetUserDataAsync()
    {
        return await Result.TryAsync(async () =>
        {
            var userData = await LoadUserDataAsync();
            return userData;
        });
    }
}
```

## 📊 Performance Benefits

The Result pattern provides significant performance improvements over traditional exception handling in .NET applications. Exceptions are expensive - they involve stack unwinding, object allocation, and can be 1000x slower than returning a Result object:

```csharp
// ❌ Traditional approach - throwing exceptions
public User GetUser(int id)
{
    var user = _repository.FindById(id);
    if (user == null)
        throw new NotFoundException($"User {id} not found"); // ~1000x slower!
    return user;
}

// ✅ Result pattern - no exceptions
public Result<User> GetUser(int id)
{
    var user = _repository.FindById(id);
    if (user == null)
        return Result<User>.FailNotFound($"User {id} not found"); // Much faster!
    return Result<User>.Succeed(user);
}

// Multiple ways to create failures
var notFound = Result<User>.FailNotFound("User not found");
var generalFail = Result<User>.Fail("Operation failed", "Detailed error description");
var withStatusCode = Result<User>.Fail("Forbidden", "Access denied", HttpStatusCode.Forbidden);
var fromProblem = Result<User>.Fail(Problem.Create("type", "title", 400, "detail"));
var fromException = Result<User>.Fail(new InvalidOperationException("Not allowed"));
```

## 🧪 Testing

The Result pattern makes unit testing in .NET much cleaner by eliminating the need to test exception scenarios. Instead of asserting on thrown exceptions, you can directly check Result properties:

```csharp
[Test]
public async Task GetUser_WhenUserExists_ReturnsSuccess()
{
    // Arrange
    var userId = 123;
    var expectedUser = new User { Id = userId, Name = "John" };
    _mockRepository.Setup(x => x.FindById(userId)).Returns(expectedUser);

    // Act
    var result = await _userService.GetUser(userId);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Name.Should().Be(expectedUser.Name);
}

[Test]
public async Task GetUser_WhenUserNotFound_ReturnsCorrectProblem()
{
    // Arrange
    var userId = 999;
    _mockRepository.Setup(x => x.FindById(userId)).Returns((User)null);

    // Act
    var result = await _userService.GetUser(userId);

    // Assert
    result.IsFailed.Should().BeTrue();
    result.Problem.StatusCode.Should().Be(404);
    result.Problem.Title.Should().Be("Not Found");
}
```

## 🛠️ Configuration Options

```csharp
services.AddCommunication(options =>
{
    // Show detailed error information (disable in production)
    options.ShowErrorDetails = false;
    
    // Custom error response builder
    options.ErrorResponseBuilder = (problem, context) =>
    {
        return new
        {
            type = problem.Type,
            title = problem.Title,
            status = problem.StatusCode,
            detail = options.ShowErrorDetails ? problem.Detail : null,
            instance = problem.Instance,
            traceId = Activity.Current?.Id ?? context.TraceIdentifier,
            extensions = problem.Extensions
        };
    };
});
```

## 📝 Best Practices for Result Pattern in .NET

1. **Always return Result types from your service methods in C# applications**
   ```csharp
   public interface IUserService
   {
       Task<Result<User>> GetByIdAsync(int id);
       Task<Result<User>> CreateAsync(CreateUserDto dto);
       Task<Result> DeleteAsync(int id);
       Task<CollectionResult<User>> GetPagedAsync(int page, int pageSize);
   }
   ```

2. **Use specific Problem Details for different error scenarios**
   ```csharp
   return Result<Order>.Fail(Problem.Create(
       type: "https://example.com/probs/insufficient-inventory",
       title: "Insufficient Inventory",
       statusCode: 422,
       detail: $"Product {productId} has only {available} items, but {requested} were requested",
       instance: $"/orders/{orderId}"
   ));
   ```

3. **Leverage Railway-Oriented Programming for complex workflows**
   ```csharp
   public async Task<Result<OrderConfirmation>> ProcessOrderAsync(OrderRequest request)
   {
       return await ValidateOrderRequest(request)
           .BindAsync(validRequest => CheckInventoryAsync(validRequest))
           .BindAsync(inventory => CalculatePricingAsync(inventory))
           .BindAsync(pricing => ProcessPaymentAsync(pricing))
           .BindAsync(payment => CreateOrderAsync(payment))
           .MapAsync(order => GenerateConfirmationAsync(order));
   }
   ```

4. **Use TryGetProblem for conditional error handling**
   ```csharp
   if (result.TryGetProblem(out var problem))
   {
       _logger.LogError("Operation failed: {Type} - {Detail}", 
           problem.Type, problem.Detail);
       
       if (problem.StatusCode == 429) // Too Many Requests
       {
           var retryAfter = problem.Extensions.GetValueOrDefault("retryAfter");
           // Handle rate limiting
       }
   }
   ```

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

Special thanks to all contributors who have helped make this library better!

---

**Made with ❤️ by [ManagedCode](https://github.com/managedcode)**