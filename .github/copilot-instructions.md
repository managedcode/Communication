# ManagedCode.Communication

Result pattern for .NET that replaces exceptions with type-safe return values. Features railway-oriented programming, ASP.NET Core integration, RFC 7807 Problem Details, and built-in pagination.

## General

* Always prefer Result types over throwing exceptions for expected error cases.
* Use railway-oriented programming patterns with `Map`, `Then`, `Compensate` for chaining operations.
* Always use the latest C# features, currently C# 13.
* Make only high confidence suggestions when reviewing code changes.
* Never change `Directory.Build.props`, or solution files unless explicitly asked.

## .NET Environment

* This project targets .NET 9.0 and uses C# 13.0.
* The project uses nullable reference types and treats warnings as errors.

## Building and Testing

```bash
dotnet build
dotnet test
```

## Formatting

* Apply code-formatting style defined in `.editorconfig`.
* Prefer file-scoped namespace declarations and single-line using directives.
* Insert a newline before the opening curly brace of any code block.
* Use pattern matching and switch expressions wherever possible.
* Use `nameof` instead of string literals when referring to member names.

### Nullable Reference Types

* Declare variables non-nullable, and check for `null` at entry points.
* Always use `is null` or `is not null` instead of `== null` or `!= null`.
* Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.

## Testing

* We use xUnit for tests with FluentAssertions for assertions.
* Do not emit "Arrange", "Act" or "Assert" comments.
* Use the naming pattern: `Method_Scenario_ExpectedResult()`.
* Copy existing style in nearby test files for method names and capitalization.

```csharp
[Fact]
public void Succeed_ShouldCreateSuccessfulResult()
{
    var result = Result.Succeed();
    
    result.IsSuccess.Should().BeTrue();
    result.IsFailed.Should().BeFalse();
    result.Problem.Should().BeNull();
}
```

## Core Result Types

This library implements the Result pattern for functional error handling:

* `Result` - Success/failure without a value
* `Result<T>` - Success with value T or failure  
* `CollectionResult<T>` - Collections with built-in pagination
* `Problem` - RFC 7807 compliant error details

## Railway-Oriented Programming

Chain operations using functional combinators:

```csharp
// Basic Result usage
var success = Result.Succeed();
var failure = Result.Fail("Error message");

// Railway-oriented programming
return ValidateEmail(email)
    .Then(ProcessEmail)
    .Then(SaveToDatabase);
```

## Best Practices

**DO ✅ Use Result for operations that can fail:**
```csharp
public Result<User> GetUser(int id)
{
    var user = _repository.FindById(id);
    return user is not null 
        ? Result<User>.Succeed(user)
        : Result<User>.FailNotFound($"User {id} not found");
}
```

**DO ✅ Provide specific error information:**
```csharp
public Result ValidateEmail(string email)
{
    if (string.IsNullOrEmpty(email))
        return Result.FailValidation(("email", "Email is required"));
    
    if (!email.Contains('@'))
        return Result.FailValidation(("email", "Invalid email format"));
    
    return Result.Succeed();
}
```

**DON'T ❌ Throw exceptions from Result-returning methods:**
```csharp
public Result<User> GetUser(int id)
{
    if (id <= 0)
        return Result<User>.FailValidation(("id", "ID must be positive")); // ✅
    // NOT: throw new ArgumentException("Invalid ID"); // ❌
}
```

**DON'T ❌ Ignore Result values:**
```csharp
var result = UpdateUser(user);
if (result.IsFailed)
    return result; // ✅ Handle the failure
```

## Framework Integration

**ASP.NET Core:**
* Controllers can return Result types directly
* Automatic HTTP status code mapping from Problem Details
* Built-in filters for Result handling

**Orleans:**
* Use `UseOrleansCommunication()` for automatic serialization
* Result types work across grain boundaries

**Performance:**
* `Result` and `Result<T>` are structs - avoid boxing
* Use railway-oriented programming to avoid intermediate variables
* Cache common Problem instances for frequent errors