# ManagedCode.Communication

[![.NET](https://github.com/managedcode/Communication/actions/workflows/dotnet.yml/badge.svg)](https://github.com/managedcode/Communication/actions/workflows/dotnet.yml)
[![Coverage Status](https://coveralls.io/repos/github/managedcode/Communication/badge.svg?branch=main&service=github)](https://coveralls.io/github/managedcode/Communication?branch=main)
[![nuget](https://github.com/managedcode/Communication/actions/workflows/nuget.yml/badge.svg?branch=main)](https://github.com/managedcode/Communication/actions/workflows/nuget.yml)
[![CodeQL](https://github.com/managedcode/Communication/actions/workflows/codeql-analysis.yml/badge.svg?branch=main)](https://github.com/managedcode/Communication/actions/workflows/codeql-analysis.yml)
[![NuGet Package](https://img.shields.io/nuget/v/ManagedCode.Communication.svg)](https://www.nuget.org/packages/ManagedCode.Communication)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ManagedCode.Communication.svg)](https://www.nuget.org/packages/ManagedCode.Communication)

> A powerful .NET library that revolutionizes error handling by providing a Result pattern implementation, eliminating exceptions and making your code more predictable, testable, and maintainable.

## 🎯 Why ManagedCode.Communication?

Traditional exception-based error handling can make code difficult to follow and test. The Communication library introduces a **Result pattern** that transforms how you handle operations that might fail:

- ✅ **No More Exceptions** - Replace try-catch blocks with elegant Result objects
- 🔍 **Explicit Error Handling** - Makes potential failures visible in method signatures
- 🧪 **Better Testability** - No need to test exception scenarios
- 🚀 **Improved Performance** - Avoid the overhead of throwing exceptions
- 📝 **Self-Documenting Code** - Method signatures clearly indicate possible failures

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

// Simple failure result
var failure = Result.Fail("Something went wrong");
if (failure.IsFailed)
{
    Console.WriteLine($"Error: {failure.GetError()}");
}
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

// Failure with error details
var notFound = Result<User>.Fail("User not found", HttpStatusCode.NotFound);
if (notFound.IsFailed)
{
    Console.WriteLine($"Error: {notFound.GetError()} (Status: {notFound.StatusCode})");
}
```

### Collection Results

Perfect for paginated API responses:

```csharp
var products = await GetProductsAsync(page: 1, pageSize: 20);

var result = CollectionResult<Product>.Succeed(
    items: products,
    page: 1,
    pageSize: 20,
    totalCount: 150
);

// Access pagination info
Console.WriteLine($"Page {result.Page} of {result.TotalPages}");
Console.WriteLine($"Showing {result.Items.Count()} of {result.TotalCount} products");
```

### Async Operations with Result.From

Convert any operation into a Result:

```csharp
// Wrap synchronous operations
var result = await Result<string>.From(() => 
{
    return File.ReadAllText("config.json");
});

// Wrap async operations
var apiResult = await Result<WeatherData>.From(async () => 
{
    return await weatherService.GetCurrentWeatherAsync("London");
});

if (apiResult.IsSuccess)
{
    Console.WriteLine($"Temperature: {apiResult.Value.Temperature}°C");
}
else
{
    Console.WriteLine($"API call failed: {apiResult.GetError()}");
}
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
    options.AddCommunicationFilters();
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
            return Result<UserDto>.Fail($"User with ID {id} not found", HttpStatusCode.NotFound);
            
        return Result<UserDto>.Succeed(user.ToDto());
    }

    [HttpPost]
    public async Task<Result<UserDto>> CreateUser([FromBody] CreateUserDto dto)
    {
        // Model validation is handled automatically by CommunicationModelValidationFilter
        var user = await _userService.CreateAsync(dto);
        return Result<UserDto>.Succeed(user.ToDto(), HttpStatusCode.Created);
    }

    [HttpGet]
    public async Task<CollectionResult<UserDto>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
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
            return Result.Fail("Message cannot be empty");

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

### Custom Error Types

```csharp
public class ValidationError : Error
{
    public Dictionary<string, string[]> Errors { get; }
    
    public ValidationError(Dictionary<string, string[]> errors) 
        : base("Validation failed", HttpStatusCode.BadRequest)
    {
        Errors = errors;
    }
}

// Usage
var validationErrors = new Dictionary<string, string[]>
{
    ["Email"] = ["Invalid email format", "Email already exists"],
    ["Password"] = ["Password must be at least 8 characters"]
};

return Result<User>.Fail(new ValidationError(validationErrors));
```

### Result Extensions and Chaining

```csharp
// Map successful results
var result = await GetUserAsync(id)
    .Map(user => user.ToDto())
    .Map(dto => new UserViewModel(dto));

// Handle both success and failure cases
var message = await CreateOrderAsync(orderDto)
    .Match(
        onSuccess: order => $"Order {order.Id} created successfully",
        onFailure: error => $"Failed to create order: {error.Message}"
    );

// Chain operations
var finalResult = await GetUserAsync(userId)
    .Bind(user => ValidateUserAsync(user))
    .Bind(user => CreateOrderForUserAsync(user, orderDto))
    .Map(order => order.ToDto());
```

### Global Exception Handling

The Communication filters automatically convert exceptions to Result objects:

```csharp
// This exception will be caught and converted to Result.Fail
[HttpGet("{id}")]
public async Task<Result<Product>> GetProduct(int id)
{
    // If this throws, CommunicationExceptionFilter handles it
    var product = await _repository.GetByIdAsync(id);
    return Result<Product>.Succeed(product);
}
```

### Status Code Mapping

The library automatically maps exceptions to appropriate HTTP status codes:

```csharp
// Built-in mappings
ArgumentException           → 400 Bad Request
UnauthorizedAccessException → 401 Unauthorized  
KeyNotFoundException       → 404 Not Found
InvalidOperationException   → 409 Conflict
NotImplementedException     → 501 Not Implemented

// ASP.NET Core specific
BadHttpRequestException     → 400 Bad Request
AuthenticationFailureException → 401 Unauthorized
AntiforgeryValidationException → 400 Bad Request
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
    public Task<Result<UserData>> GetUserDataAsync()
    {
        try
        {
            var userData = LoadUserData();
            return Task.FromResult(Result<UserData>.Succeed(userData));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<UserData>.Fail(ex));
        }
    }
}
```

## 📊 Performance Benefits

Using Result pattern instead of exceptions provides significant performance improvements:

```csharp
// ❌ Traditional approach - throwing exceptions
public User GetUser(int id)
{
    var user = _repository.FindById(id);
    if (user == null)
        throw new NotFoundException($"User {id} not found"); // Expensive!
    return user;
}

// ✅ Result pattern - no exceptions
public Result<User> GetUser(int id)
{
    var user = _repository.FindById(id);
    if (user == null)
        return Result<User>.Fail($"User {id} not found"); // Much faster!
    return Result<User>.Succeed(user);
}
```

## 🧪 Testing

Result pattern makes testing much cleaner:

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
    Assert.IsTrue(result.IsSuccess);
    Assert.AreEqual(expectedUser.Name, result.Value.Name);
}

[Test]
public async Task GetUser_WhenUserNotFound_ReturnsFailure()
{
    // Arrange
    var userId = 999;
    _mockRepository.Setup(x => x.FindById(userId)).Returns((User)null);

    // Act
    var result = await _userService.GetUser(userId);

    // Assert
    Assert.IsFalse(result.IsSuccess);
    Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
}
```

## 🛠️ Configuration Options

```csharp
services.AddCommunication(options =>
{
    // Show detailed error information (disable in production)
    options.ShowErrorDetails = false;
    
    // Custom error response builder
    options.ErrorResponseBuilder = (error, context) =>
    {
        return new
        {
            error = error.Message,
            timestamp = DateTime.UtcNow,
            path = context.Request.Path
        };
    };
    
    // Custom status code mapping
    options.StatusCodeMapping[typeof(CustomException)] = HttpStatusCode.Conflict;
});
```

## 📝 Best Practices

1. **Always return Result types from your service methods**
   ```csharp
   public interface IUserService
   {
       Task<Result<User>> GetByIdAsync(int id);
       Task<Result<User>> CreateAsync(CreateUserDto dto);
       Task<Result> DeleteAsync(int id);
   }
   ```

2. **Use specific error messages and appropriate status codes**
   ```csharp
   return Result<Order>.Fail(
       "Insufficient inventory for product SKU-123", 
       HttpStatusCode.UnprocessableEntity
   );
   ```

3. **Leverage pattern matching for elegant error handling**
   ```csharp
   var response = await ProcessOrder(orderId) switch
   {
       { IsSuccess: true } result => Ok(result.Value),
       { StatusCode: HttpStatusCode.NotFound } => NotFound(),
       var failure => BadRequest(failure.GetError())
   };
   ```

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

Special thanks to all contributors who have helped make this library better!

---

**Made with ❤️ by [ManagedCode](https://github.com/managedcode)**