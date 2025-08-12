 # ManagedCode.Communication

A comprehensive .NET library that provides a robust Result pattern implementation with seamless integration for ASP.NET Core, SignalR, and Microsoft Orleans. This library enables railway-oriented programming (ROP) and provides standardized error handling across your entire application stack.

## 🚀 Key Features

### Core Result Pattern
- **Type-safe Result types**: `Result` and `Result<T>` for operations that may fail
- **Collection Results**: `CollectionResult<T>` with built-in pagination support
- **RFC 7807 Problem Details**: Standardized error responses following HTTP API Problem Details specification
- **Railway-oriented Programming**: Fluent API for chaining operations and error handling
- **Validation Support**: Built-in validation error handling with field-specific messages

### Command Pattern
- **Command Infrastructure**: `Command` and `Command<T>` with comprehensive metadata
- **Distributed Tracing**: Built-in support for correlation IDs, causation IDs, and trace/span IDs
- **Idempotency**: Command idempotency support with Orleans integration
- **Audit Trail**: User and session tracking capabilities

### ASP.NET Core Integration
- **Automatic Result Mapping**: Seamless conversion of `Result` types to HTTP responses
- **Global Exception Handling**: Centralized exception handling with Problem Details
- **Model Validation**: Automatic validation error handling
- **SignalR Support**: Hub filters for consistent error handling in real-time applications
- **Middleware Integration**: Easy setup with extension methods

### Microsoft Orleans Integration
- **Grain Call Filters**: Automatic Result serialization and deserialization
- **Surrogate Converters**: Efficient serialization of Result types across grain boundaries
- **Command Idempotency**: Distributed command idempotency using Orleans grains
- **Performance Optimized**: Minimal overhead for distributed scenarios

## 📦 NuGet Packages

