namespace ManagedCode.Communication.Orleans;

/// <summary>Stable Orleans resource names required by command execution adapters.</summary>
public static class OrleansCommandExecutionDefaults
{
    /// <summary>Persistent-state name used by the idempotency grain.</summary>
    public const string IdempotencyStateName = "commandState";

    /// <summary>Named grain-storage provider used by the idempotency grain.</summary>
    public const string IdempotencyStorageName = "commandStore";
}

internal static class OrleansCommandExecutionConstants
{
    public const string MissingStorageMessageFormat =
        "Orleans command execution requires grain storage named '{0}'. Configure it before the silo starts.";

    public const string DisposeHolderAfterAcquireFailureLog =
        "Failed to dispose an Orleans rate-limit holder after acquisition failed.";

    public const string CancellationCleanupShutdownFailureLog =
        "One or more Orleans rate-limit cancellation cleanups failed during shutdown.";

    public const string CancellationCleanupTimeoutLog =
        "Timed out after {CleanupTimeout} while cleaning a cancelled Orleans rate-limit acquisition.";

    public const string CancellationCleanupFailureLog =
        "Failed to complete cleanup for a cancelled Orleans rate-limit acquisition.";

    public const string DisposeHolderAfterCancellationFailureLog =
        "Failed to dispose an Orleans rate-limit holder after cancellation.";

    public const string LateAcquisitionFailureLog =
        "Late Orleans rate-limit acquisition failed after cancellation.";

    public const string DisposeLateLeaseFailureLog =
        "Failed to dispose a late Orleans rate-limit lease.";
}
