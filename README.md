# ManagedCode.Communication

`Result<T>` makes an operation's success or failure explicit. Failures carry an RFC 7807 `Problem`;
long-running operations report typed progress through CQRS streams. The library also provides railway
composition, reliable command execution, and ASP.NET Core, SignalR, and Orleans adapters for .NET 10.

[![NuGet](https://img.shields.io/nuget/v/ManagedCode.Communication.svg)](https://www.nuget.org/packages/ManagedCode.Communication/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)

## Contents

- [Installation](#installation)
- [Quick start](#quick-start)
- [Results and problems](#results-and-problems)
- [Collections and pagination](#collections-and-pagination)
- [Railway composition](#railway-composition)
- [HTTP integration](#http-integration)
- [Commands and identity](#commands-and-identity)
- [Reliable command execution](#reliable-command-execution)
- [CQRS streaming](#cqrs-streaming)
- [Orleans integration](#orleans-integration)
- [Serialization](#serialization)
- [Logging and telemetry](#logging-and-telemetry)
- [Registration reference](#registration-reference)
- [Development and testing](#development-and-testing)

## Installation

Choose the package for the boundary you need. All packages target .NET 10 and use the same release version.

| Package | Public API | Dependencies within this library |
| --- | --- | --- |
| `ManagedCode.Communication` | Results, problems, collections, commands, execution, CQRS contracts and HTTP stream client, diagnostics | None |
| `ManagedCode.Communication.Extensions` | Railway operators, HTTP result clients, `IHttpClientFactory` resilience, OpenTelemetry registration helpers | Core |
| `ManagedCode.Communication.AspNetCore` | Minimal API and MVC filters, SignalR filter, SSE transport, host logging setup | Core and Extensions |
| `ManagedCode.Communication.Orleans` | Native serialization surrogates, grain-call filters, Orleans idempotency and distributed limiter adapters | Core and AspNetCore |

Core and Extensions do not depend on ASP.NET Core. Orleans currently brings the AspNetCore package transitively.
The core package uses Microsoft.Extensions caching/logging and System.Threading.RateLimiting; it does not
require the OpenTelemetry SDK. Extensions adds the OpenTelemetry hosting and HTTP client registration helpers.

For a web application:

```shell
dotnet add package ManagedCode.Communication.AspNetCore --version 10.3.4
```

For a worker, console, or browser client using railway operators:

```shell
dotnet add package ManagedCode.Communication.Extensions --version 10.3.4
```

For Orleans:

```shell
dotnet add package ManagedCode.Communication.Orleans --version 10.3.4
```

A core-only application can reference `ManagedCode.Communication` directly. Equivalent project reference:

```xml
<PackageReference Include="ManagedCode.Communication" Version="10.3.4" />
```

## Quick start

Results and problems work without dependency injection or logging registration:

```csharp
using ManagedCode.Communication;

static Result<int> Divide(int dividend, int divisor)
{
    if (divisor == 0)
        return Result.FailValidation(("divisor", "Must not be zero."));

    return Result<int>.Succeed(dividend / divisor);
}

var result = Divide(12, 3);
if (result.IsSuccess)
    Console.WriteLine(result.Value);
else
    Console.WriteLine(result.Problem.Detail);
```

Use results for expected failures that callers can handle. Inspect the returned result: C# does not require
you to handle it. Exceptions remain available for exceptional conditions; adapters can convert them into a
failed result at a chosen boundary.

## Results and problems

### Result types and factories

| Type | Carries |
| --- | --- |
| `Result` | Success or a failure without a success value |
| `Result<T>` | Success value of type `T`, or a failure |
| `CollectionResult<T>` | Collection plus optional pagination metadata, or a failure |
| `Problem` | `Type`, `Title`, `StatusCode`, `Detail`, `Instance`, and extension fields |

```csharp
Result saved = Result.Succeed();
Result<int> count = Result.Succeed(42);
Result<int> explicitCount = Result<int>.Succeed(42);
Result<int> implicitCount = 42;

Result missing = Result.FailNotFound("Order does not exist.");
Result<int> typedMissing = missing;
Result<int> unavailable = Problem.Create("Unavailable", "Try again later.", 503);
```

A failed untyped `Result` or a `Problem` can become a typed failure. A successful untyped `Result` has no
value to supply to `Result<T>`; converting it to a typed result produces a failure. Create typed successes
from their actual values.

`Result` and `Result<T>` are structs. Their state is fixed after construction, but referenced payloads and
`Problem` objects remain mutable. Avoid changing a shared problem after returning or publishing it.
`default(Result)` is not a successful result; use a factory to state the intended outcome.

The static factory interfaces (`IResultFactory<T>`, `IResultValueFactory<,>`, `ICommandFactory<T>`, and
`ICommandValueFactory<,>`) support the shared factory surface. Custom implementations should reuse these
contracts rather than duplicate every factory overload.

### Failure helpers

```csharp
var validation = Result.FailValidation(
    ("email", "Email is required."),
    ("name", "Name is required."));

var conflict = Result.FailInvalidState("Order is already paid.");
var unauthorized = Result.FailUnauthorized();
var forbidden = Result.FailForbidden();
var missing = Result.FailNotFound("Order does not exist.");
var serverFailure = Result.Fail("Order could not be saved", "The database is unavailable.");
```

Primitive helpers cover nulls, invalid arguments, and range failures. Validation and argument failures use
`400`, invalid state uses `409`, not found uses `404`, unauthorized uses `401`, forbidden uses `403`, and a
general server failure uses `500`. Use `Problem.Create(...)` when the domain needs a specific status or
machine-readable code:

```csharp
var problem = Problem.Create("Payment declined", "Use another payment method.", 422);
problem.ErrorCode = "payment_declined";
problem.Extensions["provider"] = "payments";
return Result<Receipt>.Fail(problem);
```

`Fail(exception)` and exception-catching `From`/`Try` helpers report the original exception immediately and
return problem data. They do not retain an exception or its stack trace inside the result. See
[Logging and telemetry](#logging-and-telemetry).

### Display messages

Use `ToDisplayMessage` to choose an application message by error code, with a default message when needed:

```csharp
using ManagedCode.Communication.Results.Extensions;

var messages = new Dictionary<string, string>
{
    ["payment_declined"] = "Please choose another payment method."
};
var message = result.ToDisplayMessage(messages, defaultMessage: "The operation could not be completed.");
```

Resolver delegates, dictionaries, key-value sequences, and tuple mappings are supported. Keep user-facing
messages separate from operational details that may contain information unsuitable for a public response.

## Collections and pagination

`PaginationRequest` stores `Skip`/`Take` and provides normalization, page conversion, clamping, and slicing.
`PaginationCommand` carries the request through the command contract.

```csharp
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Commands;

var options = new PaginationOptions(defaultPageSize: 25, maxPageSize: 100);
var request = PaginationRequest.FromPage(pageNumber: 2, pageSize: 25, options);

var products = await repository.ReadAsync(request.Skip, request.Take, cancellationToken);
var total = await repository.CountAsync(cancellationToken);
return CollectionResult<Product>.Succeed(products, request, total, options);
```

Supply the total number of items, not the number in the current page. Prefer normalized bounds before
reading data; `PaginationRequest.Create(skip, take, options)` normalizes incoming skip/take values, while
`ToSlice(totalItems, options)` returns the bounded offset and length for an in-memory collection.

## Railway composition

Install Extensions and import `ManagedCode.Communication.Extensions`:

```csharp
using ManagedCode.Communication;
using ManagedCode.Communication.Extensions;

var receipt = await LoadCartAsync(cartId)
    .EnsureAsync(cart => !cart.IsEmpty, Problem.Validation(("cart", "Must contain an item.")))
    .BindAsync(cart => ChargeAsync(cart.Total))
    .Map(payment => payment.Receipt)
    .TapAsync(receipt => logger.LogInformation("Issued {ReceiptId}", receipt.Id));
```

Success-path operators skip their work when the incoming result failed and preserve its problem. Recovery
operators run on failure. `Task` and `ValueTask` receivers preserve their async shape, including result,
collection, conversion, and execution helpers.

| Operator | Purpose |
| --- | --- |
| `Map` / `MapAsync` | Transform a successful value |
| `Bind` / `Then` | Run a step returning another result |
| `Tap` / `Do` | Run a side effect while preserving the result |
| `Ensure` | Turn a failed predicate into a problem |
| `Match` | Project success and failure into a chosen output |
| `Compensate` | Recover using the incoming problem |
| `Else` | Supply an alternative on failure |
| `Finally` | Run after either outcome |

`Map` accepts a synchronous mapper even on an async receiver; `MapAsync` accepts an asynchronous mapper.
The async variants support matching `Task` and `ValueTask` delegates.

### Aggregating results

Aggregation lives on `Result` in Core and needs no Extensions reference:

```csharp
var validation = Result.MergeAll(ValidateEmail(email), ValidateName(name));
var orders = Result.CombineAll(LoadOrder(firstId), LoadOrder(secondId));
```

| Factory | Behavior |
| --- | --- |
| `Merge` | Return the first failure, or success when every input succeeds |
| `MergeAll` | Aggregate every failure |
| `Combine` | Collect success values, or return the first failure |
| `CombineAll` | Collect success values, or aggregate every failure |

When every failure is validation-related, the aggregate merges validation fields. Mixed failures retain the
original problems under `Problem.Extensions["errors"]` rather than disguising them as validation failures.

## HTTP integration

### Minimal API

`WithCommunicationResults()` belongs to AspNetCore, in the `MinimalApi` namespace. Apply it to an endpoint
or a group:

```csharp
using ManagedCode.Communication;
using ManagedCode.Communication.AspNetCore.Extensions;
using ManagedCode.Communication.AspNetCore.MinimalApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCommunicationAspNetCore(); // connect failure logging to the host
var app = builder.Build();

app.MapGet("/orders/{id}", async (Guid id, IOrderService orders) => await orders.FindAsync(id))
    .WithCommunicationResults();

app.MapGroup("/orders")
    .WithCommunicationResults()
    .MapPost(string.Empty, async (CreateOrder request, IOrderService orders) =>
        await orders.CreateAsync(request));

app.Run();
```

The HTTP wire contract is a raw success payload or an RFC 7807 problem, not a serialized `Result<T>` envelope.
Minimal API successes map to `200 OK` with a value or `204 No Content` without one; failures use the problem's
status and fields. Native `Microsoft.AspNetCore.Http.IResult` responses pass through unchanged.

### MVC controllers

```csharp
using ManagedCode.Communication.AspNetCore.Extensions;

builder.Services.AddControllers();
builder.Services.AddCommunication(); // host logging plus MVC filters
app.MapControllers();
```

A controller can return `Result` or `Result<T>` directly. The filters convert results, model-validation
failures, and exceptions at the MVC boundary. To register only the filters, use
`AddControllers(options => options.AddCommunicationFilters())` or `services.AddCommunicationFilters()`;
choose one registration path.

`UseCommunication()` currently adds no middleware. It is not needed for these filters or Minimal API result
mapping. These registrations also do not install a global exception handler for arbitrary application code.

### HTTP result clients

The Extensions package reads the same raw-payload/RFC 7807 wire contract:

```csharp
using ManagedCode.Communication.Extensions.Http;

Result<OrderDto> order = await httpClient.SendForResultAsync<OrderDto>(
    () => new HttpRequestMessage(HttpMethod.Get, $"/orders/{orderId}"),
    cancellationToken);
```

Use the success-projection overload for files, empty/optional bodies, or another non-JSON response:

```csharp
var invoice = await httpClient.SendForResultAsync(
    () => new HttpRequestMessage(HttpMethod.Get, $"/orders/{orderId}/invoice"),
    static async (response, token) => await response.Content.ReadAsByteArrayAsync(token),
    cancellationToken);
```

The library still handles transport failures and RFC 7807 failures. Serialized result envelopes are rejected.
Plain-text remote errors remain supported. Connection failures produce `503` and client-side timeouts
produce `504`; explicit caller cancellation propagates `OperationCanceledException`.

For command-aware reliability, supply the command, a fresh-request factory, and an execution runtime:

```csharp
var response = await httpClient.SendForResultAsync<OrderDto, Command<OrderQuery>>(
    command,
    current => new HttpRequestMessage(HttpMethod.Get, $"/orders/{current.Value!.OrderId}"),
    execution,
    cancellationToken);
```

The factory must create a new message for each attempt and preserve the same business idempotency key.

### IHttpClientFactory resilience

```csharp
using ManagedCode.Communication.Extensions.Http;

services.AddHttpClient<CatalogClient>()
    .AddCommunicationResilienceHandler(options =>
    {
        options.Execution.Retry.MaxRetries = 3;
        options.Execution.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(1);
    });
```

This handler uses native command execution and returns the final `HttpResponseMessage`. It honors
`Retry-After` and partitions circuit state by request authority. Automatic replay is limited to content-free
`GET`, `HEAD`, `OPTIONS`, and `TRACE`. Unsafe methods and requests with content pass through once; use an
explicit command/request-factory overload when the application has established that replay is safe.

`ConfigureHttpClientDefaults(client => client.AddCommunicationResilienceHandler())` sets shared defaults.
`RemoveCommunicationResilienceHandler()` disables this handler for an individual client.

### SignalR

```csharp
using ManagedCode.Communication.AspNetCore.Extensions;

builder.Services.AddSignalR(options => options.AddCommunicationHubFilter());
```

The filter reports and converts exceptions for Result-returning hub methods. Streaming hub methods use
the CQRS stream contract described below.

## Commands and identity

`Command` and `Command<T>` implement `ICommand`. They carry the operation identity, logical command type,
UTC timestamp, optional correlation/causation identifiers, user/session identifiers, and metadata.

```csharp
using ManagedCode.Communication.Commands;

var command = Command.From("order.place", new PlaceOrder(cartId))
    .WithCorrelationId(correlationId)
    .WithCausationId(parentCommandId)
    .WithUserId(userId)
    .WithSessionId(sessionId)
    .WithMetadata(metadata => metadata.Priority = CommandPriority.High);
```

Factories generate a UUIDv7 `CommandId` and UTC timestamp. Supply the optional trailing `commandId` only
when preserving an externally supplied idempotency key or replaying an existing message. Keep that identity
stable across retries; a newly created command is a new operation.

```csharp
var replayed = Command<PlaceOrder>.From(payload, idempotencyKey);
```

Correlation, causation, trace, span, user, and session identifiers remain unset until the caller assigns them.
Use `WithTraceId` and `WithSpanId` to propagate an existing W3C activity context. `CommandMetadata` also
contains retry budget, timeout, priority, trace metadata, tags, and application extension fields.

Serialized actor identifiers are data, not proof of authority. Authorization, idempotency scopes, and limiter
identity selectors must read trusted application context.

## Reliable command execution

The native executor composes retry, cooperative timeouts, idempotency, circuit breaking, rate limiting, and
diagnostics around `ICommand`. It has no separate request envelope or external resilience-library dependency.

### Register and execute

```csharp
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Commands.Execution;
using ManagedCode.Communication.Commands.Extensions;

services.AddCommandExecution(options =>
{
    options.Retry.Enabled = true;
    options.Retry.MaxRetries = 3;
    options.Retry.Delay = TimeSpan.FromMilliseconds(200);
    options.Retry.BackoffType = RetryBackoffType.Exponential;
    options.Retry.UseJitter = true;
    options.Timeout.TotalTimeout = TimeSpan.FromSeconds(15);
    options.Timeout.AttemptTimeout = TimeSpan.FromSeconds(5);
});

var executor = serviceProvider.GetRequiredService<ICommandExecutor>();
var command = Command.From("catalog.refresh", new RefreshCatalog(sourceId));

Result<Catalog> wrapped = await executor.ExecuteValueAsync(
    command,
    (current, token) => catalogService.RefreshAsync(current.Value!, token),
    cancellationToken);

Result<Catalog> preserved = await executor.ExecuteResultAsync(
    command,
    (current, token) => catalogService.RefreshAsResultAsync(current.Value!, token),
    cancellationToken);
```

`ExecuteValueAsync` wraps raw values; `ExecuteResultAsync` preserves an existing result without nesting.
Task handlers produce Task results and ValueTask handlers produce ValueTask results. No-value handlers are
also supported. Without DI, use `CommandExecutionRuntime` with static `CommandExecutor.ExecuteAsync` or
`Result<T>.ExecuteAsync` entry points.

### Retry decisions and delays

`MaxRetries` counts retries after the first attempt: `3` allows up to four physical attempts. Execution stops
on success, a permanent failure, cancellation, total timeout, or exhausted budget.

| Source | Default retry decision |
| --- | --- |
| Failed result | Status `408`, `429`, `500`, `502`, `503`, or `504` |
| Exception | `TimeoutException`, `HttpRequestException`, or `IOException` |
| Caller cancellation | Never retry |
| Other failures | Do not retry unless an application predicate selects them |

Delay selection uses `DelayGenerator` first, then an authoritative HTTP/rate-limiter `Retry-After`, then
constant, linear, or exponential backoff. `MaxDelay` caps built-in backoff after jitter. Custom/hinted delays
are checked against `MaxRetryAfter`; a hint above that limit returns the current failure with
`retryAfterExceedsMaximum = true` instead of retrying before the hinted time.

`ShouldRetry` and `ShouldRetryException` customize the default decisions. `ShouldRetryAsync` replaces both
with a command-aware decision. `OnRetry` and `OnRetriesExhausted` observe retries; observer errors are logged
without replacing the handler's outcome. Predicate or delay-generator errors are infrastructure failures.

`CommandMetadata.MaxRetries` can lower the global budget. `RetryCount` carries retries consumed on previous
hops. Exhausted retryable failures expose `retriesExhausted` and `retryAttempts` in `Problem.Extensions`.
Disabling retry also disables its predicates, callbacks, and exhaustion metadata.

### Timeout and circuit breaker

Total timeout covers the whole execution; attempt timeout covers one physical attempt. Cancellation is
cooperative: handlers receive a cancellation token and are awaited even if they ignore it. This prevents a
still-running side effect from outliving its limiter permit or idempotency claim. A late handler completion
can therefore return a normal outcome after the timeout requested cancellation.

```csharp
services.AddCommandExecution(options =>
{
    options.CircuitBreaker.Enabled = true;
    options.CircuitBreaker.MinimumThroughput = 20;
    options.CircuitBreaker.FailureRatio = 0.5;
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
});
```

Use the circuit breaker's partition selector and failure predicates to match the dependency being protected.
A single idempotency owner holds the retry sequence; each attempt then passes through timeout, circuit
breaker, limiter, and handler. Limiter permits are reacquired for each attempt.

### Idempotency ownership and recovery

Register a store and provide trusted scope and immutable-request fingerprint selectors:

```csharp
services.AddCommandIdempotency(); // process-local memory store
services.AddCommandExecution(options =>
{
    options.Idempotency.ScopeSelector = _ => tenantContext.TenantId;
    options.Idempotency.FingerprintSelector = command => requestHasher.Hash(command);
});
```

A custom `ICommandIdempotencyStore` can be registered with `AddCommandIdempotency<TStore>()`. The atomic
record includes scope, operation identity, fingerprint, result contract, fenced owner, and terminal outcome.
Running claims are renewed while the handler is active; matching duplicate operations can reuse the outcome.

Cancellation, a crash window, a lost finalization response, or an expired running claim can leave an
`Indeterminate` outcome. The executor does not automatically repeat it. After independently establishing the
real outcome, call `TryResolveIndeterminateAsync`; reset with `TryResetIndeterminateAsync` only when replay
is known to be safe.

For external effects, propagate the same business idempotency key to the provider or coordinate an outbox/inbox.
Retry and a local store cannot guarantee exactly-once effects across an uncoordinated external system.

Core registration adds no cleanup hosted service. Stores implementing `ICommandIdempotencyMaintenance`
can be maintained by the application. AspNetCore provides a hosted cleanup overload for such stores.

### Local rate limiting

```csharp
var limiter = PartitionedCommandRateLimiter.CreateFixedWindow(
    command => trustedTenantAccessor.TenantId,
    permitLimit: 100,
    window: TimeSpan.FromMinutes(1),
    queueLimit: 20);

services.AddCommandRateLimiter(limiter);
```

`CreateConcurrency`, `CreateSlidingWindow`, and `CreateTokenBucket` provide the other local algorithms.
Factories support a permit-count selector. Factory-created limiters are owned/disposed by the adapter;
wrapping an application-owned `PartitionedRateLimiter<ICommand>` does not transfer ownership by default.
Lease cleanup failures do not rerun a handler that already succeeded. Distributed limiting uses the Orleans
adapter in [Orleans integration](#orleans-integration).

## CQRS streaming

A stream is `IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>>`. The contract contains optional progress
and one terminal result; the HTTP transport renders it as Server-Sent Events.

| Kind | Payload | Meaning |
| --- | --- | --- |
| `Started` | Optional `ProgressResult` | Execution has started |
| `Progress` | `ProgressResult` | An intermediate update |
| `Completed` | Successful `Final` | Terminal success |
| `Failed` | Failed `Final` containing a problem | Terminal failure |

`Sequence`, `EventId`, and `Message` carry ordering and presentation data. SSE event names are `cqrs-started`,
`cqrs-progress`, `cqrs-completed`, and `cqrs-failed`; `Kind` is written as a string in the JSON payload.

### Write and serve a stream

```csharp
using ManagedCode.Communication;
using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.AspNetCore.Extensions;

builder.Services.AddCommunicationCqrs();

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

`CqrsStream.Create` numbers chunks and converts the handler's returned result into a terminal chunk.
It converts nonfatal producer exceptions into failures. Fatal runtime exceptions propagate, including when
nested in an `AggregateException`. Original exceptions are reported immediately; public chunks contain
problem data rather than exception objects or stack traces.

For a handwritten iterator, use the `Started`, `Progress`, `Completed`, and `Failed` chunk factories, then
`CqrsStream.Normalize` when publishing through a transport that does not normalize automatically. The SSE
adapter normalizes by default: missing terminal results become `CqrsStreamProblems.IncompleteStream`, and
nonfatal enumeration failures become failed chunks. An exception before a handler returns its stream belongs
to the host's ordinary exception handling.

Early consumer disposal cancels and joins the producer, including its `finally` blocks, before disposal
completes. Cancellation callback failures do not skip that join. Handlers must cooperate with cancellation
to allow disposal to complete promptly.

### Read progress and the result

The HTTP stream client lives in Core and requires no ASP.NET Core reference:

```csharp
using ManagedCode.Communication.CQRS;

Result<ImportReport> report = await httpClient
    .GetForCqrsStreamAsync<ImportProgress, ImportReport>("/import")
    .ToResultAsync(progress => Console.WriteLine($"{progress.Percent}%"), cancellationToken);
```

Use the two-parameter callback for awaited work, so an async lambda cannot bind to an `Action<TProgress>`:

```csharp
var report = await stream.ToResultAsync(async (progress, token) =>
    await SaveProgressAsync(progress, token), cancellationToken);
```

The callback finishes before the next chunk is read. `ToResultAsync` handles terminal failures, interrupted
transports, and streams that end without a terminal chunk. Explicit caller cancellation propagates
`OperationCanceledException`, including cancellation while an incomplete source is finishing.

| API | Retains |
| --- | --- |
| `ToResultAsync([onProgress])` | Terminal result, with optional live progress callback |
| `ToOutcomeAsync()` | Result, progress values, and all chunks |
| `AsCqrsStream()` | A normalized stream for continued chunk-by-chunk enumeration |
| `ToChunkListAsync()` | Exactly the received chunks; enumeration faults still propagate |
| `chunks.ToStreamResult()` | Interprets an already collected sequence as its final result |

`ToOutcomeAsync` collects the stream in memory; choose `ToResultAsync` for long streams when history is not
needed. The returned `Task<Result<TResult>>` can continue through async railway operators.

### Server options and mixed responses

`AddCommunicationCqrs` registers shared server options and the MVC stream filter.
`AddControllers(options => options.AddCommunicationCqrsFilters())` adds the MVC filter directly instead.
Minimal API endpoints use `WithCommunicationCqrsResults`, with optional per-endpoint options:

```csharp
using ManagedCode.Communication.AspNetCore;

app.MapGet("/import", Handler)
    .WithCommunicationCqrsResults(new CqrsStreamServerOptions
    {
        AssignSequenceNumbers = true,
        EnsureTerminalChunk = true
    });
```

Both options default to `true`. When a handler dynamically chooses a stream or another HTTP response, keep
its return type as `Microsoft.AspNetCore.Http.IResult` and use the library-owned transport:

```csharp
return CqrsStreamHttpResults.ServerSentEvents(updates);
```

Sequence IDs support an application's replay protocol. Communication does not persist streams, retain a
replay history, or automatically resume from `Last-Event-ID`; durable execution/reconnection belongs to the
application.

### Client bounds and malformed input

```csharp
var options = new CqrsStreamClientOptions
{
    MaximumFrameBytes = 8 * 1024 * 1024,
    MaximumFailureBodyBytes = 32 * 1024,
    MaximumStreamBytes = 128L * 1024 * 1024,
    MalformedChunkBehavior = CqrsMalformedChunkBehavior.EmitFailedChunk
};
```

Defaults are a 16 MiB physical-frame limit and 64 KiB non-success-body limit; hard ceilings are 64 MiB and
1 MiB respectively. `MaximumStreamBytes` is optional and counts the entire successful body, including SSE
comments and delimiters. Configure an operation-appropriate total budget for bounded workloads.

Malformed-chunk handling supports `EmitFailedChunk` (default), `Skip`, or `Throw`. Bounds violations produce
stable transport failures. Valid bounded RFC 7807 failures retain their fields; invalid, oversized, or
plain-text failure bodies expose status and a safe detail without copying the raw body into the result.

### SignalR and Orleans streams

All stream helpers accept `IAsyncEnumerable`, so the same reader works with SignalR, Orleans, or another
transport implementing that contract:

```csharp
var report = await hubConnection
    .StreamAsync<CqrsStreamChunk<ImportProgress, ImportReport>>("Import", cancellationToken)
    .ToResultAsync(progress => Console.WriteLine(progress.Percent), cancellationToken);
```

A SignalR server can return `CqrsStream.Normalize(ImportAsync(token), cancellationToken: token)`.
A stream produced by `Create` already supplies the contract. `ToResultAsync`, `ToOutcomeAsync`, and
`AsCqrsStream` also normalize incoming streams; fatal runtime failures and caller cancellation retain their
exception semantics.

CQRS creation, normalization, enumeration, disposal, and progress callbacks preserve the caller's scheduler
and synchronization context. Inside an Orleans grain, awaited progress callbacks therefore remain on that
grain's scheduler:

```csharp
var report = await otherGrain.StreamAsync().ToResultAsync(
    async (progress, token) => await SaveProgressAsync(progress, token));
```

## Orleans integration

```csharp
using ManagedCode.Communication.Orleans.Extensions;

siloBuilder.UseOrleansCommunication();
clientBuilder.UseOrleansCommunication();
```

These methods add incoming/outgoing grain-call filters and limiter options. Communication's native typed
surrogates are registered through Orleans-generated metadata. This setup does not enable command execution,
choose a grain storage serializer, or install JSON grain-storage converters.

### Distributed command execution

Enable the execution adapter explicitly and configure named `commandStore` grain storage on the silo:

```csharp
using ManagedCode.Communication.Orleans.Extensions;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;

siloBuilder
    .AddMemoryGrainStorage("commandStore") // use a durable provider for production recovery
    .UseOrleansCommunication()
    .UseOrleansCommandExecution(
        execution =>
        {
            execution.Idempotency.ScopeSelector = _ => tenantContext.TenantId;
            execution.Idempotency.FingerprintSelector = command => requestHasher.Hash(command);
        },
        limiter =>
        {
            limiter.PolicyName = static _ => "commands";
            limiter.TenantId = _ => tenantContext.TenantId;
            limiter.UserId = _ => tenantContext.UserId;
        });

siloBuilder.Services.AddFixedWindowRateLimiterOptions("tenant-commands", options =>
{
    options.PermitLimit = 1_000;
    options.Window = TimeSpan.FromMinutes(1);
    options.QueueLimit = 100;
});
siloBuilder.Services.AddOrleansRequestRateLimiting(options =>
    options.AddTenant("tenant-commands", required: true));
```

A client using the distributed executor calls `clientBuilder.UseOrleansCommandExecution(...)` as well.
`ManagedCode.Orleans.RateLimiting` owns distributed algorithms and durable leases; Communication owns the
`ICommand`/result adapter, execution orchestration, and diagnostics. User, tenant, role, resource, IP, and
metadata selectors default to unset values; supply selectors from trusted execution context.

## Serialization

### Native Orleans serialization

`ManagedCode.Communication.Orleans` uses Orleans-generated serialization with typed surrogates for results,
problems, commands, pagination, collections, and CQRS chunks. Stable field IDs preserve success flags, typed
values (including failure payloads), problem fields, and command metadata.

Your application payloads crossing grain boundaries need their own generated serializers:

```csharp
using Orleans;
using ManagedCode.Communication.CQRS;

[GenerateSerializer]
public sealed record ImportProgress([property: Id(0)] int Percent);

[GenerateSerializer]
public sealed record ImportReport([property: Id(0)] int Imported);

public interface IImportGrain : IGrainWithStringKey
{
    IAsyncEnumerable<CqrsStreamChunk<ImportProgress, ImportReport>> ImportAsync();
}
```

Communication does not register a JSON grain-storage adapter. The application owns its storage provider and
serializer. Native Orleans storage uses the same generated serialization as grain communication:

```csharp
using Orleans.Storage;

var storageSerializer = new OrleansGrainStorageSerializer(
    serviceProvider.GetRequiredService<Orleans.Serialization.Serializer>());
var restored = storageSerializer.Deserialize<Result<int>>(
    storageSerializer.Serialize(Result<int>.Succeed(0)));
// restored.IsSuccess is true; restored.Value is 0.
```

Existing persisted data must match the application-selected storage format. Changing serializer configuration
does not migrate old records. Any cleanup or migration must target exact application-owned state.

### System.Text.Json for application and HTTP JSON

Communication's JSON implementation uses `System.Text.Json`. `Result`, `Result<T>`, and `Problem` provide
their JSON converters through type attributes:

```csharp
using System.Text.Json;

var json = JsonSerializer.Serialize(Result<int>.Succeed(0));
var restored = JsonSerializer.Deserialize<Result<int>>(json);
```

This application serialization is separate from the HTTP result filter, which emits a raw success payload or
problem response, and from Orleans native serialization. Do not install an application JSON codec merely to
transport Communication types through Orleans.

CQRS JSON defaults are available as the immutable `CqrsStreamSerialization.Default`. For source-generated
payload contracts, use `CqrsStreamSerialization.WithPayloadContext(MyJsonContext.Default)` in
`CqrsStreamClientOptions.JsonSerializerOptions`; it combines the payload context with contracts for the
transport types. Configure matching payload contexts through ASP.NET Core's HTTP JSON options on the server.

## Logging and telemetry

### Connect the application logger

Core results work without registration. Without a configured logger, the internal logger emits no output.
For a web application, `AddCommunicationAspNetCore()` connects the host logger at startup; `AddCommunication()`
also adds MVC filters. A console application can configure it directly:

```csharp
using ManagedCode.Communication.Logging;

using var loggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
CommunicationLogger.Configure(loggerFactory);
```

The core `services.ConfigureCommunication(loggerFactory)` overload also accepts an explicit factory.
There is no parameterless core `ConfigureCommunication()` registration.

Failures without an exception log at Warning with problem fields. Exception conversions log at Error with
the original exception and throw-site stack trace. Automatic factory diagnostics are deduplicated per
`Problem` object: wrapping the same instance does not report it as a new occurrence. New problems represent
new occurrences; successes and reading a result do not emit automatic failure diagnostics.

### OpenTelemetry and Aspire

Install Extensions for registration helpers:

```csharp
using ManagedCode.Communication.Extensions.Telemetry;

builder.AddCommunicationTelemetry();
```

This connects the host logger and subscribes to Communication traces and metrics. In Aspire, call it in each
service alongside that service's existing `AddServiceDefaults()`. Calling it only in AppHost does not collect
signals from child processes. The application still chooses its exporters, sampling, and export intervals.

To add instrumentation to an existing OpenTelemetry setup:

```csharp
builder.Services.AddOpenTelemetry().WithCommunication();
```

Or subscribe to individual providers:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddCommunicationInstrumentation())
    .WithMetrics(metrics => metrics.AddCommunicationInstrumentation());
```

Individual provider subscriptions do not configure logging. Without the helpers, subscribe directly to
`CommunicationTelemetry.SourceName` through `AddSource` and `AddMeter`.

| Signal | Purpose |
| --- | --- |
| `communication.result.failure` activity event | Automatic factory failure attached to the current activity |
| `communication.result.created.failures` counter | Distinct factory failures, including attempts later recovered |
| `communication.result.failures` counter | Explicit boundary failures and final failed command executions |
| `communication.exceptions` counter | Exceptions converted into problems |
| Command execution instruments | Attempt/total duration, retry, timeout, idempotency, circuit, limiter, queue, and outcome signals |

Automatic factory events preserve the parent operation's status and actual HTTP response code, since a caller
can recover from a failure. Explicit boundary reports and final failed operations can mark the activity as
Error. Factory failures do not create a synthetic root/dependency trace. Valid serialized W3C trace/span
identifiers can become command execution's remote parent; actor IDs are not metric tags.

### Explicit reporting and exception mapping

```csharp
using ManagedCode.Communication.Telemetry;

var result = LoadOrder(orderId).Report(logger);
var order = await CommunicationDiagnostics.TrackAsync(
    "orders.place", () => orderService.PlaceAsync(cart), logger);
```

`Track`/`TrackAsync` convert thrown exceptions into failed results and report the original exception.
`Problem.Create(exception)` alone builds problem data; report at the catch site if using it directly:

```csharp
catch (Exception exception)
{
    var problem = Problem.Create(exception);
    CommunicationDiagnostics.ReportFailure(logger, problem, exception);
    return Result<Order>.Fail(problem);
}
```

ASP.NET Core, SignalR, and Orleans filters report exceptions they convert. Runtime results and problems never
retain exceptions for later logging.

Default exception-to-status mapping is a heuristic. Server defects such as `InvalidOperationException`,
`NotSupportedException`, and `NullReferenceException` map to `500`; domain-specific mappings can be registered
once at startup:

```csharp
using ManagedCode.Communication.Helpers;

ExceptionStatusCodeMap.Map<OrderNotFoundException>(HttpStatusCode.NotFound);
```

Mapping walks the exception type hierarchy and uses the most specific registration. Packages include portable
PDBs and symbol packages; debug symbols do not enable telemetry providers or exporters.

## Registration reference

Use only the registrations required by your application. Plain result factories, railway operators, CQRS
contracts, and HTTP clients do not require a host registration.

| Boundary | Entry point | Effect |
| --- | --- | --- |
| Core logging | `ConfigureCommunication(loggerFactory)` | Connect an explicit logger factory |
| Commands | `AddCommandExecution(...)` | Register native executor/runtime and options |
| Idempotency | `AddCommandIdempotency()` / `<TStore>()` | Register local/custom atomic store; no Core hosted cleanup |
| Limiting | `AddCommandRateLimiter(limiter)` | Register a selected command limiter |
| ASP.NET Core logging | `AddCommunicationAspNetCore()` | Connect host logging at startup |
| MVC | `AddCommunication()` | Host logging and MVC result/validation/exception filters |
| MVC filters only | `AddCommunicationFilters()` | Register filters without host logging |
| Minimal API | `WithCommunicationResults()` | Map results to raw success/RFC 7807 HTTP responses |
| CQRS server | `AddCommunicationCqrs(...)` | Shared stream options and MVC stream filter |
| CQRS Minimal API | `WithCommunicationCqrsResults(...)` | Render a stream as SSE |
| SignalR | `AddCommunicationHubFilter()` on hub options | Convert Result-returning hub exceptions |
| Orleans | `UseOrleansCommunication()` on silo/client | Grain-call filters and limiter options; native generated surrogates |
| Orleans execution | `UseOrleansCommandExecution(...)` | Distributed idempotency/limiting and executor; silo requires `commandStore` |
| HTTP resilience | `AddCommunicationResilienceHandler(...)` | Native execution around safe HTTP replays |
| Telemetry | `AddCommunicationTelemetry()` | Host logger and trace/metric subscriptions |

## Development and testing

The repository uses TUnit, Microsoft.Testing.Platform, and Shouldly. `global.json` selects native
Microsoft.Testing.Platform mode, so use `--project` or `--solution`, with runner arguments passed directly.

```shell
dotnet restore ManagedCode.Communication.slnx
dotnet build -c Release ManagedCode.Communication.slnx
dotnet test --project ManagedCode.Communication.Tests/ManagedCode.Communication.Tests.csproj --configuration Release --no-build --output Normal
```

For the complete solution, the CI-equivalent command is:

```shell
dotnet test --solution ManagedCode.Communication.slnx --configuration Release --no-build --output Normal
```

Collect Cobertura coverage:

```shell
dotnet test --solution ManagedCode.Communication.slnx --configuration Release --coverage --coverage-output coverage.cobertura.xml --coverage-output-format cobertura --output Normal
```

Run the existing performance benchmarks when evaluating allocation or throughput changes:

```shell
dotnet run -c Release --project ManagedCode.Communication.Benchmark
```

Results are structs and the JSON converters handle the result envelope directly; payload shape, collection
materialization, logging, and the transport still affect allocations. Use benchmark evidence for a specific
path rather than assuming an entire operation allocates nothing.

New APIs should have success/failure regressions at their public boundary. Orleans tests cover native
round-trips, HTTP tests cover raw success/RFC 7807 contracts, and CQRS tests cover terminal outcomes,
cancellation, disposal, malformed input, and bounds. Shared assertion helpers are in
`ManagedCode.Communication.Tests/TestHelpers`.

Package version is owned by `Version` and `PackageVersion` in `Directory.Build.props`. The Release workflow
on `main` builds, tests, packs, publishes to NuGet, and creates the GitHub release/tag. A completed build does
not establish publication: verify the intended package is downloadable from NuGet.

See [AGENTS.md](AGENTS.md) for repository conventions, [GitHub Issues](https://github.com/managed-code-hub/Communication/issues)
for support, and [the source repository](https://github.com/managed-code-hub/Communication) for contributions.
The project is licensed under [MIT](LICENSE).