| Package | Description | Version |
|---------|-------------|---------|
| `ManagedCode.Communication` | Core Result pattern implementation | [![NuGet](https://img.shields.io/nuget/v/ManagedCode.Communication.svg)](https://www.nuget.org/packages/ManagedCode.Communication/) |
| `ManagedCode.Communication.AspNetCore` | ASP.NET Core integration | [![NuGet](https://img.shields.io/nuget/v/ManagedCode.Communication.AspNetCore.svg)](https://www.nuget.org/packages/ManagedCode.Communication.AspNetCore/) |
| `ManagedCode.Communication.Orleans` | Microsoft Orleans integration | [![NuGet](https://img.shields.io/nuget/v/ManagedCode.Communication.Orleans.svg)](https://www.nuget.org/packages/ManagedCode.Communication.Orleans/) |

## 🏗️ Architecture

The library follows a modular architecture with three main packages:

```
ManagedCode.Communication/
├── Core Result Pattern (ManagedCode.Communication)
├── ASP.NET Core Integration (ManagedCode.Communication.AspNetCore)
└── Orleans Integration (ManagedCode.Communication.Orleans)
```

## 🚀 Quick Start

### 1. Basic Result Usage

```csharp
using ManagedCode.Communication;

// Simple success/failure results
var successResult = Result.Succeed();
var failureResult = Result.Fail("Operation failed", "Something went wrong");

// Results with values
var userResult = Result<User>.Succeed(new User { Id = 1, Name = "John" });
var notFoundResult = Result<User>.FailNotFound("User not found");

// Validation results
var validationResult = Result.FailValidation(
    ("email", "Email is required"),
    ("age", "Age must be greater than 0")
);

// Exception handling
try
{
    // Some operation that might throw
    throw new InvalidOperationException("Something went wrong");
}
catch (Exception ex)
{
    var errorResult = Result.Fail(ex);
}
```

### 2. Railway-Oriented Programming

```csharp
// Chaining operations
var result = Result<User>.Succeed(user)
    .Then(user => ValidateUser(user))
    .Then(user => SaveUser(user))
    .Then(user => SendWelcomeEmail(user));

// Conditional operations
var result = Result<User>.Succeed(user)
    .FailIf(u => u.Age < 18, "User must be 18 or older")
    .OkIf(u => u.Email.Contains("@"), "Invalid email format");

// Async operations
var result = await Result<User>.Succeed(user)
    .ThenAsync(async u => await ValidateUserAsync(u))
    .ThenAsync(async u => await SaveUserAsync(u));
```

### 3. Collection Results with Pagination

```csharp
// Create paginated results
var users = new[] { user1, user2, user3 };
var collectionResult = CollectionResult<User>.Succeed(users, pageNumber: 1, pageSize: 10, totalItems: 25);

// Access pagination info
if (collectionResult.IsSuccess)
{
    var items = collectionResult.Collection;
    var totalPages = collectionResult.TotalPages;
    var hasMore = collectionResult.PageNumber < collectionResult.TotalPages;
}
```

### 4. ASP.NET Core Integration

#### Setup

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add Communication services
builder.Services.AddCommunication(options => 
{
    options.ShowErrorDetails = builder.Environment.IsDevelopment();
});

// Add filters to MVC
builder.Services.AddControllers(options =>
{
    options.AddCommunicationFilters();
});

// Add filters to SignalR
builder.Services.AddSignalR(options =>
{
    options.AddCommunicationFilters();
});

var app = builder.Build();
```

#### Controller Usage

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    public Result<User> GetUser(int id)
    {
        var user = _userService.GetUser(id);
        
        if (user == null)
            return Result<User>.FailNotFound($"User with ID {id} not found");
            
        return Result<User>.Succeed(user);
    }

    [HttpPost]
    public Result<User> CreateUser([FromBody] CreateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .SelectMany(x => x.Value!.Errors.Select(e => (x.Key, e.ErrorMessage)))
                .ToArray();
            return Result<User>.FailValidation(errors);
        }

        var user = _userService.CreateUser(request);
        return Result<User>.Succeed(user);
    }
}
```

#### Automatic HTTP Response Mapping

The library automatically converts `Result` types to appropriate HTTP responses:

- **Success Results**: `200 OK` with the value, or `204 No Content` for void results
- **Validation Errors**: `400 Bad Request` with Problem Details
- **Not Found**: `404 Not Found` with Problem Details
- **Unauthorized**: `401 Unauthorized` with Problem Details
- **Forbidden**: `403 Forbidden` with Problem Details
- **Server Errors**: `500 Internal Server Error` with Problem Details

### 5. Command Pattern

```csharp
// Create commands
var command = Command.Create("CreateUser");
command.UserId = "user123";
command.CorrelationId = "corr-456";

// Typed commands
public class CreateUserCommand : Command<CreateUserRequest>
{
    public CreateUserCommand(CreateUserRequest request) 
        : base(Guid.NewGuid(), "CreateUser")
    {
        Value = request;
    }
}

// Command execution with idempotency
var result = await _commandHandler.ExecuteAsync(command);
```

### 6. Microsoft Orleans Integration

#### Setup

```csharp
// Silo configuration
var siloBuilder = Host.CreateDefaultBuilder(args)
    .UseOrleans(silo =>
    {
        silo.UseOrleansCommunication();
    });

// Client configuration
var clientBuilder = Host.CreateDefaultBuilder(args)
    .UseOrleansClient(client =>
    {
        client.UseOrleansCommunication();
    });
```

#### Grain Usage

```csharp
public interface IUserGrain : IGrainWithStringKey
{
    Task<Result<User>> GetUserAsync();
    Task<Result<User>> CreateUserAsync(CreateUserRequest request);
}

public class UserGrain : Grain, IUserGrain
{
    public async Task<Result<User>> GetUserAsync()
    {
        var user = await _userRepository.GetAsync(this.GetPrimaryKeyString());
        
        if (user == null)
            return Result<User>.FailNotFound("User not found");
            
        return Result<User>.Succeed(user);
    }

    public async Task<Result<User>> CreateUserAsync(CreateUserRequest request)
    {
        // Validation
        if (string.IsNullOrEmpty(request.Email))
            return Result<User>.FailValidation(("email", "Email is required"));

        var user = await _userRepository.CreateAsync(request);
        return Result<User>.Succeed(user);
    }
}
```

## 🔧 Advanced Features

### Problem Details (RFC 7807)

```csharp
// Create custom problems
var problem = Problem.Create(
    type: "https://api.example.com/errors/invalid-email",
    title: "Invalid Email Format",
    statusCode: 400,
    detail: "The provided email address is not in a valid format"
);

// Add extensions
problem.Extensions["field"] = "email";
problem.Extensions["suggestedFormat"] = "user@domain.com";

// Validation problems
var validationProblem = Problem.Validation(
    ("email", "Email is required"),
    ("email", "Invalid email format"),
    ("age", "Age must be greater than 0")
);
```

### Railway Extensions

```csharp
// Advanced chaining
var result = await Result<User>.Succeed(user)
    .ThenAsync(async u => await ValidateUserAsync(u))
    .ThenAsync(async u => await CheckPermissionsAsync(u))
    .ThenAsync(async u => await SaveUserAsync(u))
    .MapAsync(async u => await SendNotificationAsync(u));

// Error recovery
var result = await Result<User>.Succeed(user)
    .ThenAsync(async u => await PrimaryServiceAsync(u))
    .OrElseAsync(async u => await FallbackServiceAsync(u));

// Conditional execution
var result = await Result<User>.Succeed(user)
    .WhenAsync(
        predicate: async u => await ShouldNotifyAsync(u),
        then: async u => await SendNotificationAsync(u)
    );
```

### Collection Results

```csharp
// Pagination
var result = CollectionResult<User>.Succeed(
    users: users,
    pageNumber: 1,
    pageSize: 10,
    totalItems: 100
);

// Empty collections
var emptyResult = CollectionResult<User>.Succeed(
    users: Array.Empty<User>(),
    pageNumber: 1,
    pageSize: 10,
    totalItems: 0
);

// Failed collections
var failedResult = CollectionResult<User>.Fail("Failed to load users");
```

## 🧪 Testing

The library includes comprehensive test coverage and supports easy testing:

```csharp
[Fact]
public void UserCreation_WithValidData_ShouldSucceed()
{
    // Arrange
    var request = new CreateUserRequest { Name = "John", Email = "john@example.com" };
    
    // Act
    var result = _userService.CreateUser(request);
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Should().NotBeNull();
    result.Value!.Name.Should().Be("John");
}

[Fact]
public void UserCreation_WithInvalidEmail_ShouldFailValidation()
{
    // Arrange
    var request = new CreateUserRequest { Name = "John", Email = "invalid-email" };
    
    // Act
    var result = _userService.CreateUser(request);
    
    // Assert
    result.IsSuccess.Should().BeFalse();
    result.IsInvalid.Should().BeTrue();
    result.InvalidField("email").Should().BeTrue();
    result.InvalidFieldError("email").Should().Contain("Invalid email format");
}
```

## 📊 Performance

The library is designed for high performance:

- **Struct-based Results**: Minimal memory allocation for success cases
- **Efficient Serialization**: Optimized for Orleans grain communication
- **Cached Reflection**: Performance-optimized dynamic method calls
- **Memory-efficient**: Minimal overhead in hot paths

## 🔒 Security

- **Input Validation**: Comprehensive validation support
- **Error Information Control**: Configurable error detail exposure
- **Audit Trail**: Built-in command tracking and user attribution
- **Idempotency**: Prevents duplicate command execution

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- **Documentation**: [Wiki](https://github.com/managedcode/communication/wiki)
- **Issues**: [GitHub Issues](https://github.com/managedcode/communication/issues)
- **Discussions**: [GitHub Discussions](https://github.com/managedcode/communication/discussions)

## 🙏 Acknowledgments

- Inspired by the Railway-Oriented Programming pattern
- Built on RFC 7807 Problem Details for HTTP APIs
- Designed for seamless integration with Microsoft Orleans
- Optimized for ASP.NET Core applications
