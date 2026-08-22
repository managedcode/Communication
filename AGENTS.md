# Conversations
any resulting updates to agents.md should go under the section "## Rules to follow"
When you see a convincing argument from me on how to solve or do something. add a summary for this in agents.md. so you learn what I want over time.
If I say any of the following point, you do this: add the context to agents.md, and associate this with a specific type of task.
if I say "never do x" in some way.
if I say "always do x" in some way.
if I say "the process is x" in some way.
If I tell you to remember something, you do the same, update


## Rules to follow
always check all test are passed.
- Use TUnit with Microsoft.Testing.Platform for all .NET tests; do not add or retain xUnit packages, attributes, fixtures, configuration, or documentation.
- Prefer static interface members for result/command factories to centralize shared overloads and avoid duplication across result-like types.
- Use `DateTime.UtcNow` (never `DateTimeOffset`) for all timestamps; we assume every stored time is in UTC.
- For `MergeAll`/`CombineAll` scenarios with mixed failures, keep aggregated behavior and preserve original errors in `Problem.Extensions` (do not flatten everything into validation-only output).
- In display-message APIs, use the parameter name `defaultMessage` (avoid the word `fallback` in public API naming).
- For user-facing helper APIs, prefer multiple ergonomic overloads (delegate + dictionary + tuple mappings) so callers can choose the most convenient style.
- Do not add redundant `result.Problem is not null` checks after `result.IsFailed`; rely on result nullability contract/attributes and only use null-forgiving where needed.
- Keep documentation aligned with the current major version (for this repository now: version 10); do not add cross-major migration sections unless explicitly requested.
- When behavior changes in Result/Problem flows, include a clear README update with concrete usage examples.
- When a framework adapter catches an exception and converts it into a failed `Result`, it must log the original exception object before returning the failure so distributed traces keep the real stack trace.
- Keep one forward-only HTTP wire contract: `WithCommunicationResults()` emits a raw success payload or RFC 7807 failure, and `HttpClient.SendForResultAsync<T>()` consumes exactly that shape. Do not retain serialized `Result<T>` envelope compatibility.
- For file, optional, or other non-JSON success bodies, use the `SendForResultAsync` success-projection overload so the library still owns transport and RFC 7807 failures; do not add application-specific failure parsers.
- When one Minimal API route dynamically selects between a CQRS stream and another HTTP result, use `CqrsStreamHttpResults.ServerSentEvents(stream)`; do not return `object` or implement a second SSE writer in the application.
- For execution reliability, build and maintain Communication's own execution framework; do not depend on Polly or any Polly package at runtime, through DI, or in tests.
- Design resilience as native `ICommand` execution, integrating `Result`/`Problem`, idempotency, correlation/trace metadata, and existing diagnostics instead of exposing a standalone Polly-shaped subsystem.
- The Communication execution model must emit OpenTelemetry-compatible traces and metrics by default, include an Orleans adapter, and include built-in rate limiting; prefer .NET `System.Threading.RateLimiting` for local partitions and use an Orleans-backed adapter when cluster-wide coordination is required.
- Do not introduce a parallel `IRequest`/`Request<T>` execution envelope for resilience; `ICommand` is the operation contract, and HTTP or Orleans integrations adapt to the active command execution.
- Keep clear reliability capability names such as `Retry`, `Timeout`, and `RateLimiter`; do not rename them merely to differ from Polly.
- Avoid vague public resilience abstractions such as `ExecutionFeature` and `FailureClassifier`; expose capability-specific options and `ShouldRetry` decisions instead.
- When implementing command reliability, use current Polly source as an algorithmic and test reference for concurrency, cancellation, time, backoff, telemetry, and disposal guarantees, but do not copy its public pipeline architecture or add a Polly dependency.
- For `/plan` requests, perform research and produce the implementation plan without changing feature code, project references, packages, or tests.
- Use `ManagedCode.Orleans.RateLimiting` as the distributed Orleans rate-limiting backend instead of duplicating its grain algorithms; Communication owns the `ICommand`, `Result`/`Problem`, telemetry, and adapter layer.
- Command execution APIs must support handlers returning raw values/tasks by wrapping them into `Result`, and handlers already returning `Result`/`Result<T>` by preserving them unchanged; provide symmetric `Task` and `ValueTask` overloads.
- Async result APIs must preserve the caller's async shape: a `Task` input or handler returns `Task`, while a `ValueTask` input or handler returns `ValueTask`; keep this symmetric across `Result`, collection-result, conversion, and command-execution helpers.
- Do not introduce inline string literals in implementation or test code; declare every string value as a named constant under its canonical owner and reuse an existing owner constant before adding a new one.

# Repository Guidelines

## Project Structure & Module Organization
The solution `ManagedCode.Communication.slnx` ties together the core library (`ManagedCode.Communication`), ASP.NET Core adapters, Orleans integrations, performance benchmarks, and the consolidated test suite (`ManagedCode.Communication.Tests`). Tests mirror the runtime namespaces—look for feature-specific folders such as `Results`, `Commands`, and `AspNetCore`—so keep new specs alongside the code they exercise. Shared assets live at the repository root (`README.md`, `logo.png`) and are packaged automatically through `Directory.Build.props`.

## Build, Test, and Development Commands
- `dotnet restore ManagedCode.Communication.slnx` – restore all project dependencies.
- `dotnet build -c Release ManagedCode.Communication.slnx` – compile every project with warnings treated as errors.
- `dotnet test --project ManagedCode.Communication.Tests/ManagedCode.Communication.Tests.csproj` – run the complete TUnit suite on Microsoft.Testing.Platform.
- `dotnet test --project ManagedCode.Communication.Tests/ManagedCode.Communication.Tests.csproj --coverage --coverage-output-format cobertura` – collect coverage through Microsoft.Testing.Platform.
- `dotnet run -c Release --project ManagedCode.Communication.Benchmark` – execute benchmark scenarios before performance-sensitive changes.

## Coding Style & Naming Conventions
Formatting is driven by the root `.editorconfig`: spaces only, 4-space indent for C#, CRLF endings for code, braces on new lines, and explicit types except when the type is obvious. The repo builds with C# 13, nullable reference types enabled, and analyzers elevated to errors—leave no compiler warnings behind. Stick to domain-centric names (e.g., `ResultExtensionsTests`) and prefer PascalCase for members and const fields per the configured naming rules.

## Testing Guidelines
All automated tests use TUnit with Shouldly on Microsoft.Testing.Platform; follow the existing spec style (`MethodUnderTest_WithScenario_ShouldOutcome`). New fixtures belong in the matching feature folder and should assert both success and failure branches for Result types. Use TUnit data sources and lifecycle hooks, collect coverage through `Microsoft.Testing.Extensions.CodeCoverage`, and update shared helpers under `TestHelpers` when shared setup changes.

## Commit & Pull Request Guidelines
Commits in this repository stay short, imperative, and often reference the related issue or PR number (e.g., `Add FailBadRequest methods (#30)`). Mirror that tone, limit each commit to a coherent change, and include updates to docs or benchmarks when behavior shifts. Pull requests should summarize intent, list breaking changes, attach relevant `dotnet test` outputs or coverage deltas, and link tracked issues. Screenshots or sample payloads are welcome for HTTP-facing work.
