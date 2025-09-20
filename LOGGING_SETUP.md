# Communication Library Logging Setup

## Overview
The Communication library now uses a static logger that integrates with your DI container for proper logging configuration.

## Setup in ASP.NET Core

### Option 1: Automatic DI Integration (Recommended)
```csharp
var builder = WebApplication.CreateBuilder(args);

// Add your logging configuration
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Register other services
builder.Services.AddControllers();

// Configure Communication library - this should be called AFTER all other services
builder.Services.ConfigureCommunication();

var app = builder.Build();
```

### Option 2: Manual Logger Factory
```csharp
var builder = WebApplication.CreateBuilder(args);

// Create logger factory manually
using var loggerFactory = LoggerFactory.Create(builder => 
{
    builder.AddConsole()
           .AddDebug()
           .SetMinimumLevel(LogLevel.Information);
});

// Configure Communication library with specific logger factory
builder.Services.ConfigureCommunication(loggerFactory);
```

## Setup in Console Applications

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ManagedCode.Communication.Extensions;

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

## Manual Configuration (Not Recommended)

If you're not using DI, you can configure the logger manually:

```csharp
using ManagedCode.Communication.Logging;

// Configure with logger factory
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
CommunicationLogger.Configure(loggerFactory);

// Or configure with service provider
CommunicationLogger.Configure(serviceProvider);
```

## What Gets Logged

The Communication library logs errors in the following scenarios:
- Exceptions in `From` methods of Result classes
- Failed operations with detailed context (file, line number, method name)

Example log output:
```
[Error] Error "Connection timeout" in MyService.cs at line 42 in GetUserData
```

## Default Behavior

If no configuration is provided, the library will create a minimal logger factory with Warning level logging to avoid throwing exceptions.