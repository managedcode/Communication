# ManagedCode.Communication

[![.NET](https://github.com/managedcode/Communication/actions/workflows/ci.yml/badge.svg)](https://github.com/managedcode/Communication/actions/workflows/ci.yml)
[![NuGet Package](https://img.shields.io/nuget/v/ManagedCode.Communication.svg)](https://www.nuget.org/packages/ManagedCode.Communication)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ManagedCode.Communication.svg)](https://www.nuget.org/packages/ManagedCode.Communication)

## Overview

ManagedCode.Communication is a comprehensive Result pattern implementation for .NET that fundamentally changes how you handle errors in your applications. Instead of using exceptions for control flow, this library provides a type-safe, explicit, and performant way to handle both successful and failed operations.

### What is the Result Pattern?

The Result pattern is a functional programming concept where functions return an object that explicitly represents either success or failure, rather than throwing exceptions. This makes error handling:

- **Explicit** - All possible errors are visible in method signatures
- **Composable** - Operations can be chained together with automatic error propagation  
- **Performant** - No exception throwing overhead
- **Testable** - Easier to test both success and failure scenarios

### Core Features

- **Result Types** - `Result`, `Result<T>`, and `CollectionResult<T>` for different scenarios
- **RFC 7807 Problem Details** - Standardized error format for APIs
- **Railway-Oriented Programming** - Chain operations with automatic error handling
- **Framework Integration** - Native support for ASP.NET Core, SignalR, and Orleans
- **Validation Support** - Built-in handling for validation errors
- **Custom Error Enums** - Domain-specific error codes
- **Performance** - Significantly faster than exception-based error handling

## Table of Contents

- [Installation](#installation)
- [Basic Concepts](#basic-concepts)
- [Creating Results](#creating-results)
- [Checking Results](#checking-results)
- [Problem Details (RFC 7807)](#problem-details-rfc-7807)
- [Railway-Oriented Programming](#railway-oriented-programming)
- [ASP.NET Core Integration](#aspnet-core-integration)
- [Orleans Integration](#orleans-integration)
- [SignalR Integration](#signalr-integration)
- [Validation Handling](#validation-handling)
- [Error Handling Strategies](#error-handling-strategies)
- [Testing](#testing)
- [Performance](#performance)
- [Best Practices](#best-practices)
- [Migration Guide](#migration-guide)
- [Real-World Examples](#real-world-examples)

## Installation

### NuGet Packages

```bash
# Core library - Required for all projects
dotnet add package ManagedCode.Communication

# ASP.NET Core integration - For Web APIs and MVC applications
dotnet add package ManagedCode.Communication.AspNetCore

# Orleans integration - For distributed systems using Orleans
dotnet add package ManagedCode.Communication.Orleans
```

## Basic Concepts

### The Problem with Exceptions

Traditional exception handling has several issues:

```csharp
// BAD: Using exceptions for control flow
public class UserService
{
    public User GetUser(int id)
    {
        var user = _repository.FindById(id);
        if (user == null)
            throw new NotFoundException($"User {id} not found"); // Hidden in implementation
        
        if (!user.IsActive)
            throw new ForbiddenException("User is deactivated"); // Another hidden path
        
        if (user.IsDeleted)
            throw new InvalidOperationException("User is deleted"); // Yet another
        
        return user; // Method signature lies - says it returns User, but can throw 3+ exceptions
    }
}

// Caller has to know about all possible exceptions
try
{
    var user = userService.GetUser(123);
    // Use user
}
catch (NotFoundException ex)
{
    // Handle not found
}
catch (ForbiddenException ex) 
{
    // Handle forbidden
}
catch (InvalidOperationException ex)
{
    // Handle invalid operation  
}
catch (Exception ex)
{
    // Handle unexpected errors
}
```

### The Result Pattern Solution

With Result pattern, all possible outcomes are explicit:

```csharp
// GOOD: Using Result pattern
public class UserService
{
    public Result<User> GetUser(int id)
    {
        var user = _repository.FindById(id);
        if (user == null)
            return Result<User>.FailNotFound($"User {id} not found");
        
        if (!user.IsActive)
            return Result<User>.FailForbidden("User account is deactivated");
        
        if (user.IsDeleted)
            return Result<User>.Fail("User Deleted", "This user has been deleted", 410);
        
        return Result<User>.Succeed(user);
    }
}

// Caller handles results explicitly
var result = userService.GetUser(123);

if (result.IsSuccess)
{
    var user = result.Value;
    // Use user
}
else if (result.Problem.StatusCode == 404)
{
    // Handle not found
}
else if (result.Problem.StatusCode == 403)
{
    // Handle forbidden
}
else
{
    // Handle other errors
    _logger.LogError("Failed to get user: {Error}", result.Problem.Detail);
}
```

### Result Types

The library provides three main result types:

```csharp
// Result - for operations without a return value
public Result DeleteUser(int id)
{
    if (!_repository.Exists(id))
        return Result.FailNotFound($"User {id} not found");
    
    _repository.Delete(id);
    return Result.Succeed();
}

// Result<T> - for operations that return a value
public Result<User> GetUser(int id)
{
    var user = _repository.FindById(id);
    if (user == null)
        return Result<User>.FailNotFound($"User {id} not found");
    
    return Result<User>.Succeed(user);
}

// CollectionResult<T> - for paginated collections
public CollectionResult<User> GetUsers(int page = 1, int pageSize = 20)
{
    var query = _repository.GetAll();
    var totalCount = query.Count();
    
    var items = query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToList();
    
    return CollectionResult<User>.Succeed(
        items,
        pageNumber: page,
        pageSize: pageSize,
        totalItems: totalCount
    );
}
```

## Creating Results

### Success Results

```csharp
// Simple success without value
Result successResult = Result.Succeed();

// Success with value
User user = new User { Id = 1, Name = "John" };
Result<User> userResult = Result<User>.Succeed(user);

// Success with collection and pagination
List<Product> products = GetProducts();
CollectionResult<Product> productsResult = CollectionResult<Product>.Succeed(
    products,
    pageNumber: 1,
    pageSize: 20,
    totalItems: 100
);
```

### Failure Results

```csharp
// Basic failure
Result failResult = Result.Fail();
Result failWithMessage = Result.Fail("Operation failed");

// Failure with title and detail
Result detailedFail = Result.Fail(
    title: "Payment Failed",
    detail: "Insufficient funds in account"
);

// Failure with status code
Result httpFail = Result.Fail(
    title: "Resource Conflict",
    detail: "A resource with this name already exists",
    statusCode: HttpStatusCode.Conflict // 409
);

// Common HTTP failures - shortcuts
Result notFound = Result.FailNotFound("User not found");
Result unauthorized = Result.FailUnauthorized("Invalid credentials");
Result forbidden = Result.FailForbidden("You don't have permission");
Result conflict = Result.FailConflict("Resource already exists");
Result badRequest = Result.FailBadRequest("Invalid request data");

// Validation failures
Result validationFail = Result.FailValidation(
    ("email", "Email is required"),
    ("email", "Email format is invalid"),
    ("password", "Password must be at least 8 characters"),
    ("age", "You must be 18 or older")
);

// From exception
try
{
    // Some operation
}
catch (Exception ex)
{
    return Result.Fail(ex);
}

// From Problem Details
var problem = Problem.Create(
    type: "https://api.example.com/errors/payment-failed",
    title: "Payment Failed",
    statusCode: 402,
    detail: "Your payment could not be processed"
);
return Result.Fail(problem);

// From custom error enum
public enum OrderError
{
    InsufficientStock = 1001,
    InvalidCoupon = 1002,
    ShippingNotAvailable = 1003
}

Result orderFail = Result.Fail(
    OrderError.InsufficientStock,
    "Only 3 items in stock, but 5 were requested"
);
```

### Try Pattern - Converting Exceptions to Results

The Try pattern wraps exception-throwing code and converts it to Results:

```csharp
// Synchronous Try
public Result<Configuration> LoadConfiguration()
{
    return Result.Try(() =>
    {
        var json = File.ReadAllText("config.json");
        return JsonSerializer.Deserialize<Configuration>(json);
    });
}

// Asynchronous Try
public async Task<Result<WeatherData>> GetWeatherAsync(string city)
{
    return await Result.TryAsync(async () =>
    {
        var response = await _httpClient.GetStringAsync($"api/weather/{city}");
        return JsonSerializer.Deserialize<WeatherData>(response);
    }, HttpStatusCode.BadGateway); // Optional: specify status code for failures
}

// Try with specific exception handling
public async Task<Result<User>> GetUserFromDatabaseAsync(int id)
{
    return await Result.TryAsync(
        async () => await _dbContext.Users.FirstAsync(u => u.Id == id),
        onException: ex => ex switch
        {
            InvalidOperationException => Result<User>.FailNotFound($"User {id} not found"),
            DbUpdateException => Result<User>.Fail("Database Error", ex.Message, 500),
            _ => Result<User>.Fail(ex)
        }
    );
}

// Collection Try
public async Task<CollectionResult<Order>> GetOrdersAsync()
{
    return await CollectionResult<Order>.From(async () =>
    {
        var orders = await _repository.GetOrdersAsync();
        var count = await _repository.GetOrderCountAsync();
        
        return CollectionResult<Order>.Succeed(
            orders,
            pageNumber: 1,
            pageSize: 50,
            totalItems: count
        );
    });
}
```

## Checking Results

### Basic State Checking

```csharp
var result = GetUserResult(id);

// Check if successful
if (result.IsSuccess)
{
    var user = result.Value; // Safe to access when IsSuccess is true
    Console.WriteLine($"Found user: {user.Name}");
}

// Check if failed
if (result.IsFailed)
{
    Console.WriteLine("Operation failed");
}

// Check if has problem details
if (result.HasProblem)
{
    var problem = result.Problem; // Always safe to access, never null
    Console.WriteLine($"Error: {problem.Title} - {problem.Detail}");
}

// Safe problem access with out parameter
if (result.TryGetProblem(out var problem))
{
    _logger.LogError(
        "Operation failed with status {StatusCode}: {Title} - {Detail}",
        problem.StatusCode,
        problem.Title,
        problem.Detail
    );
}
```

### Pattern Matching

```csharp
// Basic pattern matching
var message = result.Match(
    onSuccess: user => $"Welcome, {user.Name}!",
    onFailure: problem => $"Error: {problem.Title}"
);

// Pattern matching with actions
result.Match(
    onSuccess: user => 
    {
        _cache.Set(user.Id, user);
        _logger.LogInfo($"User {user.Id} retrieved");
    },
    onFailure: problem =>
    {
        _logger.LogError($"Failed to get user: {problem.Detail}");
        _metrics.IncrementErrorCounter();
    }
);

// Pattern matching for HTTP responses
public IActionResult HandleResult<T>(Result<T> result)
{
    return result.Match(
        onSuccess: value => Ok(new { success = true, data = value }),
        onFailure: problem => problem.StatusCode switch
        {
            400 => BadRequest(problem),
            401 => Unauthorized(problem),
            403 => Forbid(),
            404 => NotFound(problem),
            409 => Conflict(problem),
            422 => UnprocessableEntity(problem),
            _ => StatusCode(problem.StatusCode, problem)
        }
    );
}
```

### Throwing Exceptions from Results

Sometimes you need to integrate with code that expects exceptions:

```csharp
// Throw if failed - throws ProblemException with all problem details
var result = GetUserResult(id);
result.ThrowIfFail(); // Throws if failed, continues if successful
var user = result.Value; // Safe after ThrowIfFail

// Or use the Value property which throws if failed
try
{
    var user = result.Value; // Throws ProblemException if result is failed
}
catch (ProblemException ex)
{
    Console.WriteLine($"Problem: {ex.Problem.Title}");
}
```

## Problem Details (RFC 7807)

### Understanding RFC 7807

RFC 7807 defines a standard format for error responses in HTTP APIs. Instead of ad-hoc error messages, you get structured, machine-readable problem details.

### Problem Structure

```csharp
public class Problem
{
    // A URI reference that identifies the problem type
    public string Type { get; set; }
    
    // A short, human-readable summary of the problem type
    public string Title { get; set; }
    
    // The HTTP status code
    public int StatusCode { get; set; }
    
    // A human-readable explanation specific to this occurrence
    public string Detail { get; set; }
    
    // A URI reference that identifies the specific occurrence
    public string Instance { get; set; }
    
    // Additional problem-specific data
    public Dictionary<string, object?> Extensions { get; set; }
}
```

### Creating Problem Details

```csharp
// Basic problem
var problem = Problem.Create(
    title: "Validation Failed",
    detail: "The request contains invalid data",
    statusCode: 400
);

// Full problem with all properties
var detailedProblem = Problem.Create(
    type: "https://api.example.com/errors/out-of-credit",
    title: "Out of Credit",
    statusCode: 403,
    detail: "Your current balance is 30 USD, but that costs 50 USD",
    instance: "/account/12345/transactions/abc"
);

// Add extensions for additional context
detailedProblem.Extensions["balance"] = 30.00m;
detailedProblem.Extensions["cost"] = 50.00m;
detailedProblem.Extensions["currency"] = "USD";

// Problem for validation errors
var validationProblem = Problem.CreateValidation(
    ("email", new[] { "Email is required", "Email format is invalid" }),
    ("password", new[] { "Password is too short" })
);

// This creates a problem with Extensions["errors"] containing:
// {
//   "email": ["Email is required", "Email format is invalid"],
//   "password": ["Password is too short"]
// }
```

### JSON Representation

Problems are serialized to JSON following RFC 7807:

```json
{
  "type": "https://api.example.com/errors/out-of-credit",
  "title": "Out of Credit",
  "status": 403,
  "detail": "Your current balance is 30 USD, but that costs 50 USD",
  "instance": "/account/12345/transactions/abc",
  "balance": 30.00,
  "cost": 50.00,
  "currency": "USD"
}
```

### Custom Error Enums

Define domain-specific error codes for better error categorization:

```csharp
// Define your error enum
public enum PaymentError
{
    [Description("Insufficient funds in account")]
    InsufficientFunds = 1001,
    
    [Description("Credit card has expired")]
    CardExpired = 1002,
    
    [Description("Card was declined by bank")]
    CardDeclined = 1003,
    
    [Description("Payment gateway timeout")]
    PaymentGatewayTimeout = 1004,
    
    [Description("Invalid card number")]
    InvalidCardNumber = 1005
}

// Use in results
public Result<PaymentReceipt> ProcessPayment(PaymentRequest request)
{
    if (request.Amount > account.Balance)
    {
        return Result<PaymentReceipt>.Fail(
            PaymentError.InsufficientFunds,
            $"Your balance ({account.Balance:C}) is insufficient for this payment ({request.Amount:C})"
        );
    }
    
    if (request.Card.ExpiryDate < DateTime.Now)
    {
        return Result<PaymentReceipt>.Fail(
            PaymentError.CardExpired,
            $"Your card expired on {request.Card.ExpiryDate:MM/yyyy}"
        );
    }
    
    // Process payment...
}

// Check for specific errors
var result = ProcessPayment(request);
if (result.HasProblem && result.Problem.HasErrorCode(PaymentError.InsufficientFunds))
{
    // Prompt user to add funds
}

// Get error code as enum
var errorCode = result.HasProblem ? result.Problem.GetErrorCodeAs<PaymentError>() : null;
switch (errorCode)
{
    case PaymentError.InsufficientFunds:
        ShowAddFundsDialog();
        break;
    case PaymentError.CardExpired:
        ShowUpdateCardDialog();
        break;
    case PaymentError.CardDeclined:
        ShowAlternativePaymentOptions();
        break;
}
```

## Railway-Oriented Programming

### Concept

Railway-Oriented Programming (ROP) is a functional programming pattern where your code flows like a train on railway tracks. Success stays on the main track, while failures switch to an error track. Once on the error track, subsequent operations are skipped.

### Core Operations

#### Bind - Chain Operations That Return Results

```csharp
public Result<Invoice> CreateInvoice(InvoiceRequest request)
{
    return ValidateRequest(request)           // Result<InvoiceRequest>
        .Bind(req => CheckCustomerCredit(req)) // Result<InvoiceRequest>
        .Bind(req => CalculateTaxes(req))      // Result<InvoiceRequest>
        .Bind(req => GenerateInvoice(req))     // Result<Invoice>
        .Bind(inv => SaveInvoice(inv));        // Result<Invoice>
    
    // If any step fails, the chain stops and returns that failure
}

// Async version
public async Task<Result<Invoice>> CreateInvoiceAsync(InvoiceRequest request)
{
    return await ValidateRequestAsync(request)
        .BindAsync(req => CheckCustomerCreditAsync(req))
        .BindAsync(req => CalculateTaxesAsync(req))
        .BindAsync(req => GenerateInvoiceAsync(req))
        .BindAsync(inv => SaveInvoiceAsync(inv));
}
```

#### Map - Transform Successful Values

```csharp
public Result<InvoiceDto> GetInvoiceDto(int invoiceId)
{
    return GetInvoice(invoiceId)              // Result<Invoice>
        .Map(invoice => invoice.ToDto())       // Result<InvoiceDto>
        .Map(dto => EnrichWithCustomerData(dto)) // Result<InvoiceDto>
        .Map(dto => AddPaymentHistory(dto));    // Result<InvoiceDto>
}

// Async version
public async Task<Result<InvoiceDto>> GetInvoiceDtoAsync(int invoiceId)
{
    return await GetInvoiceAsync(invoiceId)
        .MapAsync(invoice => Task.FromResult(invoice.ToDto()))
        .MapAsync(async dto => await EnrichWithCustomerDataAsync(dto))
        .MapAsync(async dto => await AddPaymentHistoryAsync(dto));
}
```

#### Tap - Perform Side Effects

```csharp
public Result<Order> ProcessOrder(Order order)
{
    return ValidateOrder(order)
        .Tap(o => _logger.LogInfo($"Processing order {o.Id}"))
        .Tap(o => _metrics.IncrementOrderCounter())
        .Tap(o => _cache.InvalidateCustomerCache(o.CustomerId))
        .Tap(o => PublishOrderEvent(o));
    // Order is passed through unchanged
}

// Async version
public async Task<Result<Order>> ProcessOrderAsync(Order order)
{
    return await ValidateOrderAsync(order)
        .TapAsync(async o => await _logger.LogInfoAsync($"Processing order {o.Id}"))
        .TapAsync(async o => await _metrics.IncrementOrderCounterAsync())
        .TapAsync(async o => await _cache.InvalidateCustomerCacheAsync(o.CustomerId))
        .TapAsync(async o => await PublishOrderEventAsync(o));
}
```

### Complex Railway Example

```csharp
public class OrderService
{
    public async Task<Result<OrderConfirmation>> PlaceOrderAsync(PlaceOrderRequest request)
    {
        return await ValidateCustomer(request.CustomerId)
            // Combine customer with validated products
            .BindAsync(async customer =>
            {
                var productsResult = await ValidateProducts(request.Items);
                if (productsResult.IsFailed)
                    return Result<(Customer, List<Product>)>.Fail(productsResult.Problem);
                
                return Result<(Customer, List<Product>)>.Succeed((customer, productsResult.Value));
            })
            
            // Apply discounts and calculate pricing
            .BindAsync(async data =>
            {
                var (customer, products) = data;
                
                // Check if coupon is valid
                if (!string.IsNullOrEmpty(request.CouponCode))
                {
                    var couponResult = await ValidateCoupon(request.CouponCode);
                    if (couponResult.IsFailed)
                        return Result<Order>.Fail(couponResult.Problem);
                    
                    var discount = couponResult.Value;
                    return await CalculatePricing(customer, products, discount);
                }
                
                return await CalculatePricing(customer, products, null);
            })
            
            // Check credit limit
            .BindAsync(async order =>
            {
                if (order.Total > order.Customer.CreditLimit)
                {
                    return Result<Order>.Fail(
                        "Credit Limit Exceeded",
                        $"Order total {order.Total:C} exceeds your credit limit {order.Customer.CreditLimit:C}",
                        HttpStatusCode.PaymentRequired
                    );
                }
                return Result<Order>.Succeed(order);
            })
            
            // Process payment
            .BindAsync(async order =>
            {
                var paymentResult = await _paymentService.ChargeAsync(
                    order.Customer.PaymentMethod,
                    order.Total
                );
                
                if (paymentResult.IsFailed)
                    return Result<Order>.Fail(paymentResult.Problem);
                
                order.PaymentId = paymentResult.Value.TransactionId;
                order.PaymentStatus = PaymentStatus.Completed;
                return Result<Order>.Succeed(order);
            })
            
            // Save order to database
            .BindAsync(async order =>
            {
                try
                {
                    await _repository.SaveOrderAsync(order);
                    return Result<Order>.Succeed(order);
                }
                catch (DbUpdateException ex)
                {
                    // Rollback payment if save fails
                    await _paymentService.RefundAsync(order.PaymentId);
                    return Result<Order>.Fail("Database Error", "Failed to save order", 500);
                }
            })
            
            // Update inventory (side effect - doesn't change the order)
            .TapAsync(async order =>
            {
                foreach (var item in order.Items)
                {
                    await _inventoryService.DecrementStockAsync(item.ProductId, item.Quantity);
                }
            })
            
            // Send notifications (side effect)
            .TapAsync(async order =>
            {
                // Fire and forget - don't fail the order if notifications fail
                _ = Task.Run(async () =>
                {
                    await _emailService.SendOrderConfirmationAsync(order);
                    await _smsService.SendOrderSmsAsync(order);
                    await _pushService.SendOrderNotificationAsync(order);
                });
            })
            
            // Map to confirmation
            .MapAsync(async order =>
            {
                var trackingNumber = await _shippingService.CreateShipmentAsync(order);
                
                return new OrderConfirmation
                {
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    Total = order.Total,
                    EstimatedDelivery = DateTime.Now.AddDays(3),
                    TrackingNumber = trackingNumber,
                    Items = order.Items.Select(i => new OrderItemDto
                    {
                        ProductName = i.ProductName,
                        Quantity = i.Quantity,
                        Price = i.Price
                    }).ToList()
                };
            });
    }
}
```

## ASP.NET Core Integration

### Configuration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add Communication services with options
builder.Services.AddCommunication(options =>
{
    // Show detailed error messages in development
    options.ShowErrorDetails = builder.Environment.IsDevelopment();
    
    // Include exception details in problem responses
    options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    
    // Custom problem details factory
    options.ProblemDetailsFactory = (problem, httpContext) =>
    {
        // Add request ID to all problems
        problem.Extensions["requestId"] = httpContext.TraceIdentifier;
        
        // Add timestamp
        problem.Extensions["timestamp"] = DateTime.UtcNow;
        
        // Add user info if authenticated
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            problem.Extensions["user"] = httpContext.User.Identity.Name;
        }
        
        return problem;
    };
});

// Add controllers with Communication filters
builder.Services.AddControllers(options =>
{
    // Add all Communication filters at once
    options.AddCommunicationFilters();
    
    // Or add specific filters individually
    options.Filters.Add<CommunicationExceptionFilter>(); // Converts exceptions to Results
    options.Filters.Add<CommunicationModelValidationFilter>(); // Handles model validation
    options.Filters.Add<ResultToActionResultFilter>(); // Converts Results to ActionResults
});

// Add SignalR with Communication support
builder.Services.AddSignalR(options =>
{
    options.AddCommunicationHubFilter();
});

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "My API", 
        Version = "v1",
        Description = "API using Result pattern with RFC 7807 Problem Details"
    });
    
    // Add XML comments for better documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Use Communication middleware - must be early in pipeline
app.UseCommunication();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
```

### Controller Implementation

```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;
    
    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }
    
    /// <summary>
    /// Get a product by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Product details or error</returns>
    /// <response code="200">Product found successfully</response>
    /// <response code="404">Product not found</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<Result<ProductDto>> GetProduct(int id)
    {
        _logger.LogInformation("Getting product {ProductId}", id);
        
        // The framework automatically converts this Result to appropriate HTTP response
        return await _productService.GetByIdAsync(id);
    }
    
    /// <summary>
    /// Get paginated list of products
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CollectionResult<ProductDto>), StatusCodes.Status200OK)]
    public async Task<CollectionResult<ProductDto>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? sortBy = "name",
        [FromQuery] bool descending = false)
    {
        var filter = new ProductFilter
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            Category = category,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            SortBy = sortBy,
            Descending = descending
        };
        
        return await _productService.GetProductsAsync(filter);
    }
    
    /// <summary>
    /// Create a new product
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<Result<ProductDto>> CreateProduct([FromBody] CreateProductDto dto)
    {
        // Model validation is handled automatically by CommunicationModelValidationFilter
        var result = await _productService.CreateAsync(dto);
        
        // Set Location header for created resource
        if (result.IsSuccess)
        {
            Response.Headers.Location = $"/api/products/{result.Value.Id}";
            Response.StatusCode = 201; // Created
        }
        
        return result;
    }
    
    /// <summary>
    /// Update an existing product
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<Result<ProductDto>> UpdateProduct(
        int id,
        [FromBody] UpdateProductDto dto)
    {
        if (id != dto.Id)
        {
            return Result<ProductDto>.FailBadRequest("ID in URL doesn't match ID in body");
        }
        
        return await _productService.UpdateAsync(id, dto);
    }
    
    /// <summary>
    /// Partially update a product
    /// </summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<Result<ProductDto>> PatchProduct(
        int id,
        [FromBody] JsonPatchDocument<UpdateProductDto> patchDoc)
    {
        if (patchDoc == null)
        {
            return Result<ProductDto>.FailBadRequest("Invalid patch document");
        }
        
        // Get existing product
        var getResult = await _productService.GetByIdAsync(id);
        if (getResult.IsFailed)
            return Result<ProductDto>.Fail(getResult.Problem);
        
        // Apply patch
        var productToUpdate = getResult.Value.ToUpdateDto();
        patchDoc.ApplyTo(productToUpdate, ModelState);
        
        if (!ModelState.IsValid)
        {
            return Result<ProductDto>.FailValidation(
                ModelState.SelectMany(x => x.Value.Errors.Select(e => (x.Key, e.ErrorMessage)))
                    .ToArray()
            );
        }
        
        return await _productService.UpdateAsync(id, productToUpdate);
    }
    
    /// <summary>
    /// Delete a product
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<Result> DeleteProduct(int id)
    {
        var result = await _productService.DeleteAsync(id);
        
        if (result.IsSuccess)
        {
            Response.StatusCode = 204; // No Content
        }
        
        return result;
    }
    
    /// <summary>
    /// Bulk import products from CSV
    /// </summary>
    [HttpPost("import")]
    [ProducesResponseType(typeof(ImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<Result<ImportResult>> ImportProducts(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return Result<ImportResult>.FailBadRequest("No file uploaded");
        }
        
        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return Result<ImportResult>.FailBadRequest("Only CSV files are supported");
        }
        
        if (file.Length > 10 * 1024 * 1024) // 10MB
        {
            return Result<ImportResult>.FailBadRequest("File size cannot exceed 10MB");
        }
        
        using var stream = file.OpenReadStream();
        return await _productService.ImportFromCsvAsync(stream);
    }
}
```

### Service Layer with Results

```csharp
public interface IProductService
{
    Task<Result<ProductDto>> GetByIdAsync(int id);
    Task<CollectionResult<ProductDto>> GetProductsAsync(ProductFilter filter);
    Task<Result<ProductDto>> CreateAsync(CreateProductDto dto);
    Task<Result<ProductDto>> UpdateAsync(int id, UpdateProductDto dto);
    Task<Result> DeleteAsync(int id);
    Task<Result<ImportResult>> ImportFromCsvAsync(Stream csvStream);
}

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IValidator<CreateProductDto> _createValidator;
    private readonly IValidator<UpdateProductDto> _updateValidator;
    private readonly ICacheService _cache;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ProductService> _logger;
    
    public async Task<Result<ProductDto>> GetByIdAsync(int id)
    {
        // Check cache first
        var cacheKey = $"product:{id}";
        if (await _cache.TryGetAsync<ProductDto>(cacheKey) is { } cached)
        {
            _logger.LogDebug("Product {ProductId} found in cache", id);
            return Result<ProductDto>.Succeed(cached);
        }
        
        // Get from database
        return await Result.TryAsync(async () =>
        {
            var product = await _repository.FindByIdAsync(id);
            if (product == null)
            {
                return Result<ProductDto>.FailNotFound($"Product with ID {id} not found");
            }
            
            if (product.IsDeleted)
            {
                return Result<ProductDto>.Fail(
                    "Product Deleted",
                    $"Product {id} has been deleted",
                    HttpStatusCode.Gone
                );
            }
            
            var dto = product.ToDto();
            
            // Cache for 5 minutes
            await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5));
            
            return Result<ProductDto>.Succeed(dto);
        });
    }
    
    public async Task<CollectionResult<ProductDto>> GetProductsAsync(ProductFilter filter)
    {
        return await CollectionResult<ProductDto>.From(async () =>
        {
            var query = _repository.Query();
            
            // Apply search
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                query = query.Where(p => 
                    p.Name.Contains(filter.Search) || 
                    p.Description.Contains(filter.Search) ||
                    p.Sku.Contains(filter.Search)
                );
            }
            
            // Apply category filter
            if (!string.IsNullOrWhiteSpace(filter.Category))
            {
                query = query.Where(p => p.Category == filter.Category);
            }
            
            // Apply price filters
            if (filter.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= filter.MinPrice.Value);
            }
            
            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);
            }
            
            // Get total count before pagination
            var totalCount = await query.CountAsync();
            
            // Apply sorting
            query = filter.SortBy?.ToLower() switch
            {
                "name" => filter.Descending ? 
                    query.OrderByDescending(p => p.Name) : 
                    query.OrderBy(p => p.Name),
                "price" => filter.Descending ? 
                    query.OrderByDescending(p => p.Price) : 
                    query.OrderBy(p => p.Price),
                "created" => filter.Descending ? 
                    query.OrderByDescending(p => p.CreatedAt) : 
                    query.OrderBy(p => p.CreatedAt),
                _ => query.OrderBy(p => p.Name)
            };
            
            // Apply pagination
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(p => p.ToDto())
                .ToListAsync();
            
            return CollectionResult<ProductDto>.Succeed(
                items,
                filter.Page,
                filter.PageSize,
                totalCount
            );
        });
    }
    
    public async Task<Result<ProductDto>> CreateAsync(CreateProductDto dto)
    {
        // Validate input
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return Result<ProductDto>.FailValidation(
                validationResult.Errors
                    .Select(e => (e.PropertyName, e.ErrorMessage))
                    .ToArray()
            );
        }
        
        // Check for duplicate SKU
        var existingProduct = await _repository.FindBySkuAsync(dto.Sku);
        if (existingProduct != null)
        {
            return Result<ProductDto>.Fail(
                "Duplicate SKU",
                $"A product with SKU '{dto.Sku}' already exists",
                HttpStatusCode.Conflict
            );
        }
        
        // Create product
        return await Result.TryAsync(async () =>
        {
            var product = new Product
            {
                Sku = dto.Sku,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Category = dto.Category,
                StockQuantity = dto.InitialStock,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.Id
            };
            
            await _repository.AddAsync(product);
            await _repository.SaveChangesAsync();
            
            _logger.LogInformation("Product {ProductId} created with SKU {Sku}", product.Id, product.Sku);
            
            return product;
        })
        .TapAsync(async product =>
        {
            // Publish event (fire and forget)
            await _eventBus.PublishAsync(new ProductCreatedEvent
            {
                ProductId = product.Id,
                Sku = product.Sku,
                Name = product.Name,
                Price = product.Price,
                CreatedAt = product.CreatedAt
            });
            
            // Invalidate cache
            await _cache.InvalidateAsync("products:*");
        })
        .MapAsync(product => Task.FromResult(product.ToDto()));
    }
    
    public async Task<Result<ImportResult>> ImportFromCsvAsync(Stream csvStream)
    {
        var importResult = new ImportResult
        {
            StartedAt = DateTime.UtcNow,
            ProcessedCount = 0,
            SuccessCount = 0,
            FailedCount = 0,
            Errors = new List<ImportError>()
        };
        
        try
        {
            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            
            var records = csv.GetRecords<ProductCsvRecord>().ToList();
            
            foreach (var (record, index) in records.Select((r, i) => (r, i)))
            {
                importResult.ProcessedCount++;
                
                // Validate record
                if (string.IsNullOrWhiteSpace(record.Sku))
                {
                    importResult.FailedCount++;
                    importResult.Errors.Add(new ImportError
                    {
                        Row = index + 2, // +1 for header, +1 for 1-based
                        Field = "SKU",
                        Error = "SKU is required"
                    });
                    continue;
                }
                
                // Create product
                var createResult = await CreateAsync(new CreateProductDto
                {
                    Sku = record.Sku,
                    Name = record.Name,
                    Description = record.Description,
                    Price = record.Price,
                    Category = record.Category,
                    InitialStock = record.Stock
                });
                
                if (createResult.IsSuccess)
                {
                    importResult.SuccessCount++;
                }
                else
                {
                    importResult.FailedCount++;
                    importResult.Errors.Add(new ImportError
                    {
                        Row = index + 2,
                        Sku = record.Sku,
                        Error = createResult.Problem.Detail
                    });
                }
            }
            
            importResult.CompletedAt = DateTime.UtcNow;
            importResult.Duration = importResult.CompletedAt.Value - importResult.StartedAt;
            
            return Result<ImportResult>.Succeed(importResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import products from CSV");
            return Result<ImportResult>.Fail("Import Failed", ex.Message);
        }
    }
}
```

## Orleans Integration

### Silo Configuration

```csharp
var builder = Host.CreateDefaultBuilder(args)
    .UseOrleans(siloBuilder =>
    {
        siloBuilder
            .UseLocalhostClustering()
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "dev";
                options.ServiceId = "MyService";
            })
            .ConfigureApplicationParts(parts =>
            {
                parts.AddApplicationPart(typeof(UserGrain).Assembly).WithReferences();
            })
            // Enable Communication support for Orleans
            .UseOrleansCommunication()
            .AddMemoryGrainStorage("users")
            .AddMemoryGrainStorage("products");
    })
    .ConfigureServices(services =>
    {
        services.AddCommunication();
        services.AddSingleton<IUserValidator, UserValidator>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
    });

await builder.RunConsoleAsync();
```

### Grain Implementation

```csharp
public interface IUserGrain : IGrainWithGuidKey
{
    Task<Result<UserState>> GetUserAsync();
    Task<Result<UserState>> CreateUserAsync(CreateUserCommand command);
    Task<Result<UserState>> UpdateUserAsync(UpdateUserCommand command);
    Task<Result> ChangePasswordAsync(string currentPassword, string newPassword);
    Task<Result> ActivateUserAsync();
    Task<Result> DeactivateUserAsync();
    Task<CollectionResult<UserActivity>> GetActivitiesAsync(int page, int pageSize);
}

[Serializable]
public class UserState
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public string PasswordHash { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<UserActivity> Activities { get; set; } = new();
}

public class UserGrain : Grain, IUserGrain
{
    private readonly IPersistentState<UserState> _state;
    private readonly IUserValidator _validator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<UserGrain> _logger;
    
    public UserGrain(
        [PersistentState("user", "users")] IPersistentState<UserState> state,
        IUserValidator validator,
        IPasswordHasher passwordHasher,
        ILogger<UserGrain> logger)
    {
        _state = state;
        _validator = validator;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }
    
    public override async Task OnActivateAsync()
    {
        _logger.LogInformation("User grain {UserId} activated", this.GetPrimaryKey());
        await base.OnActivateAsync();
    }
    
    public async Task<Result<UserState>> GetUserAsync()
    {
        if (!_state.RecordExists)
        {
            return Result<UserState>.FailNotFound(
                $"User {this.GetPrimaryKey()} not found"
            );
        }
        
        if (!_state.State.IsActive)
        {
            return Result<UserState>.FailForbidden(
                "User account is deactivated"
            );
        }
        
        return Result<UserState>.Succeed(_state.State);
    }
    
    public async Task<Result<UserState>> CreateUserAsync(CreateUserCommand command)
    {
        // Check if user already exists
        if (_state.RecordExists)
        {
            return Result<UserState>.Fail(
                "User Already Exists",
                $"User {this.GetPrimaryKey()} already exists",
                HttpStatusCode.Conflict
            );
        }
        
        // Validate command
        var validationResult = await _validator.ValidateCreateCommandAsync(command);
        if (validationResult.IsFailed)
        {
            return Result<UserState>.Fail(validationResult.Problem!);
        }
        
        // Check email uniqueness
        var emailGrain = GrainFactory.GetGrain<IEmailIndexGrain>(0);
        var emailExists = await emailGrain.CheckEmailExistsAsync(command.Email);
        if (emailExists)
        {
            return Result<UserState>.Fail(
                "Email Already Taken",
                $"The email {command.Email} is already registered",
                HttpStatusCode.Conflict
            );
        }
        
        // Create user
        _state.State = new UserState
        {
            Id = this.GetPrimaryKey(),
            Email = command.Email,
            Name = command.Name,
            PasswordHash = _passwordHasher.HashPassword(command.Password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Activities = new List<UserActivity>
            {
                new UserActivity
                {
                    Type = "AccountCreated",
                    Description = "Account was created",
                    Timestamp = DateTime.UtcNow
                }
            }
        };
        
        // Register email
        await emailGrain.RegisterEmailAsync(command.Email, _state.State.Id);
        
        // Persist state
        await _state.WriteStateAsync();
        
        _logger.LogInformation("User {UserId} created with email {Email}", 
            _state.State.Id, _state.State.Email);
        
        // Send welcome email (fire and forget)
        var emailServiceGrain = GrainFactory.GetGrain<IEmailServiceGrain>(0);
        await emailServiceGrain.SendWelcomeEmailAsync(_state.State.Id, _state.State.Email);
        
        return Result<UserState>.Succeed(_state.State);
    }
    
    public async Task<Result<UserState>> UpdateUserAsync(UpdateUserCommand command)
    {
        if (!_state.RecordExists)
        {
            return Result<UserState>.FailNotFound(
                $"User {this.GetPrimaryKey()} not found"
            );
        }
        
        // Validate command
        var validationResult = await _validator.ValidateUpdateCommandAsync(command);
        if (validationResult.IsFailed)
        {
            return Result<UserState>.Fail(validationResult.Problem!);
        }
        
        // Check email uniqueness if changing email
        if (!string.IsNullOrEmpty(command.Email) && command.Email != _state.State.Email)
        {
            var emailGrain = GrainFactory.GetGrain<IEmailIndexGrain>(0);
            var emailExists = await emailGrain.CheckEmailExistsAsync(command.Email);
            
            if (emailExists)
            {
                return Result<UserState>.Fail(
                    "Email Already Taken",
                    $"The email {command.Email} is already registered",
                    HttpStatusCode.Conflict
                );
            }
            
            // Update email index
            await emailGrain.UpdateEmailAsync(_state.State.Email, command.Email, _state.State.Id);
            _state.State.Email = command.Email;
        }
        
        // Update fields
        if (!string.IsNullOrEmpty(command.Name))
        {
            _state.State.Name = command.Name;
        }
        
        // Add activity
        _state.State.Activities.Add(new UserActivity
        {
            Type = "ProfileUpdated",
            Description = "Profile information was updated",
            Timestamp = DateTime.UtcNow
        });
        
        // Keep only last 100 activities
        if (_state.State.Activities.Count > 100)
        {
            _state.State.Activities = _state.State.Activities
                .OrderByDescending(a => a.Timestamp)
                .Take(100)
                .ToList();
        }
        
        // Persist state
        await _state.WriteStateAsync();
        
        _logger.LogInformation("User {UserId} updated", _state.State.Id);
        
        return Result<UserState>.Succeed(_state.State);
    }
    
    public async Task<Result> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        if (!_state.RecordExists)
        {
            return Result.FailNotFound($"User {this.GetPrimaryKey()} not found");
        }
        
        // Verify current password
        if (!_passwordHasher.VerifyPassword(_state.State.PasswordHash, currentPassword))
        {
            return Result.FailUnauthorized("Current password is incorrect");
        }
        
        // Validate new password
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            return Result.FailValidation(("password", "Password cannot be empty"));
        }
        
        if (newPassword.Length < 8)
        {
            return Result.FailValidation(("password", "Password must be at least 8 characters"));
        }
        
        if (!newPassword.Any(char.IsDigit))
        {
            return Result.FailValidation(("password", "Password must contain at least one digit"));
        }
        
        if (!newPassword.Any(char.IsUpper))
        {
            return Result.FailValidation(("password", "Password must contain at least one uppercase letter"));
        }
        
        // Update password
        _state.State.PasswordHash = _passwordHasher.HashPassword(newPassword);
        
        // Add activity
        _state.State.Activities.Add(new UserActivity
        {
            Type = "PasswordChanged",
            Description = "Password was changed",
            Timestamp = DateTime.UtcNow
        });
        
        // Persist state
        await _state.WriteStateAsync();
        
        // Invalidate all sessions
        var sessionGrain = GrainFactory.GetGrain<ISessionManagerGrain>(_state.State.Id);
        await sessionGrain.InvalidateAllSessionsAsync();
        
        _logger.LogInformation("User {UserId} changed password", _state.State.Id);
        
        return Result.Succeed();
    }
    
    public async Task<CollectionResult<UserActivity>> GetActivitiesAsync(int page, int pageSize)
    {
        if (!_state.RecordExists)
        {
            return CollectionResult<UserActivity>.FailNotFound(
                $"User {this.GetPrimaryKey()} not found"
            );
        }
        
        var activities = _state.State.Activities
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        
        return CollectionResult<UserActivity>.Succeed(
            activities,
            page,
            pageSize,
            _state.State.Activities.Count
        );
    }
}
```

## SignalR Integration

### Hub Implementation

```csharp
public class NotificationHub : Hub
{
    private readonly INotificationService _notificationService;
    private readonly IUserService _userService;
    private readonly ILogger<NotificationHub> _logger;
    
