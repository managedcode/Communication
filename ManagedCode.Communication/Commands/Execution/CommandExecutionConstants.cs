namespace ManagedCode.Communication.Commands.Execution;

internal static class CommandExecutionConstants
{
    public const string TotalTimeoutKind = "total";
    public const string AttemptTimeoutKind = "attempt";
    public const string RetryCallback = "retry";
    public const string RetriesExhaustedCallback = "retries-exhausted";
    public const string RateLimitQueuedCallback = "queued";
    public const string RateLimitRejectedCallback = "rejected";
    public const string CircuitCallbackPhase = "circuit_callback";
    public const string IdempotencyRenewalPhase = "idempotency_renewal";
    public const string IdempotencyFinalizationPhase = "idempotency_finalization";
    public const string RateLimitCleanupPhase = "rate_limit_cleanup";
    public const string RateLimitCallbackPhase = "rate_limit_callback";
    public const string TimeoutCallbackPhase = "timeout_callback";
    public const string RetryCallbackPhase = "retry_callback";
    public const string IdempotencyStoreErrorOutcome = "store_error";
    public const string IdempotencyHitOutcome = "hit";
    public const string IdempotencyConflictOutcome = "conflict";
    public const string IdempotencyIndeterminateOutcome = "indeterminate";
    public const string IdempotencyWaitOutcome = "wait";
    public const string IdempotencyMissOutcome = "miss";
    public const string CommandIdFormat = "D";
    public const char IdempotencyKeySeparator = '\u001f';

    public const string TotalTimeoutDetailFormat =
        "The command exceeded its {0:0} ms total execution timeout.";

    public const string AttemptTimeoutDetailFormat =
        "Command attempt {0} exceeded its {1:0} ms timeout.";

    public const string CircuitOpenDetailFormat =
        "Dependency partition '{0}' is {1} and is rejecting command attempts.";

    public const string CallbackOutcomeDetailFormat =
        "The {0} outcome was preserved, but its callback failed.";

    public const string CallbackDecisionDetailFormat =
        "The {0} decision was preserved, but its callback failed.";

    public const string InvalidGeneratedTimeoutDetailFormat =
        "The generated {0} timeout must be positive or Timeout.InfiniteTimeSpan.";
}
