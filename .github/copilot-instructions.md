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