    public NotificationHub(
        INotificationService notificationService,
        IUserService userService,
        ILogger<NotificationHub> logger)
    {
        _notificationService = notificationService;
        _userService = userService;
        _logger = logger;
    }
    
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            Context.Abort();
            return;
        }
        
        _logger.LogInformation("User {UserId} connected to notifications", userId);
        
        // Add to user's personal group
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        
        // Add to role-based groups
        var userRoles = await _userService.GetUserRolesAsync(userId);
        foreach (var role in userRoles)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"role-{role}");
        }
        
        // Send any pending notifications
        var pendingNotifications = await _notificationService.GetPendingNotificationsAsync(userId);
        if (pendingNotifications.Any())
        {
            await Clients.Caller.SendAsync("PendingNotifications", pendingNotifications);
        }
        
        await base.OnConnectedAsync();
    }
    
    public async Task<Result> MarkAsRead(Guid notificationId)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.FailUnauthorized("User not authenticated");
        }
        
        var result = await _notificationService.MarkAsReadAsync(userId, notificationId);
        
        if (result.IsSuccess)
        {
            // Notify other connections of the same user
            await Clients.OthersInGroup($"user-{userId}")
                .SendAsync("NotificationRead", notificationId);
        }
        
        return result;
    }
    
    public async Task<Result> MarkAllAsRead()
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.FailUnauthorized("User not authenticated");
        }
        
        var result = await _notificationService.MarkAllAsReadAsync(userId);
        
        if (result.IsSuccess)
        {
            await Clients.OthersInGroup($"user-{userId}")
                .SendAsync("AllNotificationsRead");
        }
        
        return result;
    }
    
    public async Task<CollectionResult<NotificationDto>> GetNotifications(
        int page = 1,
        int pageSize = 20,
        bool unreadOnly = false)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            return CollectionResult<NotificationDto>.FailUnauthorized("User not authenticated");
        }
        
        return await _notificationService.GetNotificationsAsync(userId, page, pageSize, unreadOnly);
    }
    
    public async Task<Result> SubscribeToTopic(string topic)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.FailUnauthorized("User not authenticated");
        }
        
        // Validate topic
        if (!await _notificationService.IsValidTopicAsync(topic))
        {
            return Result.FailBadRequest($"Invalid topic: {topic}");
        }
        
        // Check permissions
        if (!await _userService.CanSubscribeToTopicAsync(userId, topic))
        {
            return Result.FailForbidden($"You don't have permission to subscribe to {topic}");
        }
        
        // Add to topic group
        await Groups.AddToGroupAsync(Context.ConnectionId, $"topic-{topic}");
        
        // Save subscription
        await _notificationService.SubscribeToTopicAsync(userId, topic);
        
        _logger.LogInformation("User {UserId} subscribed to topic {Topic}", userId, topic);
        
        return Result.Succeed();
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        
        if (exception != null)
        {
            _logger.LogError(exception, "User {UserId} disconnected with error", userId);
        }
        else
        {
            _logger.LogInformation("User {UserId} disconnected", userId);
        }
        
        await base.OnDisconnectedAsync(exception);
    }
}

