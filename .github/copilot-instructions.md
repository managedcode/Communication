# ManagedCode.Communication

Result pattern for .NET that replaces exceptions with type-safe return values. Features railway-oriented programming, ASP.NET Core integration, RFC 7807 Problem Details, and built-in pagination.

## Build Commands

```bash
dotnet restore
dotnet build
dotnet test
```

## Project Structure

- **ManagedCode.Communication** - Core Result types and railway-oriented programming extensions
- **ManagedCode.Communication.AspNetCore** - ASP.NET Core integration, filters, and middleware
- **ManagedCode.Communication.Orleans** - Microsoft Orleans serialization support
- **ManagedCode.Communication.Tests** - Test suite
- **ManagedCode.Communication.Benchmark** - Performance benchmarks

## Key Concepts

This library implements the Result pattern for functional error handling without exceptions:

**Core Result Types:**
- `Result` - Success/failure without a value
- `Result<T>` - Success with value T or failure  
- `CollectionResult<T>` - Collections with built-in pagination
- `Problem` - RFC 7807 compliant error details

**Railway-Oriented Programming:**
Chain operations using functional combinators like `Map`, `Bind`/`Then`, `Tap`/`Do`, `Match`, and `Compensate`.

```csharp
// Basic Result usage
var success = Result.Succeed();
var failure = Result.Fail("Error message");

// Generic Result with value
var result = Result<string>.Succeed("Hello World");
if (result.IsSuccess)
{
    Console.WriteLine(result.Value);
}

// Railway-oriented programming
var chainResult = ValidateEmail("test@example.com")
    .Then(email => ProcessEmail(email))
    .Then(processed => SaveToDatabase(processed));
```

## Code Style & Conventions

**Framework Configuration:**
- Target: .NET 9.0
- Nullable reference types enabled
- TreatWarningsAsErrors = true
- EnableNETAnalyzers = true

**Formatting (from .editorconfig):**
- 4 spaces for C# indentation
- CRLF line endings
- Opening braces on new lines (`csharp_new_line_before_open_brace = all`)
- Spaces around binary operators
- No space after cast: `(int)value`

**C# Style Preferences:**
- Use `var` only when type is apparent: `var user = GetUser();`
- Prefer explicit types for built-ins: `int count = 0;` not `var count = 0;`
- Expression-bodied properties preferred: `public string Name => _name;`
- Pattern matching over is/as: `if (obj is User user)`
- Null conditional operators: `user?.Name` over null checks

**Naming Conventions:**
- PascalCase for public members, types, constants
- No prefixes for interfaces, fields, or private members
- Method names should be descriptive: `ValidateEmailAddress()` not `Validate()`

## Testing Patterns

**Test Framework:**
- xUnit for test framework
- FluentAssertions for readable assertions
- Coverlet for code coverage

**Test Structure:**
```csharp
[Fact]
public void Method_Scenario_ExpectedResult()
{
    // Arrange
    var input = CreateTestData();
    
    // Act  
    var result = systemUnderTest.Method(input);
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Should().NotBeNull();
}
```

## Best Practices

**DO ✅ - Result Pattern Usage:**
```csharp
// Use Result for operations that can fail
public Result<User> GetUser(int id)
{
    var user = _repository.FindById(id);
    return user != null 
        ? Result<User>.Succeed(user)
        : Result<User>.FailNotFound($"User {id} not found");
}

// Chain operations using railway-oriented programming  
public Result<Order> ProcessOrder(OrderDto dto)
{
    return ValidateOrder(dto)
        .Then(CreateOrder)
        .Then(CalculateTotals)
        .Then(SaveOrder);
}

// Provide specific error information
public Result ValidateEmail(string email)
{
    if (string.IsNullOrEmpty(email))
        return Result.FailValidation(("email", "Email is required"));
    
    if (!email.Contains("@"))
        return Result.FailValidation(("email", "Invalid email format"));
    
    return Result.Succeed();
}

// Use CollectionResult for paginated data
public CollectionResult<Product> GetProducts(int page, int pageSize)
{
    var products = _repository.GetPaged(page, pageSize);
    var total = _repository.Count();
    return CollectionResult<Product>.Succeed(products, page, pageSize, total);
}
```

**DON'T ❌ - Anti-patterns:**
```csharp
// DON'T: Throw exceptions from Result-returning methods
public Result<User> GetUser(int id)
{
    if (id <= 0)
        return Result<User>.FailValidation(("id", "ID must be positive")); // ✅
    // NOT: throw new ArgumentException("Invalid ID"); // ❌
}

// DON'T: Ignore Result values
var result = UpdateUser(user);
if (result.IsFailed)
    return result; // ✅ Handle the failure

// DON'T: Mix Result and exceptions  
// DON'T: Create generic error messages - be specific
return Result.Fail("User creation failed", "Email already exists"); // ✅
```

**Performance Guidelines:**
1. `Result` and `Result<T>` are structs - avoid boxing
2. Use railway-oriented programming to avoid intermediate variables
3. Cache common Problem instances for frequent errors
4. Use `ConfigureAwait(false)` in library code

## Framework Integration

**ASP.NET Core:**
- Controllers can return Result types directly
- Automatic HTTP status code mapping from Problem Details
- Built-in filters for Result handling

**Orleans:**
- Use `UseOrleansCommunication()` for automatic serialization
- Result types work across grain boundaries
- Problem Details preserved in distributed calls

**Command Pattern:**
- Built-in command infrastructure with idempotency
- Commands implement `ICommand` or `ICommand<T>`
- Automatic validation and result wrapping

## Common Patterns

**Validation:**
```csharp
private Result ValidateDto(CreateUserDto dto)
{
    var errors = new List<(string field, string message)>();
    
    if (string.IsNullOrWhiteSpace(dto.Email))
        errors.Add(("email", "Email is required"));
    
    return errors.Any() 
        ? Result.FailValidation(errors.ToArray())
        : Result.Succeed();
}
```

**Error Recovery:**
```csharp
public async Task<Result<User>> GetUserWithFallback(int id)
{
    return await GetUser(id)
        .CompensateAsync(async error => 
        {
            var archived = await GetArchivedUser(id);
            return archived ?? Result<User>.FailNotFound($"User {id} not found");
        });
}
```

**Aggregating Results:**
```csharp
public Result<Order> CreateOrder(List<OrderItem> items)
{
    var validationResults = items.Select(ValidateItem);
    var combinedResult = Result.Combine(validationResults);
    
    return combinedResult.IsSuccess 
        ? ProcessOrder(items)
        : Result<Order>.Fail(combinedResult.Problem);
}
```