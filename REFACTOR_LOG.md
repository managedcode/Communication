# Refactor Progress Log

## Tasks
- [x] Audit existing static factory methods on `Result`, `Result<T>`, and `CollectionResult<T>`.
- [x] Introduce shared helper infrastructure to centralise problem/result creation logic (ResultFactory & CollectionResultFactory).
- [x] Move invocation helpers (`From`, `Try`, etc.) into interface-based extension classes without breaking `Result.Succeed()` API.
- [x] Refactor railway extensions to operate on interfaces and provide consistent naming (`Then`, `Merge`, etc.).
- [x] Update collection result helpers and ensure task/value-task shims reuse the new extensions.
- [ ] Adjust command helpers if needed for symmetry.
- [x] Update unit tests and README examples to use the new extension methods where applicable.
- [x] Run `dotnet test` to verify.
- [x] Migrate test assertions from FluentAssertions to Shouldly and remove the old dependency.
- [x] Re-run `dotnet test` after assertion migration.
- [ ] Review architecture for remaining inconsistencies or confusing patterns post-refactor.

- Design Proposal
  - Introduce static factory interfaces to centralise creation logic and keep `Result`/`Result<T>` implementations minimal.
  - Create `Extensions/Results` namespace hosting execution utilities (`ToResult`, `TryAsResult`, railway combinators) targeting `IResult`/`IResult<T>`.
  - Mirror the pattern for collection and command helpers to ensure symmetry.

## Notes
- Static factories identified across:
  - `ManagedCode.Communication/Result/*` (`Fail`, `Succeed`, `Try`, `From`, `Invalid`, etc.).
  - `ManagedCode.Communication/ResultT/*` (mirrors `Result` plus generic overloads).
  - `ManagedCode.Communication/CollectionResultT/*` (array/collection handling).
  - Command helpers (`Command.Create`, `Command<T>.Create`, `Command.From`).
- Target C# 13 features for interface-based reuse where possible.
- Preserve public APIs like `Result.Succeed()` while delegating implementation to shared helpers.
- Keep refactor incremental to avoid breaking the entire suite in one step.
- Added dedicated Shouldly-based coverage for `Result.From`/`Result<T>.From`, Task/ValueTask wrappers, command metadata fluent APIs, and collection async helpers; core library line coverage now sits at ~80%.