// Service to send notifications
public class NotificationSender
{
    private readonly IHubContext<NotificationHub> _hubContext;
    
    public NotificationSender(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }
    
    public async Task SendToUserAsync(string userId, NotificationDto notification)
    {
        await _hubContext.Clients
            .Group($"user-{userId}")
            .SendAsync("NewNotification", notification);
    }
    
    public async Task SendToRoleAsync(string role, NotificationDto notification)
    {
        await _hubContext.Clients
            .Group($"role-{role}")
            .SendAsync("NewNotification", notification);
    }
    
    public async Task SendToTopicAsync(string topic, NotificationDto notification)
    {
        await _hubContext.Clients
            .Group($"topic-{topic}")
            .SendAsync("NewNotification", notification);
    }
    
    public async Task BroadcastAsync(NotificationDto notification)
    {
        await _hubContext.Clients
            .All
            .SendAsync("Broadcast", notification);
    }
}
```

## Validation Handling

### Creating Validation Errors

```csharp
public Result<User> CreateUser(CreateUserDto dto)
{
    var errors = new List<(string field, string message)>();
    
    // Validate email
    if (string.IsNullOrWhiteSpace(dto.Email))
    {
        errors.Add(("email", "Email is required"));
    }
    else if (!IsValidEmail(dto.Email))
    {
        errors.Add(("email", "Email format is invalid"));
    }
    
    // Validate password
    if (string.IsNullOrWhiteSpace(dto.Password))
    {
        errors.Add(("password", "Password is required"));
    }
    else
    {
        if (dto.Password.Length < 8)
        {
            errors.Add(("password", "Password must be at least 8 characters"));
        }
        if (!dto.Password.Any(char.IsDigit))
        {
            errors.Add(("password", "Password must contain at least one digit"));
        }
        if (!dto.Password.Any(char.IsUpper))
        {
            errors.Add(("password", "Password must contain at least one uppercase letter"));
        }
    }
    
    // Validate age
    if (dto.Age < 18)
    {
        errors.Add(("age", "You must be 18 or older"));
    }
    
    // Return validation errors if any
    if (errors.Any())
    {
        return Result<User>.FailValidation(errors.ToArray());
    }
    
    // Continue with user creation...
}
```

### Using FluentValidation

```csharp
public class CreateUserValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email format is invalid");
        
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit")
            .Matches(@"[!@#$%^&*]").WithMessage("Password must contain at least one special character");
        
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .Length(2, 100).WithMessage("Name must be between 2 and 100 characters");
        
        RuleFor(x => x.Age)
            .GreaterThanOrEqualTo(18).WithMessage("You must be 18 or older");
        
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[1-9]\d{1,14}$").When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("Invalid phone number format");
    }
}

