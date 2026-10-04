# ADR-001: Propagate fatal CQRS stream exceptions

Status: Accepted

## Context

`CqrsStream.Create` and `CqrsStream.Normalize` convert caught exceptions into failed CQRS chunks. This is useful for ordinary operation failures, but treating fatal runtime exceptions as recoverable stream outcomes can hide a process-level failure and can copy exception metadata into a `Problem`.

## Decision

The producer and normalizer must propagate direct `OutOfMemoryException`, `StackOverflowException`, and `AccessViolationException`, and must propagate a native `AggregateException` unchanged when its flattened inner exceptions contain one of those fatal types. Never convert these failures through `FromException`. Preserve conversion for ordinary exceptions and aggregates without fatal inner exceptions, caller cancellation behavior, native channel capacity and backpressure, and source-enumerator disposal.

## Implementation contract

1. Add the shared public `ManagedCode.Communication.CQRS.CqrsRuntimeFailures.FindFatal(Exception?)` helper. It returns a direct fatal exception unchanged, or the first fatal inner exception from `AggregateException.Flatten().InnerExceptions`; it returns null for ordinary exceptions. The direct nonfatal path must not flatten or allocate. Use this helper only in the existing exception-conversion filters in `CqrsStream.cs` and `CqrsStreamNormalizer.cs`; preserve the original direct/aggregate exception propagation.
2. Add TUnit tests in `ManagedCode.Communication.Tests/CQRS/CqrsFatalExceptionTests.cs` using preconstructed sentinel instances for all three fatal types, direct aggregates and nested aggregates including a deep aggregate. Exercise both actual `Create` and `Normalize`; assert original exception identity, no failed chunk/handler invocation, and source disposal. Keep ordinary aggregate and cancellation controls unchanged.
3. Update the `CQRSStreaming` requirement, XML documentation and README wording so only nonfatal exceptions and aggregates without fatal inner exceptions are described as converted to failed chunks.
4. Verify the focused cases, then run the owning repository's full restore, Release build, format verification, and full TUnit suite. Package release follows the repository's canonical `release.yml` workflow.

## Compatibility and delivery

No public API, wire schema, serializer, or package dependency changes. The next patch is `10.2.8`, set through both existing version properties in `Directory.Build.props`. Publish through the existing main-branch release workflow, verify the exact-source CI and package publication, and confirm all intended package artifacts are available before consuming this version elsewhere.
