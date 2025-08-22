# ManagedCode.Communication

A high-performance .NET 9.0 library providing Result types and railway-oriented programming patterns for robust error handling in distributed applications.

## Project Requirements

This project requires **.NET 9.0 SDK**. Install it using:

```bash
dotnet --list-sdks  # Check if 9.0.x is available
```

If not available, install .NET 9.0:
```bash
wget -q https://dot.net/v1/dotnet-install.sh && chmod +x dotnet-install.sh
./dotnet-install.sh --version 9.0.100 --install-dir ~/.dotnet
export PATH="~/.dotnet:$PATH"
```

## Build and Test

**Restore packages:**
```bash
dotnet restore
```

**Build all projects:**
```bash
dotnet build --configuration Release
```

**Run tests:**
```bash
dotnet test --configuration Release --no-build
```

## Project Structure

- **ManagedCode.Communication** - Core Result types and railway-oriented programming extensions
- **ManagedCode.Communication.AspNetCore** - ASP.NET Core integration, filters, and middleware
- **ManagedCode.Communication.Orleans** - Microsoft Orleans serialization support
- **ManagedCode.Communication.Tests** - Test suite
- **ManagedCode.Communication.Benchmark** - Performance benchmarks

## Key Concepts

This library implements railway-oriented programming patterns using Result types for error handling without exceptions:

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