# ManagedCode.Communication

**Make failure part of the signature.** A method returns `Result<T>` — either it worked, or it carries a
`Problem` (RFC 7807) explaining why not. No invisible second return path, one error shape from the domain layer
to the HTTP response, and railway operators to chain it all without a pyramid of `try`/`catch`.

And when a command is too slow to answer in one call, [**CQRS Streaming**](#cqrs-streaming) reports its progress
as a typed stream that is guaranteed to tell you how it ended.

Built for .NET 10, with ASP.NET Core, SignalR and Orleans integration in the box.

[![NuGet](https://img.shields.io/nuget/v/ManagedCode.Communication.svg)](https://www.nuget.org/packages/ManagedCode.Communication/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)

## At a glance

**Failure is a value, not a second return path.** Every method says up front that it can fail, and every failure
is the same shape.

```csharp
public async Task<Result<Order>> PlaceOrderAsync(Cart cart)
{
    if (cart.IsEmpty)
        return Result<Order>.FailValidation(("cart", "is empty"));

    var payment = await ChargeAsync(cart.Total);
    if (payment.IsFailed)
        return Result<Order>.Fail(payment.Problem!);      // pass the failure along untouched

    return Result<Order>.Succeed(await CreateOrderAsync(cart));
}
```

**Railway: chain the happy path, short-circuit on the first failure.** Every operator runs only on success and
passes a failure straight through — no `try`/`catch`, no null checks between the steps. Async or not, the chain
never has to break for an `await`.

```csharp
var receipt = await LoadCartAsync(cartId)
    .EnsureAsync(cart => !cart.IsEmpty, Problem.Validation(("cart", "is empty")))
    .BindAsync(cart => ChargeAsync(cart.Total))
    .Map(payment => payment.Receipt)
    .TapAsync(receipt => logger.Issued(receipt))
    .CompensateAsync(problem => RetryOnce(problem));
```

**A slow command becomes a typed stream.** Progress and result are both typed, and the stream is guaranteed to
tell you how it ended — see [CQRS Streaming](#cqrs-streaming).

```csharp
// server
app.MapGet("/import", (CancellationToken ct) =>
        CqrsStream.Create<ImportProgress, ImportReport>(async writer =>
        {
            for (var i = 1; i <= 10; i++)
                await writer.ProgressAsync(new ImportProgress(i * 10));

            return Result<ImportReport>.Succeed(new ImportReport(10));
        }, ct))
    .WithCommunicationCqrsResults();

// client — Server-Sent Events on the wire; progress arrives via the callback,
// the answer comes back from the method, and nothing blocks
Result<ImportReport> report = await http
    .GetForCqrsStreamAsync<ImportProgress, ImportReport>("/import")
    .ToResultAsync(progress => Console.WriteLine($"{progress.Percent}%"));
```

A stream that breaks, or ends without saying how it went, comes back as an ordinary failed `Result` — so it
joins the railway like anything else:

```csharp
var imported = await http
    .GetForCqrsStreamAsync<ImportProgress, ImportReport>("/import")
    .ToResultAsync(progress => logger.Progress(progress.Percent))
    .Map(report => report.Imported)
    .CompensateAsync(problem => Result<int>.Succeed(0));
```

**And it maps itself to HTTP.** Return a `Result<T>` from an action and the filter turns it into a `200` or an
RFC 7807 problem response — no plumbing in the controller.

```csharp
[HttpGet("{id}")]
public Task<Result<Order>> Get(string id) => _orders.FindAsync(id);
```


## Table of Contents

- [At a glance](#at-a-glance)
- [Overview](#overview)
- [Key Features](#key-features)
- [Installation](#installation)
- [Logging Configuration](#logging-configuration)
- [Core Concepts](#core-concepts)
- [Quick Start](#quick-start)
- [CQRS Streaming](#cqrs-streaming)
- [Railway-Oriented Programming](#railway-oriented-programming)
- [Command Pattern and Idempotency](#command-pattern-and-idempotency)
  - [Command Correlation and Tracing Identifiers](#command-correlation-and-tracing-identifiers)
  - [Idempotency Architecture Overview](#idempotency-architecture-overview)
- [Error Handling Patterns](#error-handling-patterns)
- [Integration Guides](#integration-guides)
- [Performance](#performance)
- [Registration Reference](#registration-reference)
- [Observability](#observability)
- [Behaviour Notes](#behaviour-notes)
- [Comparison](#comparison)
- [Best Practices](#best-practices)
- [Examples](#examples)
- [API Reference](#api-reference)

## Overview

ManagedCode.Communication models the outcome of an operation as a value. Instead of throwing, a method returns
`Result` or `Result<T>`: either it succeeded, or it carries a `Problem` describing why it did not. The failure
becomes part of the signature rather than something a caller discovers at runtime.

### Why a Result type?

An exception is an invisible second return path. Nothing in `Task<Order> PlaceOrderAsync(Cart cart)` tells you it
can fail with `PaymentDeclinedException`, so callers guard against what they happen to remember. `Task<Result<Order>>`
says it up front, and the compiler carries that fact everywhere the value goes.

What you get:

- **Failures are in the signature.** A reviewer sees which calls can fail without reading their implementations.
- **One error shape end to end.** Every failure is a `Problem` (RFC 7807), so a domain rule, a validation error and
  a crashed dependency all serialize the same way and map to the same HTTP response.
- **Composition instead of nesting.** Railway operators chain the happy path and short-circuit on the first
  failure, replacing pyramids of `try`/`catch`.
- **Cheap success.** `Result` and `Result<T>` are structs; a successful call allocates nothing. Exceptions are only
  costly when thrown — the point here is the clarity, and not paying stack-capture cost for *expected* failures
  like "not found" or "invalid input".
- **Straightforward tests.** Assert on a returned value rather than on which exception escaped.

What it does **not** give you: the compiler will not force you to inspect a `Result`. C# has no enforcement for
that, and this library ships no analyzer. Ignoring a returned `Result` compiles cleanly — treat that the way you
treat any ignored return value.

Exceptions are still the right tool for genuinely exceptional, unrecoverable conditions. Results are for the
failures your callers are expected to handle.

## Key Features

### 🎯 Core Result Types

- **`Result`**: Represents success/failure without a value
- **`Result<T>`**: Represents success with value `T` or failure
- **`CollectionResult<T>`**: Represents collections with built-in pagination
- **`Problem`**: RFC 7807 compliant error details

### 📡 CQRS Streaming

- A long-running command is an `IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>>` — typed progress, typed
  answer, one terminal chunk guaranteed.
- Travels over Server-Sent Events out of the box, so any client can read it without a client library.
- A handler that throws, or ends early, still produces a terminal `Failed` chunk instead of a dead connection.
- The same contract over SignalR, Orleans or gRPC via `CqrsStream.Normalize`.
- `ToResultAsync(onProgress)` drains a stream to its answer, so callers never write the loop — and the result
  feeds straight into the railway.
- See [CQRS Streaming](#cqrs-streaming).

### ⚙️ Static Factory Abstractions

- Leverage C# static interface members to centralize factory overloads for every result, command, and collection type.
- `IResultFactory<T>` and `ICommandFactory<T>` deliver a consistent surface while bridge helpers remove repetitive boilerplate.
- Extending the library now only requires implementing the minimal `Succeed`/`Fail` contract—the shared helpers provide the rest.

### 🧭 Pagination Utilities

- `PaginationRequest` encapsulates skip/take semantics, built-in normalization, and clamping helpers.
- `PaginationOptions` lets you define default, minimum, and maximum page sizes for a bounded API surface.
- `PaginationCommand` captures pagination intent as a first-class command with generated overloads for skip/take, page numbers, and enum command types.
- `CollectionResult<T>.Succeed(..., PaginationRequest request, int totalItems)` keeps result metadata aligned with pagination commands.

### 🚂 Railway-Oriented Programming

Complete set of functional combinators for composing operations:

- `Map`: Transform success values
- `Bind` / `Then`: Chain Result-returning operations
- `Tap` / `Do`: Execute side effects
- `Match`: Pattern matching on success/failure
- `Compensate`: Recovery from failures
- `Merge` / `Combine`: Aggregate multiple results

### 🌐 Framework Integration

- **ASP.NET Core**: Automatic HTTP response mapping
- **SignalR**: Hub filters for real-time error handling
- **Microsoft Orleans**: Grain call filters and surrogates
- **Command Pattern**: Built-in command infrastructure with idempotency

### 🔍 Observability Built In

- Source-generated `LoggerCenter` APIs provide zero-allocation logging across ASP.NET Core filters, SignalR hubs, and command stores.
- Call sites automatically check log levels, so you only pay for the logs you emit.
- Extend logging with additional `[LoggerMessage]` partials to keep high-volume paths allocation free.

### 🛡️ Error Types

Pre-defined error categories with appropriate HTTP status codes:

- Validation errors (400 Bad Request)
- Not Found (404)
- Unauthorized (401)
- Forbidden (403)
- Internal Server Error (500)
- Custom enum-based errors

## Installation

### Which package do I need?

| Package | Contains | Depends on ASP.NET Core? |
| --- | --- | --- |
| `ManagedCode.Communication` | `Result`, `Result<T>`, `CollectionResult<T>`, `Problem`, commands and idempotency, and the CQRS streaming contract (`CqrsStreamChunk<,>`, `CqrsStream`, the SSE reader) | no |
| `ManagedCode.Communication.Extensions` | Railway composition (`Bind`, `Map`, `Tap`, `Then`, `Ensure`, `Match`, `Compensate`…) and `HttpClient` → `Result` helpers with optional Polly | no |
| `ManagedCode.Communication.AspNetCore` | MVC filters, Minimal API filter, SignalR, DI wiring, and the CQRS Server-Sent Events transport | yes |
| `ManagedCode.Communication.Orleans` | Orleans grain filters and serialization | no (Orleans) |

The two "no" packages work anywhere .NET runs — console, worker, Blazor WebAssembly, MAUI. The CQRS streaming
contract and its client live in the base package on purpose: both ends of a stream need the same chunk type, and
it costs nothing to carry (`System.Net.ServerSentEvents`, `System.Net.Http.Json` and `System.Threading.Channels`
all ship in the .NET runtime, so the base package still has no extra NuGet dependency).

`ManagedCode.Communication.AspNetCore` pulls in `.Extensions` transitively, so a web application only needs that
one reference.


### Package Manager Console

```powershell
# Core library
Install-Package ManagedCode.Communication

# ASP.NET Core integration
Install-Package ManagedCode.Communication.AspNetCore

# Minimal API extensions
Install-Package ManagedCode.Communication.Extensions

# Orleans integration
Install-Package ManagedCode.Communication.Orleans
```

### .NET CLI

```bash
# Core library
dotnet add package ManagedCode.Communication

# ASP.NET Core integration
dotnet add package ManagedCode.Communication.AspNetCore

# Minimal API extensions
dotnet add package ManagedCode.Communication.Extensions

# Orleans integration
dotnet add package ManagedCode.Communication.Orleans
```

### PackageReference

```xml
<PackageReference Include="ManagedCode.Communication" Version="10.1.0" />
<PackageReference Include="ManagedCode.Communication.AspNetCore" Version="10.1.0" />
<PackageReference Include="ManagedCode.Communication.Extensions" Version="10.1.0" />
<PackageReference Include="ManagedCode.Communication.Orleans" Version="10.1.0" />
```

## Logging Configuration

The library includes integrated logging for error scenarios. Configure logging to capture detailed error information:

### ASP.NET Core Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add your logging configuration
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Register other services
builder.Services.AddControllers();

// Configure Communication library - this enables automatic error logging
builder.Services.ConfigureCommunication();

var app = builder.Build();
```

### Minimal API Result Mapping

Add the optional `ManagedCode.Communication.Extensions` package to bridge Minimal API endpoints with the Result pattern. The
package provides the `ResultEndpointFilter` and a fluent helper `WithCommunicationResults` that wraps the endpoint builder and
returns `IResult` instances automatically:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureCommunication();

var app = builder.Build();

// Apply the filter to a single endpoint
app.MapGet("/orders/{id}", async (Guid id, IOrderService orders) =>
        await orders.GetAsync(id))
   .WithCommunicationResults();

// Or apply it to a group so every route inherits the conversion
app.MapGroup("/orders")
   .WithCommunicationResults()
   .MapPost(string.Empty, async (CreateOrder command, IOrderService orders) =>
        await orders.CreateAsync(command));

app.Run();
```

Handlers can return any `Result` or `Result<T>` instance and the filter will reuse the existing ASP.NET Core converters so
you do not need to write manual `IResult` translations.

### Resilient HTTP Clients

The extensions package also ships helpers that turn `HttpClient` calls directly into `Result` instances and optionally run
them through Polly resilience pipelines:

```csharp
using ManagedCode.Communication.Extensions.Http;
using Polly;
using Polly.Retry;

var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
    .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromMilliseconds(200),
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .HandleResult(response => !response.IsSuccessStatusCode)
    })
    .Build();

var result = await httpClient.SendForResultAsync<OrderDto>(
    () => new HttpRequestMessage(HttpMethod.Get, $"/orders/{orderId}"),
    pipeline);

if (result.IsSuccess)
{
    // access result.Value without manually reading the HTTP payload
}
```

The helpers use the existing `HttpResponseMessage` converters, so non-success status codes automatically map to a
`Problem` with the response body and status code.
success responses map to `200 OK`/`204 No Content` while failures become RFC 7807 problem details. Native `Microsoft.AspNetCore.Http.IResult`
responses pass through unchanged, so you can mix and match traditional Minimal API patterns with ManagedCode.Communication results.

### Console Application Setup

```csharp
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

The library automatically logs errors in Result factory methods (`From`, `Try`, etc.) with detailed context including file names, line numbers, and method names for easier debugging.

## Core Concepts

### Result Type

The `Result` type represents an operation that can either succeed or fail:

```csharp
public struct Result
{
    public bool IsSuccess { get; }
    public Problem? Problem { get; }
}
```

### Result Type with Value

The generic `Result<T>` includes a value on success:

```csharp
public struct Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Problem? Problem { get; }
}
```

### Problem Type

Implements RFC 7807 Problem Details for HTTP APIs:

```csharp
public class Problem
{
    public string Type { get; set; }
    public string Title { get; set; }
    public int StatusCode { get; set; }
    public string Detail { get; set; }
    public Dictionary<string, object> Extensions { get; set; }
}
```

### Display Message Helpers

Use built-in helpers to convert technical `Problem` payloads into UI-friendly messages:

```csharp
var problem = Problem.Create("RegistrationUnavailable", "Service is temporarily unavailable", 503);
problem.ErrorCode = "RegistrationUnavailable";

// Default message resolution chain:
// ErrorCode mapper -> Detail -> Title -> defaultMessage -> "An error occurred"
var message = problem.ToDisplayMessage(defaultMessage: "Please try again later");

var registrationMessages = new Dictionary<string, string>
{
    ["RegistrationUnavailable"] = "Registration is currently unavailable.",
    ["RegistrationBlocked"] = "Registration is temporarily blocked.",
    ["RegistrationInviteRequired"] = "Registration requires an invitation code."
};

// 1) Dictionary overload
var byDictionary = problem.ToDisplayMessage(
    registrationMessages,
    defaultMessage: "Please try again later");

// 2) Tuple mappings overload
var byTuples = problem.ToDisplayMessage(
    "Please try again later",
    ("RegistrationUnavailable", "Registration is currently unavailable."),
    ("RegistrationBlocked", "Registration is temporarily blocked."),
    ("RegistrationInviteRequired", "Registration requires an invitation code."));

// 3) Delegate overload
static string? ResolveRegistrationMessage(string code) => code switch
{
    "RegistrationUnavailable" => "Registration is currently unavailable.",
    "RegistrationBlocked" => "Registration is temporarily blocked.",
    "RegistrationInviteRequired" => "Registration requires an invitation code.",
    _ => null
};

var byDelegate = problem.ToDisplayMessage(
    ResolveRegistrationMessage,
    defaultMessage: "Please try again later");

// The same overloads are available for Result, Result<T> and CollectionResult<T>
var resultMessage = Result.Fail(problem).ToDisplayMessage(
    registrationMessages,
    defaultMessage: "Please try again later");

// Typed extension access
if (problem.TryGetExtension("retryAfter", out int retryAfterSeconds))
{
    Console.WriteLine($"Retry after: {retryAfterSeconds}s");
}
```

## Quick Start

### Basic Usage

```csharp
using ManagedCode.Communication;

// Creating Results
var success = Result.Succeed();
var failure = Result.Fail("Operation failed");

// Results with values
var userResult = Result<User>.Succeed(new User { Id = 1, Name = "John" });
var notFound = Result<User>.FailNotFound("User not found");

// Validation errors
var invalid = Result.FailValidation(
    ("email", "Email is required"),
    ("age", "Age must be positive")
);

// From exceptions
try
{
    // risky operation
}
catch (Exception ex)
{
    var error = Result.Fail(ex);
}
```

### Checking Result State

```csharp
if (result.IsSuccess)
{
    // Handle success
}

if (result.IsFailed)
{
    // Handle failure
}

if (result.IsInvalid)
{
    // Handle validation errors
}

// Pattern matching
result.Match(
    onSuccess: () => Console.WriteLine("Success!"),
    onFailure: problem => Console.WriteLine($"Failed: {problem.Detail}")
);
```

## CQRS Streaming

Some commands do not finish quickly. An import, a report, a bulk migration — the caller needs to know that it
started, roughly where it got to, and how it ended. The usual answers are all unsatisfying: poll a status
endpoint, invent a jobs table, or push raw WebSocket frames and hand-roll the protocol at both ends.

This models the whole thing as one typed stream:

```csharp
IAsyncEnumerable<CqrsStreamChunk<ImportProgress, ImportReport>>
```

Two type parameters: what progress looks like, and what the answer looks like. The stream emits any number of
progress chunks and ends with **exactly one** terminal chunk — completed or failed, never silence.

```mermaid
sequenceDiagram
    autonumber
    participant Client
    participant Transport as SSE transport
    participant Handler

    Client->>Transport: GET /import
    Handler->>Transport: Started
    Transport-->>Client: event: started
    Handler->>Transport: Progress 30%
    Transport-->>Client: event: progress
    Handler->>Transport: Progress 70%
    Transport-->>Client: event: progress
    Handler->>Transport: Completed (ImportReport)
    Transport-->>Client: event: completed

    Note over Transport,Client: If the handler throws, or ends without a terminal chunk,<br/>the transport sends a terminal Failed chunk anyway.
```

Over HTTP it travels as Server-Sent Events, so a browser, `curl` or any non-.NET client can read it with no
client library at all. The same stream works over SignalR, Orleans or gRPC without changing the handler.

### Why not just poll, or use raw WebSockets?

| | What you end up writing |
| --- | --- |
| Polling a status endpoint | A jobs table, a status enum, an expiry policy, and a client loop that is always either too slow or too chatty. |
| Raw WebSockets / SignalR messages | Your own message envelope, your own "it's finished" signal, your own error frame — and both ends have to agree on all three. |
| **This** | A typed stream. The envelope, the terminal guarantee and the error frame are the contract. |

### The chunk contract

A chunk is one of four kinds, and the transport guarantees the shape of the sequence:

```mermaid
flowchart LR
    Open([stream opens]) --> S["Started<br/><small>optional, at most one</small>"]
    S --> P["Progress<br/><small>any number, including none</small>"]
    P -- more work --> P
    P --> T{{"exactly one<br/>terminal chunk"}}
    T --> C["Completed<br/><small>carries the result</small>"]
    T --> F["Failed<br/><small>carries a Problem</small>"]
    C --> Close([stream closes])
    F --> Close

    style C stroke-width:2px
    style F stroke-width:2px
```

- `Started` — optional, announces that execution began.
- `Progress` — optional, any number of in-flight updates.
- `Completed` — terminal success; the payload is in `Final`.
- `Failed` — terminal failure; a `Problem` says what went wrong.

What the transport guarantees on **both** ends:

- **Every stream ends on a terminal chunk.** A handler that returns without one gets a `Failed` chunk carrying
  `CqrsStreamProblems.IncompleteStream` appended, rather than the stream just stopping.
- **An unhandled exception becomes a terminal `Failed` chunk** with a `Problem` built from it, instead of
  tearing down the connection mid-response.
- **Every chunk is numbered.** `Sequence` is filled in when a handler omits it and is written to the SSE `id:`
  field, so consumers can restore ordering and resume with `Last-Event-ID`.
- **`Kind` travels as a string**, so adding enum members never renumbers existing ones across independently
  deployed clients and servers.

An exception thrown *before* the handler returns its stream never produced a stream, so it is not the
transport's to handle — it flows into the host's normal exception handling.

### Writing a handler

`CqrsStream.Create` numbers the chunks, guarantees the terminal chunk, and turns a thrown exception into a
`Failed` chunk:

```csharp
app.MapGet("/import", (CancellationToken cancellationToken) =>
        CqrsStream.Create<ImportProgress, ImportReport>(async writer =>
        {
            await writer.StartedAsync(new ImportProgress(0));

            for (var i = 1; i <= 10; i++)
            {
                await DoWorkAsync(writer.CancellationToken);
                await writer.ProgressAsync(new ImportProgress(i * 10));
            }

            return Result<ImportReport>.Succeed(new ImportReport(10));
        }, cancellationToken))
    .WithCommunicationCqrsResults();
```

Returning a failed `Result<TResult>` reports a business failure; throwing reports an unexpected one. Both arrive
as a terminal `Failed` chunk, so the consumer has a single code path for "it did not work".

You can hand-write the iterator instead when you want full control:

```csharp
static async IAsyncEnumerable<CqrsStreamChunk<ImportProgress, ImportReport>> ImportAsync()
{
    yield return CqrsStreamChunk<ImportProgress, ImportReport>.Started(new ImportProgress(0));
    await Task.Delay(100);
    yield return CqrsStreamChunk<ImportProgress, ImportReport>.Progress(new ImportProgress(50));
    yield return CqrsStreamChunk<ImportProgress, ImportReport>.Completed(new ImportReport(10));
}
```

### Reading a stream

```csharp
await foreach (var chunk in client.GetForCqrsStreamAsync<ImportProgress, ImportReport>("/import"))
{
    if (chunk.TryGetProgress(out var progress))
        Console.WriteLine($"{progress.Percent}%");
    else if (chunk.TryGetResult(out var report))
        Console.WriteLine($"imported {report.Imported}");
    else if (chunk.TryGetProblem(out var problem))
        Console.WriteLine($"failed: {problem.Title} — {problem.Detail}");
}
```

The reader does not throw for transport problems. A non-success status code, a dropped connection and an
undecodable frame all arrive as a terminal `Failed` chunk, so that loop covers every outcome. Only cancellation
propagates, as an `OperationCanceledException`.

### Consuming a stream without writing the loop

`await foreach` is the right tool when you genuinely want to react chunk by chunk. Most callers do not — they
want the answer, and maybe a progress callback on the way. Draining by hand means a loop, a list, a branch per
chunk kind, and a decision about what a stream that simply stops means.

`ToResultAsync` is that loop, written once:

```csharp
Result<ImportReport> report = await grain.StreamAsync().ToResultAsync();
```

The terminal chunk becomes the result. A stream that ends **without** one fails with
`CqrsStreamProblems.IncompleteStream` rather than reporting a success the command never claimed.

**With progress.** Pass a callback and it fires as each update arrives — the answer still comes back from the
method:

```csharp
var report = await client
    .GetForCqrsStreamAsync<ImportProgress, ImportReport>("/import")
    .ToResultAsync(progress => hub.Clients.All.SendAsync("progress", progress.Percent));
```

The callback also comes in an awaited form, which takes the cancellation token as its second parameter:

```csharp
var report = await stream.ToResultAsync(async (progress, token) =>
    await hub.Clients.All.SendAsync("progress", progress.Percent, token));
```

Nothing here blocks, and the awaited callback finishes before the next chunk is pulled — a slow handler applies
back-pressure instead of letting chunks pile up behind it. (The token parameter is also what stops an async
lambda from binding to the `Action<TProgress>` overload and having its task silently dropped.)

**When you want the whole picture.** `ToOutcomeAsync` keeps the result, every progress payload, and every chunk:

```csharp
var outcome = await grain.StreamAsync().ToOutcomeAsync();

outcome.Chunks.Count.ShouldBe(3);
outcome.Progress.Select(p => p.Percent).ShouldBe([30, 70]);
outcome.Value!.Status.ShouldBe("done");
```

An outcome converts to its `Result<TResult>` implicitly, so it can be returned wherever a result is expected.

| | Keeps | Use when |
| --- | --- | --- |
| `ToResultAsync()` | the terminal chunk only | you want the answer |
| `ToResultAsync(onProgress)` | the terminal chunk only | you want the answer and live progress |
| `ToOutcomeAsync()` | result, progress, chunks | tests, audit logs, replaying what happened |
| `ToChunkListAsync()` | every chunk | you will interpret them yourself |
| `chunks.ToStreamResult()` | — | you already have the chunks in hand |

### Streams and the railway

`ToResultAsync` returns `Task<Result<TResult>>`, which is exactly what the async railway operators take. A
stream is therefore just the start of a chain — no `await`, no temporary variable, no `if` in between:

```csharp
var imported = await client
    .GetForCqrsStreamAsync<ImportProgress, ImportReport>("/import")
    .ToResultAsync(progress => logger.Progress(progress.Percent))
    .EnsureAsync(report => report.Imported > 0, Problem.Validation(("import", "produced nothing")))
    .Map(report => report.Imported)
    .TapAsync(count => metrics.Imported(count))
    .CompensateAsync(problem => problem.StatusCode == 409
        ? Result<int>.Succeed(0)          // already imported by someone else — not an error here
        : Result<int>.Fail(problem));
```

Every step runs only on success; a failure anywhere — including the stream breaking, or ending without saying
how it went — skips the rest and arrives at `CompensateAsync` as an ordinary `Problem`.


### Registration

```csharp
builder.Services.AddCommunicationCqrs();                                    // minimal API
builder.Services.AddControllers(o => o.AddCommunicationCqrsFilters());      // MVC controllers
```

Both default to numbering chunks and guaranteeing a terminal chunk. To change that:

```csharp
// server, globally or per endpoint
builder.Services.AddCommunicationCqrs(o => o.EnsureTerminalChunk = false);
app.MapGet("/import", Handler)
   .WithCommunicationCqrsResults(new CqrsStreamServerOptions { EnsureTerminalChunk = false });

// client
var options = new CqrsStreamClientOptions
{
    MalformedChunkBehavior = CqrsMalformedChunkBehavior.Skip  // default: EmitFailedChunk; also: Throw
};
```

Two namespaces cover the feature: `ManagedCode.Communication.CQRS` for the contract, the authoring helper and
the client reader, and `ManagedCode.Communication.AspNetCore.Extensions` for the server transport. The first
lives in the base package, so a console app, a worker or a Blazor WebAssembly client can consume a stream
without referencing ASP.NET Core.

### Other transports

The guarantees come from `CqrsStream.Normalize`, which the SSE transport calls for you. Any other transport gets
the same contract by calling it directly.

```csharp
public class ImportHub : Hub
{
    public IAsyncEnumerable<CqrsStreamChunk<ImportProgress, ImportReport>> Import(CancellationToken token)
        => CqrsStream.Normalize(ImportAsync(token), cancellationToken: token);
}
```

Skipping `Normalize` is what makes the difference: a hub method that throws mid-stream faults the connection and
the client sees a `HubException` instead of a terminal chunk it can inspect. A `CqrsStream.Create` stream already
carries the guarantees and needs no `Normalize`. The client side is plain SignalR, with the same loop as over
HTTP:

```csharp
await foreach (var chunk in hub.StreamAsync<CqrsStreamChunk<ImportProgress, ImportReport>>("Import"))
{
}
```

**Orleans.** `ManagedCode.Communication.Orleans` registers a serialization surrogate for `CqrsStreamChunk<,>`,
so chunks can cross a grain boundary:

```csharp
public interface IImportGrain : IGrainWithStringKey
{
    IAsyncEnumerable<CqrsStreamChunk<ImportProgress, ImportReport>> ImportAsync();
}
```

Your own progress and result payloads still need `[GenerateSerializer]` with `[Id(n)]` members, as with any type
crossing a grain boundary. A missing serializer is a *startup* failure in Orleans, not a runtime one: a silo
whose grain interfaces mention an unserializable type refuses to boot.


## Railway-Oriented Programming

Every operator has a `Task<Result<T>>` receiver and accepts an ordinary synchronous delegate, so an async chain
never has to be broken by an `await` and a temporary variable just because one step happens not to be async.


> **Package:** `ManagedCode.Communication.Extensions`, namespace `ManagedCode.Communication.Extensions`.
> One `using` gives you the whole railway surface. ASP.NET Core applications get it transitively through
> `ManagedCode.Communication.AspNetCore`. The aggregation helpers (`Result.Merge`, `Result.MergeAll`,
> `Result.Combine`, `Result.CombineAll`) are static methods on `Result` in the core package, so combining
> results needs no extra reference.

```csharp
using ManagedCode.Communication;            // Result, Problem
using ManagedCode.Communication.Extensions; // Bind / Map / Tap / Then / Ensure / Match / Compensate / ...
```

Railway-oriented programming treats operations as a series of tracks where success continues on the main track and failures switch to an error track.

### The full surface

Most operators short-circuit on failure: once a result is failed, the step is skipped and the original `Problem`
is carried to the end. The exceptions are the ones that exist to handle failure — `Else`, `Compensate*`, `Match`,
`Switch` — and `Finally`, which runs on both branches.

| Operator | Purpose |
| --- | --- |
| `Bind` / `Then` | Run the next step, which itself returns a `Result`. **Two names for one operation** — `Bind` is the conventional ROP name, `Then` reads better in long chains. Pick one per codebase. |
| `BindAsync` / `ThenAsync` | Async form of the above. |
| `Map` / `MapAsync` | Transform the value with a plain function that cannot fail. |
| `Tap` / `TapAsync`, `Do` / `DoAsync` | Run a side effect (logging, metrics) and pass the value through unchanged. |
| `Ensure`, `Where`, `Verify`, `Check` | Fail the chain when a predicate does not hold. |
| `FailIf`, `OkIf` | Flip a result based on a predicate. |
| `Match` | Collapse to a single value by handling both branches. The usual way to leave the railway. |
| `Else` | Substitute an alternative result when the current one failed. |
| `Compensate`, `CompensateAsync`, `CompensateWith` | Recover from a failure, optionally by calling a fallback. |
| `Switch`, `SwitchFirst` | Branch on success/failure without leaving the chain. |
| `Finally` | Run an action on both branches, like a `finally` block. |
| `ToResult` | Lift a nullable value into a `Result`, failing when it is null. |

Every operator has an `…Async` form that accepts a `Task<Result>` / `Task<Result<T>>` receiver, so an
asynchronous pipeline never has to be interrupted by an `await` and a temporary variable:

```csharp
var result = await LoadUserAsync(id)
    .EnsureAsync(user => user.IsActive, Problem.Create("inactive", "User is disabled.", 403))
    .TapAsync(user => _audit.RecordAsync(user.Id))
    .BindAsync(user => LoadCartAsync(user.Id))
    .MapAsync(cart => cart.Total)
    .CompensateAsync(problem => RecoverAsync(problem))
    .MatchAsync(total => Results.Ok(total), problem => Results.Problem(problem.Detail));
```

Aggregation lives on `Result` itself in the core package, so it needs no extra reference:

| Method | Purpose |
| --- | --- |
| `Result.Merge(...)` | Succeeds when all succeed; returns the **first** failure otherwise. |
| `Result.MergeAll(...)` | Succeeds when all succeed; aggregates **every** failure otherwise. |
| `Result.Combine(...)` | Collects values into a `CollectionResult<T>`, stopping at the first failure. |
| `Result.CombineAll(...)` | Collects values, aggregating every failure. |


### Basic Chaining

```csharp
public Result<Order> ProcessOrder(int userId)
{
    return Result.From(() => GetUser(userId))
        .Then(user => ValidateUser(user))
        .Then(user => GetUserCart(user.Id))
        .Then(cart => ValidateCart(cart))
        .Then(cart => CreateOrder(cart))
        .Then(order => ProcessPayment(order))
        .Then(order => SendConfirmation(order));
}
```

### Async Operations

```csharp
public async Task<Result<Order>> ProcessOrderAsync(int userId)
{
    return await Result.From(() => GetUserAsync(userId))
        .ThenAsync(user => ValidateUserAsync(user))
        .ThenAsync(user => GetUserCartAsync(user.Id))
        .ThenAsync(cart => CreateOrderAsync(cart))
        .ThenAsync(order => ProcessPaymentAsync(order))
        .ThenAsync(order => SendConfirmationAsync(order));
}
```

### Error Recovery

```csharp
var result = await GetPrimaryService()
    .CompensateAsync(async error => 
    {
        _logger.LogWarning($"Primary service failed: {error.Detail}");
        return await GetFallbackService();
    })
    .CompensateWith(defaultValue); // Final fallback
```

### Combining Multiple Results

```csharp
// Merge: Stop at first failure
var firstFailureResult = Result.Merge(
    ValidateName(name),
    ValidateEmail(email),
    ValidateAge(age)
);

// MergeAll: aggregate all failures
var allFailuresResult = Result.MergeAll(
    ValidateName(name),
    ValidateEmail(email),
    ValidateAge(age)
);

if (allFailuresResult.TryGetProblem(out var problem))
{
    // All failures were validation failures:
    // problem.GetValidationErrors() returns merged field errors.
    //
    // Mixed failures (401/403/500/...) return aggregate problem:
    // problem.StatusCode == 500
    // problem.Extensions["errors"] contains the original Problem[] list.
}

if (allFailuresResult.TryGetProblem(out var aggregateProblem) &&
    aggregateProblem.TryGetExtension("errors", out Problem[]? originalErrors))
{
    foreach (var error in originalErrors)
    {
        Console.WriteLine($"{error.StatusCode}: {error.Title} - {error.Detail}");
    }
}

// Combine: Aggregate values
var combined = Result.Combine(
    GetUserProfile(),
    GetUserSettings(),
    GetUserPermissions()
); // Returns CollectionResult<T>

// CombineAll: aggregate failures while preserving original errors
var combinedAll = Result.CombineAll(
    GetUserProfile(),
    GetUserSettings(),
    GetUserPermissions()
);
```

## Command Pattern and Idempotency

### Command Infrastructure

The library includes built-in support for command pattern with distributed idempotency:

```csharp
// Basic command
public class CreateOrderCommand : Command<Order>
{
    public CreateOrderCommand(string orderId, Order order) 
        : base(orderId, "CreateOrder")
    {
        Value = order;
        UserId = "user123";
        CorrelationId = Guid.NewGuid().ToString();
    }
}

// Command with metadata
var command = new Command("command-id", "ProcessPayment")
{
    UserId = "user123",
    SessionId = "session456",
    CorrelationId = "correlation789",
    CausationId = "parent-command-id",
    TraceId = Activity.Current?.TraceId.ToString(),
    SpanId = Activity.Current?.SpanId.ToString()
};
```

### Pagination Commands

Pagination is now a first-class command concept that keeps factories DRY and metadata consistent:

```csharp
var options = new PaginationOptions(defaultPageSize: 25, maxPageSize: 100);
var request = PaginationRequest.Create(skip: 0, take: 0, options); // take defaults to 25

// Rich factory surface without duplicate overloads
var paginationCommand = PaginationCommand.Create(request, options)
    .WithCorrelationId(Guid.NewGuid().ToString());

// Apply to results without manually recalculating metadata
var page = CollectionResult<Order>.Succeed(orders, paginationCommand.Value!, totalItems: 275, options);

// Use enum-based command types when desired
enum PaginationCommandType { ListCustomers }
var typedCommand = PaginationCommand.Create(PaginationCommandType.ListCustomers);
```

`PaginationRequest` exposes helpers such as `Normalize`, `ClampToTotal`, and `ToSlice` to keep skip/take logic predictable. Configure bounds globally with `PaginationOptions` to protect APIs from oversized queries.

### Idempotent Command Execution

#### ASP.NET Core Idempotency

```csharp
// Register idempotency store
builder.Services.AddSingleton<ICommandIdempotencyStore, InMemoryCommandIdempotencyStore>();
// Or use Orleans-based store
builder.Services.AddSingleton<ICommandIdempotencyStore, OrleansCommandIdempotencyStore>();

// Service with idempotent operations
public class PaymentService
{
    private readonly ICommandIdempotencyStore _idempotencyStore;
    
    public async Task<Result<Payment>> ProcessPaymentAsync(ProcessPaymentCommand command)
    {
        // Automatic idempotency - returns cached result if already executed
        return await _idempotencyStore.ExecuteIdempotentAsync(
            command.Id,
            async () =>
            {
                // This code runs only once per command ID
                var payment = await _paymentGateway.ChargeAsync(command.Amount);
                await _repository.SavePaymentAsync(payment);
                return Result<Payment>.Succeed(payment);
            },
            command.Metadata
        );
    }
}
```

#### Orleans-Based Idempotency

```csharp
// Automatic idempotency with Orleans grains
public class OrderGrain : Grain, IOrderGrain
{
    private readonly ICommandIdempotencyStore _idempotencyStore;
    
    public async Task<Result<Order>> CreateOrderAsync(CreateOrderCommand command)
    {
        // Uses ICommandIdempotencyGrain internally for distributed coordination
        return await _idempotencyStore.ExecuteIdempotentAsync(
            command.Id,
            async () =>
            {
                // Guaranteed to execute only once across the cluster
                var order = new Order { /* ... */ };
                await SaveOrderAsync(order);
                return Result<Order>.Succeed(order);
            }
        );
    }
}
```

### Command Execution Status

```csharp
public enum CommandExecutionStatus
{
    NotStarted,    // Command hasn't been processed
    Processing,    // Currently being processed
    Completed,     // Successfully completed
    Failed,        // Processing failed
    Expired        // Result expired from cache
}

// Check command status
var status = await _idempotencyStore.GetCommandStatusAsync("command-id");
if (status == CommandExecutionStatus.Completed)
{
    var result = await _idempotencyStore.GetCommandResultAsync<Order>("command-id");
}
```

### Command Correlation and Tracing Identifiers

Commands implement `ICommand` and surface correlation, causation, trace, span, user, and session identifiers alongside optional metadata so every hop can attach observability context. The base `Command` and `Command<T>` types keep those properties on the
root object, and serializers/Orleans surrogates round-trip them without custom plumbing.
root object, and serializers/Orleans surrogates round-trip them without custom plumbing.

#### Identifier lifecycle
- Static command factories generate monotonic version 7 identifiers via `Guid.CreateVersion7()` and stamp a UTC timestamp so commands can be sorted chronologically even when sharded.
- Factory helpers never mutate the correlation or trace identifiers; callers opt in through fluent extension
  methods that return the same command instance, so they chain freely:

| Method | Sets |
| --- | --- |
| `WithCorrelationId(id)` | Correlation identifier shared by everything in one logical operation. |
| `WithCausationId(id)` | Identifier of the command that caused this one. |
| `WithTraceId(id)` / `WithSpanId(id)` | Distributed-tracing identifiers. |
| `WithUserId(id)` | Acting user. |
| `WithSessionId(id)` | Session the command belongs to. |
| `WithMetadata(metadata)` / `WithMetadata(m => …)` | Replaces or edits the whole `CommandMetadata`. |

Every factory generates the command id itself — a time-ordered UUIDv7 — and takes `commandId` as an **optional
trailing parameter**. Pass one only when the identity comes from outside: an idempotency key sent by the caller,
or a replayed message whose identity must be preserved.

```csharp
var command  = Command<PlaceOrder>.From(payload);                    // id generated
var replayed = Command<PlaceOrder>.From(payload, idempotencyKey);    // id supplied
```

Only the command id is generated. Correlation, causation, trace, span, user and session describe how a command
relates to the rest of the system, which the library cannot infer — they stay `null` until you set them.

Correlation, causation, trace, span, user and session identifiers live on the command itself
(`command.CorrelationId`, `command.UserId`, …); `CommandMetadata` carries the rest — priority, retries, timeout,
tags and free-form properties.

```csharp
var command = Command<PlaceOrder>.From(payload)
    .WithCorrelationId(correlationId)
    .WithCausationId(parentCommandId)
    .WithUserId(user.Id)
    .WithSessionId(session.Id)
    .WithMetadata(metadata => metadata.Priority = CommandPriority.High);
```
- Metadata mirrors the trace/span identifiers for workload-specific diagnostics without coupling transport-level identifiers to
payload annotations.

#### Field reference

| Field | Purpose | Typical source | Notes |
| --- | --- | --- | --- |
| `CommandId` | Unique, monotonic identifier for deduplication | Static command factories | Remains stable for retries and storage lookups. |
| `CorrelationId` | Ties a command to an upstream workflow/request | HTTP `X-Correlation-Id`, message headers | Preserved through
 serialization and Orleans surrogates. |
| `CausationId` | Records the predecessor command/event | Current command ID | Supports causal chains in telemetry. |
| `TraceId` | Connects to distributed tracing spans | OpenTelemetry/`Activity` context | The library stores, but never generate
s, trace identifiers. |
| `SpanId` | Identifies the originating span | OpenTelemetry/`Activity` context | Often paired with `Metadata.TraceId` for deep
er traces. |
| `UserId` / `SessionId` | Attach security/session principals | Authentication middleware | Useful for multi-tenant auditing. |

#### Trace vs. correlation
- **Correlation IDs** bundle every command spawned from a single business request. Assign them at ingress and keep the value st
able across retries so dashboards can answer “what commands ran because of this call?”.
- **Trace/Span IDs** follow distributed tracing semantics. Commands avoid creating new traces and instead persist the ambient `A
ctivity` identifiers through serialization so telemetry back-ends can stitch spans together.
- Both identifier sets are serialized together, enabling pivots between business-level correlation and technical call graphs wit
hout extra configuration.

#### Generation and propagation guidance
- Use `Command.Create(...)` / `Command<T>.Create(...)` (or the matching `From(...)` helpers) to get a version 7 identifier and U
TC timestamp automatically.
- Read or generate correlation IDs from HTTP headers or upstream messages and apply them via `.WithCorrelationId(...)` before d
ispatching commands.
- Capture `Activity.TraceId`/`Activity.SpanId` through `.WithTraceId(...)` and `.WithSpanId(...)` (and metadata counterparts) wh
en bridging to queues, Orleans, or background pipelines.
- Serialization tests verify the identifiers round-trip, so consumers can rely on receiving the same values they emitted.

#### Operational considerations
- Factory unit tests ensure commands created through the helpers carry version 7 identifiers, UTC timestamps, and derived `Comma
ndType` values for traceability.
- Idempotency regression tests assert that concurrent callers reuse cached results and propagate failures consistently, preservi
ng correlation integrity when retry storms occur.

### Idempotency Architecture Overview

#### Scope
The shared idempotency helpers (`CommandIdempotencyExtensions`), default in-memory store, and test coverage work together to pro
tect concurrency, caching, and retry behaviour across hosts.

#### Strengths
- **Deterministic status transitions.** `ExecuteIdempotentAsync` only invokes the provided delegate after atomically claiming th
e command, writes the result, and then flips the status to `Completed`, so retries either reuse cached output or wait for the in
-flight execution to finish.
- **Batch reuse of cached outputs.** Batch helpers perform bulk status/result lookups and bypass execution for already completed
 commands, even when cached results are `null` or default values.
- **Fine-grained locking in the memory store.** Per-command `SemaphoreSlim` instances eliminate global contention, and reference
 counting ensures locks are released once no callers use a key.
- **Concurrency regression tests.** Dedicated unit tests confirm that concurrent callers share a single execution, failed primar
y runs surface consistent exceptions, and the final status ends up in `Failed` when appropriate.

#### Risks & considerations
- **Missing-result ambiguity.** If a store reports `Completed` but the result entry expired, the extensions currently return the
 default value. Stores that can distinguish “missing” from “stored default” should override `TryGetCachedResultAsync` to trigger
 a re-execution.
- **Wait semantics rely on polling.** Adaptive polling keeps responsiveness reasonable, but distributed stores can swap in push-
style notifications if tail latency becomes critical.
- **Status retention policies.** The memory store’s cleanup removes status and result after a TTL; other implementations must pr
ovide similar hygiene to avoid unbounded growth while keeping enough history for retries.

#### Recommendations
1. Document store-specific retention guarantees so callers can tune retry windows.
2. Consider extending the store contract with a boolean flag (or sentinel wrapper) that differentiates cached `default` values f
rom missing entries.
3. Monitor lock-pool growth in long-lived applications and log keys that never release to diagnose misbehaving callers before me
mory pressure builds up.

## Error Handling Patterns

### Validation Pattern

```csharp
public Result<User> CreateUser(CreateUserDto dto)
{
    // Collect all validation errors
    var errors = new List<(string field, string message)>();
    
    if (string.IsNullOrEmpty(dto.Email))
        errors.Add(("email", "Email is required"));
    
    if (!dto.Email.Contains("@"))
        errors.Add(("email", "Invalid email format"));
    
    if (dto.Age < 0)
        errors.Add(("age", "Age must be positive"));
    
    if (dto.Age < 18)
        errors.Add(("age", "Must be 18 or older"));
    
    if (errors.Any())
        return Result<User>.FailValidation(errors.ToArray());
    
    var user = new User { /* ... */ };
    return Result<User>.Succeed(user);
}
```

### Repository Pattern with Entity Framework

```csharp
public class UserRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserRepository> _logger;
    
    public async Task<Result<User>> GetByIdAsync(int id)
    {
        try
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
            
            if (user == null)
                return Result<User>.FailNotFound($"User {id} not found");
            
            return Result<User>.Succeed(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error getting user {UserId}", id);
            return Result<User>.Fail(ex);
        }
    }
    
    public async Task<CollectionResult<User>> GetPagedAsync(
        int page, 
        int pageSize,
        Expression<Func<User, bool>>? filter = null,
        Expression<Func<User, object>>? orderBy = null)
    {
        try
        {
            // Build query with IQueryable for efficient SQL generation
            IQueryable<User> query = _context.Users.AsNoTracking();
            
            // Apply filter if provided
            if (filter != null)
                query = query.Where(filter);
            
            // Apply ordering
            query = orderBy != null 
                ? query.OrderBy(orderBy) 
                : query.OrderBy(u => u.Id);
            
            // Get total count - generates COUNT(*) SQL query
            var totalItems = await query.CountAsync();
            
            if (totalItems == 0)
                return CollectionResult<User>.Succeed(Array.Empty<User>(), page, pageSize, 0);
            
            // Get page of data - generates SQL with OFFSET and FETCH
            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToArrayAsync();
            
            return CollectionResult<User>.Succeed(users, page, pageSize, totalItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error in GetPagedAsync");
            return CollectionResult<User>.Fail(ex);
        }
    }
    
    // Example with complex query
    public async Task<CollectionResult<UserDto>> SearchUsersAsync(
        string searchTerm,
        int page,
        int pageSize)
    {
        try
        {
            var query = _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive)
                .Where(u => EF.Functions.Like(u.Name, $"%{searchTerm}%") ||
                           EF.Functions.Like(u.Email, $"%{searchTerm}%"));
            
            // Count before projection for efficiency
            var totalItems = await query.CountAsync();
            
            // Project to DTO and paginate - single SQL query
            var users = await query
                .OrderBy(u => u.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    LastLoginDate = u.LastLoginDate
                })
                .ToArrayAsync();
            
            return CollectionResult<UserDto>.Succeed(users, page, pageSize, totalItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed for term: {SearchTerm}", searchTerm);
            return CollectionResult<UserDto>.Fail(ex);
        }
    }
}
```

### Service Layer Pattern

```csharp
public class OrderService
{
    public async Task<Result<Order>> CreateOrderAsync(CreateOrderDto dto)
    {
        // Validate input
        var validationResult = ValidateOrderDto(dto);
        if (validationResult.IsFailed)
            return validationResult;
        
        // Get user
        var userResult = await _userRepo.GetByIdAsync(dto.UserId);
        if (userResult.IsFailed)
            return Result<Order>.Fail(userResult.Problem);
        
        // Check permissions
        var user = userResult.Value;
        if (!user.CanCreateOrders)
            return Result<Order>.FailForbidden("User cannot create orders");
        
        // Create order
        return await Result.Try(async () =>
        {
            var order = new Order
            {
                UserId = user.Id,
                Items = dto.Items,
                Total = CalculateTotal(dto.Items)
            };
            
            await _orderRepo.SaveAsync(order);
            return order;
        });
    }
}
```

## Integration Guides

### ASP.NET Core Integration

#### Installation and Setup

```csharp
// 1. Install NuGet package
// dotnet add package ManagedCode.Communication.AspNetCore

// 2. Program.cs configuration
var builder = WebApplication.CreateBuilder(args);

// Method 1: Simple configuration with auto-detection of environment
builder.AddCommunication(); // ShowErrorDetails = IsDevelopment

// Method 2: Custom configuration
builder.Services.AddCommunication(options =>
{
    options.ShowErrorDetails = true; // Show detailed error messages in responses
});

// 3. Add filters to MVC controllers (ORDER MATTERS!)
builder.Services.AddControllers(options =>
{
    options.AddCommunicationFilters();
    // Filters are applied in this order:
    // 1. CommunicationModelValidationFilter - Catches validation errors first
    // 2. ResultToActionResultFilter - Converts Result to HTTP response
    // 3. CommunicationExceptionFilter - Catches any unhandled exceptions last
});

// 4. Optional: Add filters to SignalR hubs
builder.Services.AddSignalR(options =>
{
    options.AddCommunicationFilters();
});

var app = builder.Build();
```

#### Filter Execution Order

The order of filters is important for proper error handling:

| Order | Filter | Purpose | When It Runs |
|-------|--------|---------|--------------|
| 1 | `CommunicationModelValidationFilter` | Converts ModelState errors to `Result.FailValidation` | Before action execution if model is invalid |
| 2 | `ResultToActionResultFilter` | Maps `Result<T>` return values to HTTP responses | After action execution |
| 3 | `CommunicationExceptionFilter` | Catches unhandled exceptions, returns Problem Details | On any exception |

⚠️ **Important**: The filters must be registered using `AddCommunicationFilters()` to ensure correct ordering. Manual registration may cause unexpected behavior.

#### Controller Implementation

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(User), 200)]
    [ProducesResponseType(typeof(Problem), 404)]
    public async Task<Result<User>> GetUser(int id)
    {
        return await _userService.GetUserAsync(id);
    }
    
    [HttpPost]
    [ProducesResponseType(typeof(User), 201)]
    [ProducesResponseType(typeof(Problem), 400)]
    public async Task<Result<User>> CreateUser([FromBody] CreateUserDto dto)
    {
        return await _userService.CreateUserAsync(dto);
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(CollectionResult<User>), 200)]
    public async Task<CollectionResult<User>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        return await _userService.GetUsersAsync(page, pageSize);
    }
}
```

#### Automatic HTTP Response Mapping

The library automatically converts Result types to appropriate HTTP responses:

| Result State | HTTP Status | Response Body |
|-------------|-------------|---------------|
| `Result.Succeed()` | 204 No Content | Empty |
| `Result<T>.Succeed(value)` | 200 OK | `value` |
| `Result.FailValidation(...)` | 400 Bad Request | Problem Details |
| `Result.FailUnauthorized()` | 401 Unauthorized | Problem Details |
| `Result.FailForbidden()` | 403 Forbidden | Problem Details |
| `Result.FailNotFound()` | 404 Not Found | Problem Details |
| `Result.Fail(...)` | 500 Internal Server Error | Problem Details |

### SignalR Integration

```csharp
public class ChatHub : Hub
{
    public async Task<Result<MessageDto>> SendMessage(string user, string message)
    {
        if (string.IsNullOrEmpty(message))
            return Result<MessageDto>.FailValidation(("message", "Message cannot be empty"));
        
        var messageDto = new MessageDto
        {
            User = user,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
        
        await Clients.All.SendAsync("ReceiveMessage", user, message);
        return Result<MessageDto>.Succeed(messageDto);
    }
    
    public async Task<Result> JoinGroup(string groupName)
    {
        if (string.IsNullOrEmpty(groupName))
            return Result.FailValidation(("groupName", "Group name is required"));
        
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        return Result.Succeed();
    }
}
```

### Microsoft Orleans Integration

#### Setup

```csharp
// Silo configuration
var builder = Host.CreateDefaultBuilder(args)
    .UseOrleans(silo =>
    {
        silo.UseLocalhostClustering()
            .UseOrleansCommunication(); // Required for Result serialization
    });

// Client configuration  
var clientBuilder = Host.CreateDefaultBuilder(args)
    .UseOrleansClient(client =>
    {
        client.UseOrleansCommunication(); // Required for Result serialization
    });
```

That's it! The `UseOrleansCommunication()` extension automatically configures:
- Serialization for all Result types across grain boundaries
- Proper handling of Problem Details in distributed calls
- Support for CollectionResult with pagination
- Exception-to-failed-result conversion for grain methods returning `Task<Result>`, `Task<Result<T>>`, `Task<CollectionResult<T>>`, and matching `ValueTask<>` forms
- Structured error logging with the original exception object before a grain exception is converted to a failed Result, so observability backends keep the real stack trace

#### Grain Implementation

```csharp
public interface IUserGrain : IGrainWithStringKey
{
    Task<Result<UserState>> GetStateAsync();
    Task<Result> UpdateProfileAsync(UpdateProfileDto dto);
    Task<CollectionResult<Activity>> GetActivitiesAsync(int page, int pageSize);
}

public class UserGrain : Grain, IUserGrain
{
    private readonly IPersistentState<UserState> _state;
    
    public UserGrain([PersistentState("user")] IPersistentState<UserState> state)
    {
        _state = state;
    }
    
    public Task<Result<UserState>> GetStateAsync()
    {
        if (!_state.RecordExists)
            return Task.FromResult(Result<UserState>.FailNotFound("User not found"));
        
        return Task.FromResult(Result<UserState>.Succeed(_state.State));
    }
    
    public async Task<Result> UpdateProfileAsync(UpdateProfileDto dto)
    {
        if (!_state.RecordExists)
            return Result.FailNotFound("User not found");
        
        // Validate
        if (string.IsNullOrEmpty(dto.DisplayName))
            return Result.FailValidation(("displayName", "Display name is required"));
        
        // Update state
        _state.State.DisplayName = dto.DisplayName;
        _state.State.Bio = dto.Bio;
        _state.State.UpdatedAt = DateTime.UtcNow;
        
        await _state.WriteStateAsync();
        return Result.Succeed();
    }
    
    public async Task<CollectionResult<Activity>> GetActivitiesAsync(int page, int pageSize)
    {
        if (!_state.RecordExists)
            return CollectionResult<Activity>.FailNotFound("User not found");
        
        // For real data, use a repository with Entity Framework
        var repository = GrainFactory.GetGrain<IActivityRepositoryGrain>(0);
        return await repository.GetUserActivitiesAsync(this.GetPrimaryKeyString(), page, pageSize);
    }
}
```

## Performance

### Keeping results cheap

1. **Use structs**: `Result` and `Result<T>` are value types (structs), so a success carries no heap allocation
2. **Avoid boxing**: Use generic methods to prevent boxing of value types
3. **Chain operations**: Use railway-oriented programming to avoid intermediate variables
4. **Async properly**: Use `ConfigureAwait(false)` in library code
5. **Build problems per failure, do not share them**: `Problem` is mutable — it has settable properties and an
   `Extensions` dictionary that `AddValidationError` writes into. A shared static instance can be mutated by any
   caller that touches it, poisoning every later use. Create one per failure, or use a factory method.
6. **Prefer the `CancellationToken` overloads** of the idempotency helpers. `Func<Task<T>>` cannot observe
   cancellation, so a timeout or a cancelled caller has to wait for the operation to finish on its own.

### Serialization cost

`Result`, `Result<T>` and `CqrsStreamChunk<,>` carry hand-written `System.Text.Json` converters, attached to the
types themselves — you get them with any `JsonSerializerOptions`, without registering anything.

They exist because the default path is expensive for these particular shapes: a struct that mixes `init`-only
members with a private `[JsonInclude]` field pushes `System.Text.Json` onto a reflection-driven path. Measured,
per operation:

| | Allocated |
| --- | --- |
| Deserialize `Result<T>` | 208 B |
| Deserialize a stream chunk | 344 B |
| Serialize a stream chunk | 176 B |
| Chunk size on the wire | 148 B |

`CollectionResult<T>` needs no converter — it has no private serialized field, so the default path costs it only
~56 bytes over the items themselves.

That is close to the floor. Deserializing a progress chunk allocates 216 bytes, of which 136 are the chunk
object and 48 the payload string inside it — both of which any code path has to allocate. The serializer's own
share is about 32 bytes.

On the client, chunks are deserialized straight from each frame's UTF-8 bytes rather than from a string per
frame, and the JSON contract is resolved once per stream instead of once per frame. Reading a 20 000-frame
stream end to end costs about 292 bytes per frame against 171 bytes of payload on the wire.

#### The payload type matters more than anything else here

This is worth more than everything above put together, and it lives in your code rather than in this library.
`System.Text.Json` deserializes a type with a parameterized constructor through a different path than one it can
populate property by property, and that path allocates. Per object, on the same JSON:

| Payload shape | Allocated |
| --- | --- |
| `record Progress(string State)` | 176 B |
| `record Progress { public string? State { get; init; } }` | 72 B |
| `class Progress { public string? State { get; init; } }` | 72 B |
| `class Progress { public string? State { get; set; } }` | 72 B |

The positional record costs 104 bytes more per object for no benefit — the `init` record is just as immutable.
On a stream running at a thousand chunks a second that is 100 KB/s of pure garbage, so prefer the second form
for progress and result payloads. Everywhere else the difference is too small to think about.

If the payload type is not yours to reshape, hand its source-generated contract to the transport instead — that
recovers most of the same ground without touching the type:

```csharp
[JsonSerializable(typeof(Progress))]
internal partial class StreamPayloads : JsonSerializerContext;

private static readonly CqrsStreamClientOptions StreamOptions = new()
{
    JsonSerializerOptions = CqrsStreamSerialization.WithPayloadContext(StreamPayloads.Default)
};

await foreach (var chunk in http.GetForCqrsStreamAsync<Progress, Report>("/reports/1", StreamOptions))
{
    // ...
}
```

`WithPayloadContext` consults your context first and falls back to reflection for everything it does not cover,
including the transport's own chunk, result and problem types. Pointing `TypeInfoResolver` straight at your
context instead would leave `CqrsStreamChunk<,>` without a contract and fail on the first chunk — combining is
the whole point of the method. The wire format is unchanged either way, so one end may use a context and the
other not.

That is the client half. On the server the SSE response is written with ASP.NET Core's own JSON options, so add
the context there in the usual way:

```csharp
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.TypeInfoResolverChain.Insert(0, StreamPayloads.Default));
```

`ManagedCode.Communication.Tests/Results/SerializationAllocationTests.cs` holds allocation budgets for all of
this, so a regression fails the build rather than going unnoticed.

## Behaviour Notes

Things that surprise people, gathered in one place.

### `Problem` is mutable

`Problem` has settable properties and a live `Extensions` dictionary. Treat every instance as owned by one
failure. Do not cache a shared instance in a `static` field: `AddValidationError`, `ErrorCode` and the property
setters all mutate in place, so one caller can change what every later caller sees.

### Exception-to-status mapping is a heuristic

`HttpStatusCodeHelper.GetStatusCodeForException` classifies common exception types. It cannot know your domain,
so register overrides at startup with `ExceptionStatusCodeMap` — see
[Mapping exceptions to status codes](#mapping-exceptions-to-status-codes).

### Results are immutable once built

`Result<T>.Value` and the `CollectionResult<T>` members are `init`-only. Build results through `Succeed` / `Fail`;
serializers can still populate them.

### HTTP status vs. command outcome

A failed command reported over CQRS streaming still arrives on a `200 OK` response — the HTTP exchange
succeeded, the command did not. Inspect the terminal chunk, not the status code. A non-2xx status means the
request never reached the handler.

## Registration Reference

### Nothing is required

Every part of the library works with **no registration at all** — no container, no logger, no OpenTelemetry.
`Result`, `Problem`, railway operators, the CQRS contract and its HTTP client are plain types you can `new` up in
a console app or a unit test.

If you never call `CommunicationLogger.Configure`, logging falls back to an internal factory that writes nowhere
and throws nothing. If you never subscribe to `CommunicationTelemetry.SourceName`, recording a failure is a
couple of null checks. Registration turns signals *on*; it is never a precondition for correctness.

### What each entry point does

**Core** (`ManagedCode.Communication`)

| Call | Effect |
| --- | --- |
| `services.ConfigureCommunication(loggerFactory)` | Points the library's internal logger at your factory. Optional. |
| `CommunicationLogger.Configure(serviceProvider \| loggerFactory)` | Same, without a service collection. Optional. |
| `ExceptionStatusCodeMap.Map<TException>(status)` | Overrides the exception-to-status mapping. Call once at startup. |

**Commands and idempotency** (`ManagedCode.Communication`)

| Call | Effect |
| --- | --- |
| `services.AddCommandIdempotency()` | In-memory store plus the background cleanup service. |
| `services.AddCommandIdempotency<TStore>()` | Your own `ICommandIdempotencyStore`, with cleanup. |
| `services.AddCommandIdempotencyStore<TStore>()` | Store only, no background service. |
| `services.AddCommandIdempotencyWithManualCleanup<TStore>()` | Store plus cleanup you trigger yourself. |

**ASP.NET Core** (`ManagedCode.Communication.AspNetCore`)

| Call | Effect |
| --- | --- |
| `services.AddCommunication(options)` | Logging plus the MVC filters. The usual one-liner. |
| `services.AddCommunicationAspNetCore([loggerFactory])` | Logging only. |
| `services.AddCommunicationFilters()` | MVC filters only: exception handling, model validation, `Result` → status code. |
| `services.AddControllers(o => o.AddCommunicationFilters())` | Same, applied directly to `MvcOptions`. |
| `app.UseCommunication()` | Middleware for request-scoped handling. |
| `services.AddCommunicationCqrs([options])` | CQRS Server-Sent Events transport plus `CqrsStreamServerOptions`. |
| `services.AddControllers(o => o.AddCommunicationCqrsFilters())` | CQRS MVC filter only. |
| `endpoint.WithCommunicationResults()` | Minimal API: map a returned `Result` to an HTTP response. |
| `endpoint.WithCommunicationCqrsResults([options])` | Minimal API: render a chunk stream as SSE. |
| `services.AddSignalR(o => o.AddCommunicationHubFilter())` | Hub filter turning hub exceptions into failed results. |

**Orleans** (`ManagedCode.Communication.Orleans`)

| Call | Effect |
| --- | --- |
| `siloBuilder.UseOrleansCommunication()` | Grain call filters and the serialization surrogates. |
| `clientBuilder.UseOrleansCommunication()` | The client-side half of the same. |

**Observability** — see [Observability](#observability):

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddSource(CommunicationTelemetry.SourceName))
    .WithMetrics(m => m.AddMeter(CommunicationTelemetry.SourceName));
```

### A typical web application

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCommunication();        // logging + MVC filters
builder.Services.AddCommunicationCqrs();    // only if you stream commands
builder.Services.AddCommandIdempotency();   // only if you need idempotent commands

builder.Services.AddOpenTelemetry()         // only if you collect telemetry
    .WithTracing(t => t.AddSource(CommunicationTelemetry.SourceName))
    .WithMetrics(m => m.AddMeter(CommunicationTelemetry.SourceName));

var app = builder.Build();
app.UseCommunication();
app.MapControllers();
app.Run();
```

Drop any line you do not need — none of them are load-bearing for the rest.

## Observability

### OpenTelemetry

Failures are reported through `System.Diagnostics.ActivitySource` and `System.Diagnostics.Metrics.Meter`, both of
which ship with .NET — the library takes **no dependency on the OpenTelemetry SDK**. Subscribe to the source and
the signals appear; subscribe to nothing and recording costs a couple of null checks.

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(CommunicationTelemetry.SourceName))
    .WithMetrics(metrics => metrics.AddMeter(CommunicationTelemetry.SourceName));
```

| Signal | Name | Notes |
| --- | --- | --- |
| Traces | `ManagedCode.Communication` | Failed operations set the span status to `Error` and tag it with `error.type`, `problem.type`, `problem.title`, `problem.status`, `problem.error_code`. |
| Metric | `communication.result.failures` | Counter of failed results, tagged by `error.type` and `problem.status`. |
| Metric | `communication.exceptions` | Counter of exceptions converted into a `Problem`. |

### Recording the real error

A `Problem` built from an exception keeps only the exception's type name and message — **the stack trace and any
inner exceptions are gone**. Pass the exception itself so it reaches the trace as a proper exception event:

```csharp
catch (Exception exception)
{
    var problem = Problem.Create(exception);
    CommunicationDiagnostics.ReportFailure(logger, problem, exception); // logs + traces, stack trace included
    return Result<Order>.Fail(problem);
}
```

The ASP.NET Core exception filter, the SignalR hub filter and the Orleans grain call filter already do this for
you — anything they convert arrives in your traces with its original stack.

### Helpers

```csharp
// Report a failure without breaking a chain; a successful result passes through untouched.
var result = LoadOrder(id).Report(logger);

// Wrap an operation in a span, reporting whatever it returns or throws.
var order = await CommunicationDiagnostics.TrackAsync("orders.place",
    () => _service.PlaceAsync(cart), logger);
```

`Track`/`TrackAsync` convert a thrown exception into a failed `Result<T>`, so callers stay on the Result path
while the exception still reaches the log and the trace.

Static, source-generated logging lives in `LoggerCenter` (general) and `ProblemLoggerCenter` (failures), so
logging a failure allocates nothing when the level is disabled.

### Mapping exceptions to status codes

The rule is **4xx means the caller was wrong, 5xx means the server was**. `InvalidOperationException`,
`NotSupportedException`, `InvalidCastException`, `NullReferenceException` and `IndexOutOfRangeException` are
server defects and map to **500** — reporting them as 400 blames the client and hides the defect from every
alert watching the 5xx rate.

The mapping cannot know your domain, so override it once at startup:

```csharp
ExceptionStatusCodeMap.Map<OrderNotFoundException>(HttpStatusCode.NotFound);
ExceptionStatusCodeMap.Map<DomainRuleViolationException>(HttpStatusCode.UnprocessableEntity);
```

Lookup walks the exception's type hierarchy, so mapping a base type covers everything derived from it and the
most derived registration wins.

## Testing

The repository uses xUnit with [Shouldly](https://github.com/shouldly/shouldly) for assertions. Shared matchers such as `ShouldBeEquivalentTo` and `AssertProblem()` live in `ManagedCode.Communication.Tests/TestHelpers`, keeping tests fluent without FluentAssertions.

- Run the full suite: `dotnet test ManagedCode.Communication.Tests/ManagedCode.Communication.Tests.csproj`
- Generate lcov coverage: `dotnet test ManagedCode.Communication.Tests/ManagedCode.Communication.Tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=lcov`

The suite is 1 089 tests and runs in a few seconds. Line coverage: core ~80%, ASP.NET Core ~98%, Extensions ~79%, Orleans ~97%. Mirror the existing patterns when adding APIs — exercise both the success and the failure path, and drive the public surface rather than internal helpers.

## Comparison

### Comparison with Other Libraries

| Feature | ManagedCode.Communication | FluentResults | CSharpFunctionalExtensions | ErrorOr |
|---------|--------------------------|---------------|---------------------------|---------|
| **Multiple Errors** | ✅ Yes | ✅ Yes | ❌ No | ✅ Yes |
| **Railway-Oriented** | ✅ Full | ✅ Full | ✅ Full | ⚠️ Limited |
| **HTTP Integration** | ✅ Built-in | ❌ No | ⚠️ Extension | ❌ No |
| **Orleans Support** | ✅ Built-in | ❌ No | ❌ No | ❌ No |
| **SignalR Support** | ✅ Built-in | ❌ No | ❌ No | ❌ No |
| **RFC 7807** | ✅ Full | ❌ No | ❌ No | ❌ No |
| **Pagination** | ✅ Built-in | ❌ No | ❌ No | ❌ No |
| **Command Pattern** | ✅ Built-in | ❌ No | ❌ No | ❌ No |
| **Performance** | ✅ Struct-based | ❌ Class-based | ✅ Struct-based | ✅ Struct-based |
| **Async Support** | ✅ Full | ✅ Full | ✅ Full | ✅ Full |

### When to Use ManagedCode.Communication

Choose this library when you need:

- **Full-stack integration**: ASP.NET Core + SignalR + Orleans
- **Standardized errors**: RFC 7807 Problem Details
- **Pagination**: Built-in collection results with paging
- **Command pattern**: Command infrastructure with idempotency
- **Performance**: Struct-based implementation for minimal overhead

## Best Practices

### DO ✅

```csharp
// DO: Use Result for operations that can fail
public Result<User> GetUser(int id)
{
    var user = _repository.FindById(id);
    return user != null 
        ? Result<User>.Succeed(user)
        : Result<User>.FailNotFound($"User {id} not found");
}

// DO: Chain operations using railway-oriented programming
public Result<Order> ProcessOrder(OrderDto dto)
{
    return ValidateOrder(dto)
        .Then(CreateOrder)
        .Then(CalculateTotals)
        .Then(ApplyDiscounts)
        .Then(SaveOrder);
}

// DO: Provide specific error information
public Result ValidateEmail(string email)
{
    if (string.IsNullOrEmpty(email))
        return Result.FailValidation(("email", "Email is required"));
    
    if (!email.Contains("@"))
        return Result.FailValidation(("email", "Invalid email format"));
    
    return Result.Succeed();
}

// DO: Use CollectionResult for paginated data
public CollectionResult<Product> GetProducts(int page, int pageSize)
{
    var products = _repository.GetPaged(page, pageSize);
    var total = _repository.Count();
    return CollectionResult<Product>.Succeed(products, page, pageSize, total);
}
```

### DON'T ❌

```csharp
// DON'T: Throw exceptions from Result-returning methods
public Result<User> GetUser(int id)
{
    if (id <= 0)
        throw new ArgumentException("Invalid ID"); // ❌ Don't throw
    
    // Instead:
    if (id <= 0)
        return Result<User>.FailValidation(("id", "ID must be positive")); // ✅
}

// DON'T: Ignore Result values
var result = UpdateUser(user); // ❌ Result ignored
DoSomethingElse();

// Instead:
var result = UpdateUser(user);
if (result.IsFailed)
    return result; // ✅ Handle the failure

// DON'T: Mix Result and exceptions
public async Task<User> GetUserMixed(int id)
{
    var result = await GetUserAsync(id);
    if (result.IsFailed)
        throw new Exception(result.Problem.Detail); // ❌ Mixing patterns
    
    return result.Value;
}

// DON'T: Create generic error messages
return Result.Fail("Error"); // ❌ Too vague

// Instead:
return Result.Fail("User creation failed", "Email already exists"); // ✅
```

## Examples

### Complete Web API Example

```csharp
// Domain Model
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

// Service Interface
public interface IProductService
{
    Task<Result<Product>> GetByIdAsync(int id);
    Task<Result<Product>> CreateAsync(CreateProductDto dto);
    Task<Result> UpdateStockAsync(int id, int quantity);
    Task<CollectionResult<Product>> SearchAsync(string query, int page, int pageSize);
}

// Service Implementation
public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ProductService> _logger;
    
    public async Task<Result<Product>> GetByIdAsync(int id)
    {
        return await Result.Try(async () =>
        {
            var product = await _repository.FindByIdAsync(id);
            return product ?? throw new KeyNotFoundException($"Product {id} not found");
        })
        .CompensateAsync(async error =>
        {
            _logger.LogWarning("Product {Id} not found, checking archive", id);
            var archived = await _repository.FindInArchiveAsync(id);
            return archived != null
                ? Result<Product>.Succeed(archived)
                : Result<Product>.FailNotFound($"Product {id} not found");
        });
    }
    
    public async Task<Result<Product>> CreateAsync(CreateProductDto dto)
    {
        // Validation
        var validationResult = await ValidateProductDto(dto);
        if (validationResult.IsFailed)
            return Result<Product>.Fail(validationResult.Problem);
        
        // Check for duplicates
        var existing = await _repository.FindByNameAsync(dto.Name);
        if (existing != null)
            return Result<Product>.Fail("Duplicate product", 
                $"Product with name '{dto.Name}' already exists");
        
        // Create product
        var product = new Product
        {
            Name = dto.Name,
            Price = dto.Price,
            Stock = dto.InitialStock
        };
        
        await _repository.AddAsync(product);
        await _repository.SaveChangesAsync();
        
        return Result<Product>.Succeed(product);
    }
    
    public async Task<Result> UpdateStockAsync(int id, int quantity)
    {
        return await GetByIdAsync(id)
            .Then(product =>
            {
                if (product.Stock + quantity < 0)
                    return Result.Fail("Insufficient stock", 
                        $"Cannot reduce stock by {Math.Abs(quantity)}. Current stock: {product.Stock}");
                
                product.Stock += quantity;
                return Result.Succeed();
            })
            .ThenAsync(async () =>
            {
                await _repository.SaveChangesAsync();
                return Result.Succeed();
            });
    }
    
    public async Task<CollectionResult<Product>> SearchAsync(string query, int page, int pageSize)
    {
        try
        {
            var (products, total) = await _repository.SearchAsync(query, page, pageSize);
            return CollectionResult<Product>.Succeed(products, page, pageSize, total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed for query: {Query}", query);
            return CollectionResult<Product>.Fail(ex);
        }
    }
    
    private async Task<Result> ValidateProductDto(CreateProductDto dto)
    {
        var errors = new List<(string field, string message)>();
        
        if (string.IsNullOrWhiteSpace(dto.Name))
            errors.Add(("name", "Product name is required"));
        else if (dto.Name.Length > 100)
            errors.Add(("name", "Product name must be 100 characters or less"));
        
        if (dto.Price <= 0)
            errors.Add(("price", "Price must be greater than zero"));
        
        if (dto.InitialStock < 0)
            errors.Add(("initialStock", "Initial stock cannot be negative"));
        
        // Async validation
        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            var categoryExists = await _repository.CategoryExistsAsync(dto.CategoryId);
            if (!categoryExists)
                errors.Add(("categoryId", "Invalid category"));
        }
        
        return errors.Any() 
            ? Result.FailValidation(errors.ToArray())
            : Result.Succeed();
    }
}

// Controller
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    
    [HttpGet("{id}")]
    public async Task<Result<Product>> Get(int id)
    {
        return await _productService.GetByIdAsync(id);
    }
    
    [HttpPost]
    public async Task<Result<Product>> Create([FromBody] CreateProductDto dto)
    {
        return await _productService.CreateAsync(dto);
    }
    
    [HttpPatch("{id}/stock")]
    public async Task<Result> UpdateStock(int id, [FromBody] UpdateStockDto dto)
    {
        return await _productService.UpdateStockAsync(id, dto.Quantity);
    }
    
    [HttpGet("search")]
    public async Task<CollectionResult<Product>> Search(
        [FromQuery] string q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        return await _productService.SearchAsync(q, page, pageSize);
    }
}
```

### Complex Business Logic Example

```csharp
public class OrderProcessingService
{
    public async Task<Result<Order>> ProcessOrderAsync(ProcessOrderCommand command)
    {
        // Complete order processing pipeline
        return await Result
            // Validate command
            .From(() => ValidateCommand(command))
            
            // Load user
            .ThenAsync(async () => await _userRepository.GetByIdAsync(command.UserId))
            
            // Check user permissions
            .Then(user => user.CanPlaceOrders 
                ? Result<User>.Succeed(user)
                : Result<User>.FailForbidden("User cannot place orders"))
            
            // Verify user credit
            .ThenAsync(async user => await _creditService.CheckCreditAsync(user.Id))
            .Then(creditResult => creditResult.AvailableCredit >= command.TotalAmount
                ? Result.Succeed()
                : Result.Fail("Insufficient credit"))
            
            // Check inventory
            .ThenAsync(async () => await CheckInventoryAsync(command.Items))
            
            // Reserve inventory
            .ThenAsync(async () => await ReserveInventoryAsync(command.Items))
            
            // Create order
            .ThenAsync(async () => await CreateOrderAsync(command))
            
            // Process payment
            .ThenAsync(async order => await ProcessPaymentAsync(order, command.PaymentMethod))
            
            // Send confirmation
            .ThenAsync(async order => await SendOrderConfirmationAsync(order))
            
            // Handle any failures
            .CompensateAsync(async problem =>
            {
                _logger.LogError("Order processing failed: {Problem}", problem.Detail);
                
                // Rollback inventory reservation
                await ReleaseInventoryAsync(command.Items);
                
                // Notify user
                await _notificationService.NotifyOrderFailedAsync(command.UserId, problem.Detail);
                
                return Result<Order>.Fail(problem);
            });
    }
    
    private async Task<Result> CheckInventoryAsync(List<OrderItem> items)
    {
        var unavailable = new List<string>();
        
        foreach (var item in items)
        {
            var stock = await _inventoryService.GetStockAsync(item.ProductId);
            if (stock < item.Quantity)
            {
                unavailable.Add($"{item.ProductName}: requested {item.Quantity}, available {stock}");
            }
        }
        
        return unavailable.Any()
            ? Result.Fail("Insufficient inventory", string.Join("; ", unavailable))
            : Result.Succeed();
    }
}
```

## API Reference

### Result Creation Methods

#### Success Methods

```csharp
// Basic success
Result.Succeed()
Result<T>.Succeed(T value)
CollectionResult<T>.Succeed(T[] items, int pageNumber, int pageSize, int totalItems)

// From operations
Result.From(Action action)
Result<T>.From(Func<T> func)
Result<T>.From(Task<T> task)

// Try pattern with exception catching
Result.Try(Action action)
Result<T>.Try(Func<T> func)
```

#### Failure Methods

```csharp
// Basic failures
Result.Fail()
Result.Fail(string title)
Result.Fail(string title, string detail)
Result.Fail(Problem problem)
Result.Fail(Exception exception)

// HTTP status failures
Result.FailNotFound(string detail)
Result.FailUnauthorized(string detail)
Result.FailForbidden(string detail)

// Validation failures
Result.FailValidation(params (string field, string message)[] errors)
Result.Invalid(string message)
Result.Invalid(string field, string message)

// Enum-based failures
Result.Fail<TEnum>(TEnum errorCode) where TEnum : Enum
```

### Transformation Methods

```csharp
// Map: Transform the value
Result<int> ageResult = userResult.Map(user => user.Age);

// Bind: Chain operations that return Results
Result<Order> orderResult = userResult
    .Bind(user => GetUserCart(user.Id))
    .Bind(cart => CreateOrder(cart));

// Tap: Execute side effects
Result<User> result = userResult
    .Tap(user => _logger.LogInfo($"Processing user {user.Id}"))
    .Tap(user => _cache.Set(user.Id, user));
```

### Validation Methods

```csharp
// Ensure: Add validation
Result<User> validUser = userResult
    .Ensure(user => user.Age >= 18, Problem.Create("User must be 18+"))
    .Ensure(user => user.Email.Contains("@"), Problem.Create("Invalid email"));

// Where: Filter with predicate
Result<User> filtered = userResult
    .Where(user => user.IsActive, "User is not active");

// FailIf: Conditional failure
Result<Order> order = orderResult
    .FailIf(o => o.Total <= 0, "Order total must be positive");

// OkIf: Must satisfy condition
Result<Payment> payment = paymentResult
    .OkIf(p => p.IsAuthorized, "Payment not authorized");
```

## Contributing

Contributions are welcome! Fork the repository and submit a pull request.

### Development Setup

```bash
# Clone the repository
git clone https://github.com/managed-code-hub/Communication.git

# Build the solution
dotnet build

# Run tests
dotnet test

# Run benchmarks
dotnet run -c Release --project ManagedCode.Communication.Benchmark
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- **Issues**: [GitHub Issues](https://github.com/managed-code-hub/Communication/issues)
- **Source Code**: [GitHub Repository](https://github.com/managed-code-hub/Communication)

## Acknowledgments

- Inspired by F# and Rust Result types
- Railway-oriented programming concepts
- RFC 7807 Problem Details for HTTP APIs
- Built for seamless integration with Microsoft Orleans
- Optimized for ASP.NET Core applications
