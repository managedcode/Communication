# ManagedCode.Communication

A high-performance .NET 9.0 library providing Result types and railway-oriented programming patterns for robust error handling in distributed applications.

**ALWAYS follow these instructions first and only fall back to search or bash commands when you encounter unexpected information that does not match the instructions below.**

## Working Effectively

### Bootstrap and Build the Repository

**NEVER CANCEL builds or tests** - they complete quickly but timeouts should be generous.

1. **Install .NET 9.0 SDK (REQUIRED):**
   ```bash
   wget -q https://dot.net/v1/dotnet-install.sh && chmod +x dotnet-install.sh
   ./dotnet-install.sh --version 9.0.100 --install-dir ~/.dotnet
   export PATH="/home/runner/.dotnet:$PATH"
   ```
   - Verification: `dotnet --version` should show `9.0.100`
   - **CRITICAL**: The project targets .NET 9.0 and will NOT build with older SDKs

2. **Restore packages (takes ~7 seconds):**
   ```bash
   dotnet restore ManagedCode.Communication/ManagedCode.Communication.csproj
   dotnet restore ManagedCode.Communication.AspNetCore/ManagedCode.Communication.AspNetCore.csproj  
   dotnet restore ManagedCode.Communication.Orleans/ManagedCode.Communication.Orleans.csproj
   dotnet restore ManagedCode.Communication.Tests/ManagedCode.Communication.Tests.csproj
   dotnet restore ManagedCode.Communication.Benchmark/ManagedCode.Communication.Benchmark.csproj
   ```
   - **NEVER CANCEL**: Set timeout to 300+ seconds. Build may take up to 5 minutes on slow connections.

3. **Build all projects (takes ~12 seconds):**
   ```bash
   dotnet build ManagedCode.Communication/ManagedCode.Communication.csproj --configuration Release
   dotnet build ManagedCode.Communication.AspNetCore/ManagedCode.Communication.AspNetCore.csproj --configuration Release
   dotnet build ManagedCode.Communication.Orleans/ManagedCode.Communication.Orleans.csproj --configuration Release  
   dotnet build ManagedCode.Communication.Tests/ManagedCode.Communication.Tests.csproj --configuration Release
   dotnet build ManagedCode.Communication.Benchmark/ManagedCode.Communication.Benchmark.csproj --configuration Release
   ```
   - **NEVER CANCEL**: Set timeout to 600+ seconds. Full build may take up to 10 minutes.

### Testing

**Run all tests (takes ~5 seconds, 638 tests):**
```bash
dotnet test ManagedCode.Communication.Tests/ManagedCode.Communication.Tests.csproj --configuration Release --no-build --verbosity normal
```
- **NEVER CANCEL**: Set timeout to 300+ seconds. Tests may take up to 5 minutes in CI environments.
- **Expected result**: All 638 tests pass with 70%+ code coverage
- Tests include ASP.NET Core integration tests with real HTTP requests and SignalR hubs

### Code Quality and Formatting

**Format code (IMPORTANT - there are known line ending issues):**
```bash
# Format individual projects (the repository has CRLF line ending issues on Linux)
dotnet format ManagedCode.Communication/ManagedCode.Communication.csproj --verbosity minimal
dotnet format ManagedCode.Communication.AspNetCore/ManagedCode.Communication.AspNetCore.csproj --verbosity minimal
dotnet format ManagedCode.Communication.Orleans/ManagedCode.Communication.Orleans.csproj --verbosity minimal
dotnet format ManagedCode.Communication.Tests/ManagedCode.Communication.Tests.csproj --verbosity minimal
```
- **NOTE**: The repository uses CRLF line endings (.editorconfig sets `end_of_line = crlf`) which may cause formatting errors on Linux. This is expected behavior.
- **DO NOT** try to fix line ending issues - they are intentional for Windows compatibility

## Validation Scenarios

**ALWAYS test actual functionality after making changes** by running through these complete scenarios:

### 1. Basic Library Functionality Test
Create a test console app to verify core Result functionality:

```bash
cd /tmp
dotnet new console -n TestLibrary
cd TestLibrary
dotnet add reference /home/runner/work/Communication/Communication/ManagedCode.Communication/ManagedCode.Communication.csproj
```

Test Program.cs content:
```csharp
using ManagedCode.Communication;
using ManagedCode.Communication.Extensions;
using System;

Console.WriteLine("Testing ManagedCode.Communication Library");

// Test basic Result creation
var successResult = Result.Succeed();
var failureResult = Result.Fail("Something went wrong");
Console.WriteLine($"Success: {successResult.IsSuccess}, Failure: {failureResult.IsSuccess}");

// Test Result<T> with values  
var userResult = Result<string>.Succeed("John Doe");
var notFoundResult = Result<string>.FailNotFound("User not found");
Console.WriteLine($"User: {userResult.Value}, NotFound: {notFoundResult.Problem?.Title}");

// Test railway-oriented programming chain
var chainResult = ValidateEmail("test@example.com")
    .Then(email => Result<string>.Succeed(email.ToLower()))
    .Then(email => Result<string>.Succeed($"Processed: {email}"));
Console.WriteLine($"Chain result: {chainResult.IsSuccess}, Value: {chainResult.Value}");

static Result<string> ValidateEmail(string email) => 
    email.Contains("@") ? Result<string>.Succeed(email) : Result<string>.FailValidation(("email", "Invalid format"));
```

