# CQRS Streaming

## Requirements and acceptance

**REQ-CQRS-001:** CQRS stream helpers preserve the existing ordinary failure, backpressure, cancellation, and disposal contract without converting process-fatal runtime exceptions into recoverable results.

- **AC-CQRS-001:** `CqrsStream.Create` propagates the original `OutOfMemoryException`, `StackOverflowException`, or `AccessViolationException` instance thrown by a handler. It emits no failed chunk for that exception, whether it occurs before or after a progress chunk. Ordinary exceptions still produce exactly one failed terminal chunk.
- **AC-CQRS-002:** `CqrsStream.Normalize` propagates those same fatal exception instances when source enumeration faults and still disposes the source enumerator. Ordinary source faults retain the existing failed-terminal normalization behavior.
- **AC-CQRS-003:** Existing cancellation, bounded channel/backpressure, disposal settlement, telemetry, and HTTP/Orleans stream regression tests remain unchanged and pass.

These checks use preconstructed exception sentinels. They do not attempt to trigger actual out-of-memory, stack-overflow, or access-violation conditions in the test process.

## Scope

Fatal exceptions remain process/runtime failures. CQRS streaming must not convert them to `Result`, `Problem`, or `CqrsStreamChunkKind.Failed`; ordinary exceptions continue through the current `FromException` conversion and diagnostics. This feature does not change public types, serializers, chunk shape, or stream limits.

## Verification

Focused TUnit coverage belongs in `ManagedCode.Communication.Tests/CQRS/CqrsFatalExceptionTests.cs`, with existing controls in `CqrsStreamTests`, `CqrsStreamNormalizerTests`, and `CqrsExceptionTelemetryTests`. The owning solution's full Release build, formatter, and complete TUnit suite remain required for delivery.