// In service
public async Task<Result<User>> CreateUserAsync(CreateUserDto dto)
{
    var validationResult = await _validator.ValidateAsync(dto);
    
    if (!validationResult.IsValid)
    {
        return Result<User>.FailValidation(
            validationResult.Errors
                .Select(e => (e.PropertyName, e.ErrorMessage))
                .ToArray()
        );
    }
    
    // Continue with user creation...
}
```

### Accessing Validation Errors

```csharp
var result = CreateUser(dto);

if (result.IsFailed && result.Problem.StatusCode == 400)
{
    // Get validation errors
    var errors = result.Problem.GetValidationErrors();
    
    if (errors != null)
    {
        foreach (var (field, messages) in errors)
        {
            Console.WriteLine($"{field}:");
            foreach (var message in messages)
            {
                Console.WriteLine($"  - {message}");
            }
        }
    }
}

// Or check specific field
if (result.HasProblem && result.Problem.HasValidationError("email"))
{
    var emailErrors = result.Problem.GetValidationErrors("email");
    // Handle email errors
}
```

## Error Handling Strategies

### Global Error Handler

```csharp
public class GlobalErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalErrorHandlingMiddleware> _logger;
    
    public GlobalErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var problem = exception switch
        {
            ProblemException problemEx => problemEx.Problem,
            ValidationException validationEx => Problem.CreateValidation(
                validationEx.Errors.Select(e => (e.PropertyName, new[] { e.ErrorMessage }))
            ),
            UnauthorizedAccessException => Problem.Create(
                "Unauthorized",
                "You are not authorized to perform this action",
                401
            ),
            KeyNotFoundException or FileNotFoundException => Problem.Create(
                "Not Found",
                exception.Message,
                404
            ),
            TimeoutException => Problem.Create(
                "Request Timeout",
                "The operation timed out",
                408
            ),
            _ => Problem.Create(
                "Internal Server Error",
                "An unexpected error occurred",
                500
            )
        };
        
        // Add additional context
        problem.Extensions["traceId"] = context.TraceIdentifier;
        problem.Extensions["timestamp"] = DateTime.UtcNow;
        
        context.Response.StatusCode = problem.StatusCode;
        context.Response.ContentType = "application/problem+json";
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
```

### Retry Strategy with Results

```csharp
public static class ResultRetryExtensions
{
    public static async Task<Result<T>> RetryAsync<T>(
        Func<Task<Result<T>>> operation,
        int maxAttempts = 3,
        TimeSpan? delay = null)
    {
        delay ??= TimeSpan.FromSeconds(1);
        
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var result = await operation();
            
            if (result.IsSuccess)
            {
                return result;
            }
            
            // Don't retry on client errors (4xx)
            if (result.Problem.StatusCode >= 400 && result.Problem.StatusCode < 500)
            {
                return result;
            }
            
            // Last attempt, return the failure
            if (attempt == maxAttempts)
            {
                return result;
            }
            
            // Wait before retrying (with exponential backoff)
            var waitTime = TimeSpan.FromMilliseconds(delay.Value.TotalMilliseconds * Math.Pow(2, attempt - 1));
            await Task.Delay(waitTime);
        }
        