Run: `dotnet run`
**Expected output**: Success messages showing Result types work correctly with railway-oriented programming.

### 2. ASP.NET Core Integration Test  
The test suite includes real ASP.NET Core integration tests that:
- Start an actual web server on localhost
- Test Result to HTTP status code mapping (200, 400, 403, 404, 500)
- Test SignalR hub integration with Result types
- Test authentication and authorization flows

Run the integration tests to verify web functionality:
```bash
dotnet test ManagedCode.Communication.Tests/ManagedCode.Communication.Tests.csproj --filter "AspNetCore" --verbosity normal
```

### 3. Performance Benchmark Test
Test the benchmark suite (interactive):
```bash
dotnet run -c Release --project ManagedCode.Communication.Benchmark/ManagedCode.Communication.Benchmark.csproj -- --job dry
# When prompted, enter "*" to run all benchmarks or "0" for specific benchmark
```
**Expected**: Benchmark starts and shows performance comparisons between Result types and alternatives.

## Key Projects in the Codebase

### Core Projects
- **ManagedCode.Communication** - Core Result types, railway-oriented programming extensions, command pattern
- **ManagedCode.Communication.AspNetCore** - ASP.NET Core filters, middleware, and Result-to-HTTP mapping  
- **ManagedCode.Communication.Orleans** - Microsoft Orleans serialization support for distributed Result types
- **ManagedCode.Communication.Tests** - Comprehensive test suite (638 tests)
- **ManagedCode.Communication.Benchmark** - BenchmarkDotNet performance testing

### Key Source Locations
- **Result Types**: `ManagedCode.Communication/Result/`, `ManagedCode.Communication/ResultT/`
- **Railway Extensions**: `ManagedCode.Communication/Extensions/RailwayExtensions*.cs`
- **Command Pattern**: `ManagedCode.Communication/Commands/`
- **ASP.NET Core Filters**: `ManagedCode.Communication.AspNetCore/Filters/`
- **Orleans Serializers**: `ManagedCode.Communication.Orleans/Serializers/`

## Build Timing Expectations

**All times measured on standard GitHub Actions runners:**

| Operation | Expected Time | Timeout Setting | 
|-----------|---------------|-----------------|
| .NET 9 SDK Install | 30-60 seconds | 300 seconds |
| Package Restore | 5-10 seconds | 300 seconds |
| Full Build | 10-15 seconds | 600 seconds |
| Test Execution | 4-6 seconds | 300 seconds |
| Format Check | 3-8 seconds | 300 seconds |
| Single Project Build | 1-4 seconds | 300 seconds |

**CRITICAL: NEVER CANCEL** any of these operations. Always wait for completion.

## Common Development Tasks

### Adding New Result Types
- Extend base classes in `ManagedCode.Communication/Result/` or `ManagedCode.Communication/ResultT/`
- Add corresponding tests in `ManagedCode.Communication.Tests/`
- Update ASP.NET Core mappings in `ManagedCode.Communication.AspNetCore/Extensions/ResultExtensions.cs`

### Adding ASP.NET Core Features  
- Create filters in `ManagedCode.Communication.AspNetCore/Filters/`
- Add extension methods in `ManagedCode.Communication.AspNetCore/Extensions/`
- Test with real HTTP scenarios in test project's TestApp

### Performance Testing
- Add benchmarks to `ManagedCode.Communication.Benchmark/`
- Use BenchmarkDotNet attributes for proper measurement
- Always compare against existing baseline implementations

## Troubleshooting

### Build Issues
- **".NET 9.0 not found"**: Install .NET 9.0 SDK using the exact commands above
- **"Project not found"**: Use individual project files, not solution file (`.slnx` format not fully supported)
- **"Format errors"**: Line ending issues are expected on Linux due to CRLF settings

### Test Issues  
- **Tests timeout**: ASP.NET Core integration tests start real servers and may take longer in CI environments
- **Orleans tests fail**: Ensure all projects are built before running tests (Orleans needs compiled assemblies)

### Expected CI Workflow
The `.github/workflows/ci.yml` runs:
1. .NET 9.0 setup
2. `dotnet restore` 
3. `dotnet build --configuration Release --no-restore`
4. `dotnet test --configuration Release --no-build`
5. Code coverage upload

**Always run the same sequence locally** to ensure CI compatibility.

## Library Usage Examples

The library provides Result types for error handling without exceptions:

```csharp
// Basic usage
Result<User> user = await GetUserAsync(id);
if (user.IsSuccess)
    Console.WriteLine($"Found: {user.Value.Name}");
else
    Console.WriteLine($"Error: {user.Problem.Title}");

// Railway-oriented programming
var result = await ValidateUserAsync(userData)
    .ThenAsync(user => SaveUserAsync(user))
    .ThenAsync(user => SendWelcomeEmailAsync(user))
    .ThenAsync(user => LogUserCreationAsync(user));

// ASP.NET Core integration (automatic HTTP status mapping)
[HttpPost]
public Result<User> CreateUser(CreateUserRequest request) =>
    ValidateRequest(request)
        .Then(CreateUserFromRequest)
        .Then(SaveUserToDatabase);
```

Always test these patterns when making changes to ensure the library's core value proposition remains intact.