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

# Repository Guidelines

## Project Structure & Module Organization
The solution `ManagedCode.Communication.slnx` ties together the core library (`ManagedCode.Communication`), ASP.NET Core adapters, Orleans integrations, performance benchmarks, and the consolidated test suite (`ManagedCode.Communication.Tests`). Tests mirror the runtime namespaces—look for feature-specific folders such as `Results`, `Commands`, and `AspNetCore`—so keep new specs alongside the code they exercise. Shared assets live at the repository root (`README.md`, `logo.png`) and are packaged automatically through `Directory.Build.props`.

## Build, Test, and Development Commands
- `dotnet restore ManagedCode.Communication.slnx` – restore all project dependencies.
- `dotnet build -c Release ManagedCode.Communication.slnx` – compile every project with warnings treated as errors.
- `dotnet test ManagedCode.Communication.Tests/ManagedCode.Communication.Tests.csproj` – run the xUnit suite; produces `*.trx` logs under `ManagedCode.Communication.Tests`.
- `dotnet test ManagedCode.Communication.Tests/ManagedCode.Communication.Tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=lcov` – refresh `coverage.info` via coverlet.
- `dotnet run -c Release --project ManagedCode.Communication.Benchmark` – execute benchmark scenarios before performance-sensitive changes.

## Coding Style & Naming Conventions
Formatting is driven by the root `.editorconfig`: spaces only, 4-space indent for C#, CRLF endings for code, braces on new lines, and explicit types except when the type is obvious. The repo builds with C# 13, nullable reference types enabled, and analyzers elevated to errors—leave no compiler warnings behind. Stick to domain-centric names (e.g., `ResultExtensionsTests`) and prefer PascalCase for members and const fields per the configured naming rules.

## Testing Guidelines
All automated tests use xUnit with FluentAssertions and Microsoft test hosts; follow the existing spec style (`MethodUnderTest_WithScenario_ShouldOutcome`). New fixtures belong in the matching feature folder and should assert both success and failure branches for Result types. Maintain the default coverage settings supplied by `coverlet.collector`; update snapshots or helper builders under `TestHelpers` when shared setup changes.

## Commit & Pull Request Guidelines
Commits in this repository stay short, imperative, and often reference the related issue or PR number (e.g., `Add FailBadRequest methods (#30)`). Mirror that tone, limit each commit to a coherent change, and include updates to docs or benchmarks when behavior shifts. Pull requests should summarize intent, list breaking changes, attach relevant `dotnet test` outputs or coverage deltas, and link tracked issues. Screenshots or sample payloads are welcome for HTTP-facing work.
