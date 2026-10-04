# ADR-001: Propagate fatal CQRS stream exceptions

Status: Accepted

## Context

`CqrsStream.Create` and `CqrsStream.Normalize` convert caught exceptions into failed CQRS chunks. This is useful for ordinary operation failures, but treating fatal runtime exceptions as recoverable stream outcomes can hide a process-level failure and can copy exception metadata into a `Problem`.

## Decision

The producer and normalizer must propagate `OutOfMemoryException`, `StackOverflowException`, and `AccessViolationException` instead of converting them through `FromException`. Preserve conversion for ordinary exceptions, caller cancellation behavior, native channel capacity and backpressure, and source-enumerator disposal.

## Implementation contract

1. Change only the exception-conversion boundaries in `ManagedCode.Communication/CQRS/CqrsStream.cs` and `ManagedCode.Communication/CQRS/CqrsStreamNormalizer.cs`.
2. Add TUnit tests in `ManagedCode.Communication.Tests/CQRS/CqrsFatalExceptionTests.cs` using preconstructed sentinel instances for all three fatal types. Assert reference identity on propagation, no failed chunk, and disposal after a fatal source enumeration fault. Keep existing ordinary-exception, cancellation, and telemetry tests unchanged.
3. Update the `CQRSStreaming` requirement and README wording so only nonfatal exceptions are described as converted to failed chunks.
4. Verify the focused cases, then run the owning repository's full restore, Release build, format verification, and full TUnit suite. Package release follows the repository's canonical `release.yml` workflow.

## Compatibility and delivery

No public API, wire schema, serializer, or package dependency changes. The next patch is `10.2.8`, set through both existing version properties in `Directory.Build.props`. Publish through the existing main-branch release workflow, verify the exact-source CI and package publication, and confirm all intended package artifacts are available before consuming this version elsewhere.