        return Result<T>.Fail("Max retries exceeded");
    }
}

// Usage
var result = await ResultRetryExtensions.RetryAsync(
    async () => await _httpClient.GetDataAsync(),
    maxAttempts: 3,
    delay: TimeSpan.FromSeconds(1)
);
```

## Testing

### Unit Testing with Results

```csharp
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly UserService _service;
    
    public UserServiceTests()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _emailServiceMock = new Mock<IEmailService>();
        _service = new UserService(_repositoryMock.Object, _emailServiceMock.Object);
    }
    
    [Fact]
    public async Task GetUser_WhenUserExists_ReturnsSuccess()
    {
        // Arrange
        var userId = 123;
        var expectedUser = new User 
        { 
            Id = userId, 
            Name = "John Doe",
            Email = "john@example.com" 
        };
        
        _repositoryMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(expectedUser);
        
        // Act
        var result = await _service.GetUserAsync(userId);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(userId, result.Value.Id);
        Assert.Equal("John Doe", result.Value.Name);
        Assert.Equal("john@example.com", result.Value.Email);
    }
    
    [Fact]
    public async Task GetUser_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = 999;
        _repositoryMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((User?)null);
        
        // Act
        var result = await _service.GetUserAsync(userId);
        
        // Assert
        Assert.True(result.IsFailed);
        Assert.True(result.HasProblem);
        Assert.Equal(404, result.Problem.StatusCode);
        Assert.Equal("Not Found", result.Problem.Title);
        Assert.Contains("999", result.Problem.Detail);
    }
    
    [Fact]
    public async Task CreateUser_WithInvalidEmail_ReturnsValidationError()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            Email = "invalid-email",
            Name = "John Doe",
            Password = "Password123!"
        };
        
        // Act
        var result = await _service.CreateUserAsync(dto);
        
        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal(400, result.Problem.StatusCode);
        
        var errors = result.Problem.GetValidationErrors();
        Assert.NotNull(errors);
        Assert.ContainsKey("email", errors);
        Assert.Contains("Email format is invalid", errors["email"]);
    }
    
    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ReturnsConflict()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            Email = "existing@example.com",
            Name = "John Doe",
            Password = "Password123!"
        };
        
        _repositoryMock
            .Setup(x => x.ExistsByEmailAsync(dto.Email))
            .ReturnsAsync(true);
        
        // Act
        var result = await _service.CreateUserAsync(dto);
        
        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal(409, result.Problem.StatusCode);
        Assert.Equal("Email Already Taken", result.Problem.Title);
    }
    
    [Theory]
    [InlineData("", "Password is required")]
    [InlineData("short", "Password must be at least 8 characters")]
    [InlineData("nouppercasehere", "Password must contain at least one uppercase letter")]
    [InlineData("NOLOWERCASEHERE", "Password must contain at least one lowercase letter")]
    [InlineData("NoDigitsHere", "Password must contain at least one digit")]
    public async Task CreateUser_WithInvalidPassword_ReturnsValidationError(
        string password,
        string expectedError)
    {
        // Arrange
        var dto = new CreateUserDto
        {
            Email = "test@example.com",
            Name = "John Doe",
            Password = password
        };
        
        // Act
        var result = await _service.CreateUserAsync(dto);
        
        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal(400, result.Problem.StatusCode);
        
        var errors = result.Problem.GetValidationErrors();
        Assert.NotNull(errors);
        Assert.ContainsKey("password", errors);
        Assert.Contains(expectedError, errors["password"]);
    }
}
```

### Integration Testing

```csharp
public class UserApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    public UserApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real database with in-memory
                services.RemoveAll<DbContext>();
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });
                
                // Replace external services with mocks
                services.RemoveAll<IEmailService>();
                services.AddSingleton<IEmailService>(new Mock<IEmailService>().Object);
            });
        });
        
        _client = _factory.CreateClient();
    }
    
    [Fact]
    public async Task GetUser_ReturnsUser_WhenExists()
    {
        // Arrange
        await SeedUser(new User { Id = 1, Name = "Test User", Email = "test@example.com" });
        
        // Act
        var response = await _client.GetAsync("/api/users/1");
        
        // Assert
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Result<UserDto>>(content);
        
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Test User", result.Value.Name);
    }
    
    [Fact]
    public async Task GetUser_Returns404_WhenNotExists()
    {
        // Act
        var response = await _client.GetAsync("/api/users/999");
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(content);
        
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Not Found", problem.Title);
    }
    
    [Fact]
    public async Task CreateUser_ReturnsCreated_WhenValid()
    {
        // Arrange
        var newUser = new CreateUserDto
        {
            Email = "new@example.com",
            Name = "New User",
            Password = "Password123!"
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/users", newUser);
        
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Result<UserDto>>(content);
        
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal("new@example.com", result.Value.Email);
    }
    
    [Fact]
    public async Task CreateUser_Returns400_WhenInvalid()
    {
        // Arrange
        var invalidUser = new CreateUserDto
        {
            Email = "", // Invalid
            Name = "", // Invalid
            Password = "123" // Invalid
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/users", invalidUser);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<ValidationProblemDetails>(content);
        
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.Equal("Validation Failed", problem.Title);
        Assert.NotEmpty(problem.Errors);
        Assert.ContainsKey("email", problem.Errors);
        Assert.ContainsKey("name", problem.Errors);
        Assert.ContainsKey("password", problem.Errors);
    }
}
```

## Performance

### Benchmark Results

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class ResultVsExceptionBenchmark
{
    [Benchmark]
    public void ThrowException()
    {
        try
        {
            throw new InvalidOperationException("Operation failed");
        }
        catch (InvalidOperationException)
        {
            // Handle exception
        }
    }
    
    [Benchmark]
    public void ReturnResult()
    {
        var result = Result.Fail("Operation failed");
        if (result.IsFailed)
        {
            // Handle failure
        }
    }
}
```

Results show that returning a Result is significantly faster than throwing an exception:

| Method | Mean | Error | StdDev | Allocated |
|--------|------|-------|--------|-----------|
| ThrowException | 5,847 ns | 98 ns | 92 ns | 352 B |
| ReturnResult | 6 ns | 0.1 ns | 0.1 ns | 24 B |

## Best Practices

### 1. Use Specific Failure Methods

```csharp
// Good - Specific and clear
return Result.FailNotFound($"User {id} not found");
return Result.FailUnauthorized("Invalid credentials");
return Result.FailValidation(("email", "Invalid format"));

// Bad - Generic
return Result.Fail("Error");
return Result.Fail("Something went wrong");
```

### 2. Include Context in Error Messages

```csharp
// Good - Provides context
return Result.Fail(
    "Payment Failed",
    $"Unable to charge ${amount:C} to card ending in {card.Last4}. Bank declined with code: {declineCode}",
    HttpStatusCode.PaymentRequired
);

// Bad - No context
return Result.Fail("Payment failed");
```

### 3. Use Railway Programming for Complex Flows

```csharp
// Good - Clean and readable
return await ValidateInput(request)
    .BindAsync(r => ProcessPayment(r))
    .BindAsync(p => SaveOrder(p))
    .MapAsync(o => o.ToDto());

// Bad - Nested if statements
var validateResult = await ValidateInput(request);
if (validateResult.IsFailed)
    return Result<OrderDto>.Fail(validateResult.Problem);

var paymentResult = await ProcessPayment(validateResult.Value);
if (paymentResult.IsFailed)
    return Result<OrderDto>.Fail(paymentResult.Problem);

var orderResult = await SaveOrder(paymentResult.Value);
if (orderResult.IsFailed)
    return Result<OrderDto>.Fail(orderResult.Problem);

return Result<OrderDto>.Succeed(orderResult.Value.ToDto());
```

### 4. Don't Mix Exceptions and Results

```csharp
// Bad - Mixing patterns
public Result<User> GetUser(int id)
{
    try
    {
        var user = _repository.FindById(id); // May throw
        if (user == null)
            return Result<User>.FailNotFound($"User {id} not found");
        return Result<User>.Succeed(user);
    }
    catch (Exception ex)
    {
        throw; // Don't throw when returning Result
    }
}

// Good - Consistent Result pattern
public Result<User> GetUser(int id)
{
    return Result.Try(() =>
    {
        var user = _repository.FindById(id);
        if (user == null)
            return Result<User>.FailNotFound($"User {id} not found");
        return Result<User>.Succeed(user);
    });
}
```

### 5. Use CollectionResult for Paginated Data

```csharp
// Good - Includes pagination metadata
public CollectionResult<Product> GetProducts(int page, int pageSize)
{
    var query = _db.Products;
    var total = query.Count();
    var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
    
    return CollectionResult<Product>.Succeed(items, page, pageSize, total);
}

// Bad - No pagination info
public Result<List<Product>> GetProducts(int page, int pageSize)
{
    var items = _db.Products.Skip((page - 1) * pageSize).Take(pageSize).ToList();
    return Result<List<Product>>.Succeed(items);
}
```

## Migration Guide

### From Exceptions to Results

Before:
```csharp
public class UserService
{
    public User GetUser(int id)
    {
        var user = _repository.FindById(id);
        if (user == null)
            throw new NotFoundException($"User {id} not found");
        
        if (!user.IsActive)
            throw new ForbiddenException("User is inactive");
        
        return user;
    }
    
    public void DeleteUser(int id)
    {
        var user = GetUser(id); // May throw
        _repository.Delete(user);
    }
}
```

After:
```csharp
public class UserService
{
    public Result<User> GetUser(int id)
    {
        var user = _repository.FindById(id);
        if (user == null)
            return Result<User>.FailNotFound($"User {id} not found");
        
        if (!user.IsActive)
            return Result<User>.FailForbidden("User is inactive");
        
        return Result<User>.Succeed(user);
    }
    
    public Result DeleteUser(int id)
    {
        var userResult = GetUser(id);
        if (userResult.IsFailed)
            return Result.Fail(userResult.Problem);
        
        _repository.Delete(userResult.Value);
        return Result.Succeed();
    }
}
```

### Gradual Migration Strategy

1. **Start with new code** - Use Results for all new features
2. **Wrap existing code** - Use Try pattern to wrap exception-throwing code
3. **Update interfaces** - Change method signatures to return Results
4. **Update callers** - Update code that calls changed methods
5. **Remove exception handling** - Remove try-catch blocks as you migrate

## Real-World Examples

### E-Commerce Order Processing

```csharp
public class OrderProcessor
{
    public async Task<Result<OrderConfirmation>> ProcessOrderAsync(CartCheckoutRequest request)
    {
        // Validate cart exists and is not empty
        var cartResult = await _cartService.GetCartAsync(request.CartId);
        if (cartResult.IsFailed)
            return Result<OrderConfirmation>.Fail(cartResult.Problem);
        
        if (!cartResult.Value.Items.Any())
            return Result<OrderConfirmation>.FailBadRequest("Cart is empty");
        
        // Process the order
        return await ValidateCustomer(request.CustomerId)
            .BindAsync(customer => ValidateShippingAddress(request.ShippingAddress))
            .BindAsync(address => CheckInventory(cartResult.Value.Items))
            .BindAsync(items => ApplyCoupons(items, request.CouponCodes))
            .BindAsync(items => CalculateTotals(items, request.ShippingAddress))
            .BindAsync(order => ProcessPayment(order, request.PaymentMethod))
            .BindAsync(order => CreateOrderRecord(order))
            .TapAsync(order => UpdateInventory(order.Items))
            .TapAsync(order => SendOrderEmails(order))
            .TapAsync(order => PublishOrderEvent(order))
            .MapAsync(order => GenerateConfirmation(order));
    }
    
    private async Task<Result<Customer>> ValidateCustomer(int customerId)
    {
        var customer = await _customerRepository.FindByIdAsync(customerId);
        if (customer == null)
            return Result<Customer>.FailNotFound($"Customer {customerId} not found");
        
        if (customer.IsBlocked)
            return Result<Customer>.FailForbidden("Customer account is blocked");
        
        if (!customer.EmailVerified)
            return Result<Customer>.FailForbidden("Please verify your email before placing orders");
        
        return Result<Customer>.Succeed(customer);
    }
    
    private async Task<Result<List<OrderItem>>> CheckInventory(List<CartItem> items)
    {
        var orderItems = new List<OrderItem>();
        
        foreach (var item in items)
        {
            var stockResult = await _inventoryService.CheckStockAsync(item.ProductId, item.Quantity);
            if (stockResult.IsFailed)
                return Result<List<OrderItem>>.Fail(stockResult.Problem);
            
            if (stockResult.Value.AvailableQuantity < item.Quantity)
            {
                return Result<List<OrderItem>>.Fail(
                    "Insufficient Stock",
                    $"Only {stockResult.Value.AvailableQuantity} units of {item.ProductName} available",
                    HttpStatusCode.Conflict
                );
            }
            
            orderItems.Add(new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = stockResult.Value.CurrentPrice
            });
        }
        
        return Result<List<OrderItem>>.Succeed(orderItems);
    }
}
```

### Banking Transfer Service

```csharp
public class TransferService
{
    public async Task<Result<TransferReceipt>> TransferMoneyAsync(TransferRequest request)
    {
        // Validate amount
        if (request.Amount <= 0)
            return Result<TransferReceipt>.FailBadRequest("Transfer amount must be positive");
        
        if (request.Amount > 1_000_000)
            return Result<TransferReceipt>.FailBadRequest("Transfer amount exceeds maximum limit");
        
        // Process transfer
        return await ValidateAccounts(request.FromAccountId, request.ToAccountId)
            .BindAsync(accounts => CheckAccountStatus(accounts))
            .BindAsync(accounts => CheckBalance(accounts.from, request.Amount))
            .BindAsync(accounts => CheckDailyLimit(accounts.from, request.Amount))
            .BindAsync(accounts => CheckFraudRules(accounts, request))
            .BindAsync(accounts => ExecuteTransfer(accounts, request))
            .TapAsync(transfer => LogTransfer(transfer))
            .TapAsync(transfer => SendNotifications(transfer))
            .MapAsync(transfer => GenerateReceipt(transfer));
    }
    
    private async Task<Result<(Account from, Account to)>> ValidateAccounts(
        string fromAccountId, 
        string toAccountId)
    {
        if (fromAccountId == toAccountId)
            return Result<(Account, Account)>.FailBadRequest("Cannot transfer to the same account");
        
        var fromAccount = await _accountRepository.FindByIdAsync(fromAccountId);
        if (fromAccount == null)
            return Result<(Account, Account)>.FailNotFound($"Source account {fromAccountId} not found");
        
        var toAccount = await _accountRepository.FindByIdAsync(toAccountId);
        if (toAccount == null)
            return Result<(Account, Account)>.FailNotFound($"Destination account {toAccountId} not found");
        
        return Result<(Account, Account)>.Succeed((fromAccount, toAccount));
    }
    
    private Result<(Account from, Account to)> CheckAccountStatus((Account from, Account to) accounts)
    {
        if (accounts.from.Status == AccountStatus.Frozen)
            return Result<(Account, Account)>.FailForbidden("Source account is frozen");
        
        if (accounts.from.Status == AccountStatus.Closed)
            return Result<(Account, Account)>.FailForbidden("Source account is closed");
        
        if (accounts.to.Status == AccountStatus.Closed)
            return Result<(Account, Account)>.FailForbidden("Destination account is closed");
        
        return Result<(Account, Account)>.Succeed(accounts);
    }
    
    private Result<(Account from, Account to)> CheckBalance(Account fromAccount, decimal amount)
    {
        if (fromAccount.Balance < amount)
        {
            return Result<(Account, Account)>.Fail(
                "Insufficient Funds",
                $"Available balance: {fromAccount.Balance:C}, Required: {amount:C}",
                HttpStatusCode.PaymentRequired
            );
        }
        
        return Result<(Account, Account)>.Succeed((fromAccount, null));
    }
}
```

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md)

## License

MIT

---

**Made with ❤️ by [ManagedCode](https://github.com/managedcode)**
