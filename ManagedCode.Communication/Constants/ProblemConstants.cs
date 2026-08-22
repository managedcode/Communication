namespace ManagedCode.Communication.Constants;

/// <summary>
/// All constants related to Problem types.
/// </summary>
public static class ProblemConstants
{
    /// <summary>
    /// Standard problem titles for common HTTP status codes.
    /// </summary>
    public static class Titles
    {
        /// <summary>
        ///     Title for a request that carries no valid credentials (401).
        /// </summary>
        public const string Unauthorized = "Unauthorized";
        /// <summary>
        ///     Title for a request whose credentials are valid but insufficient (403).
        /// </summary>
        public const string Forbidden = "Forbidden";
        /// <summary>
        ///     Title for a resource that does not exist (404).
        /// </summary>
        public const string NotFound = "Not Found";
        /// <summary>
        ///     Title for a request whose contents failed validation (400/422).
        /// </summary>
        public const string ValidationFailed = "Validation Failed";
        /// <summary>
        ///     Title for a fault on the server side (500).
        /// </summary>
        public const string InternalServerError = "Internal Server Error";
        /// <summary>
        ///     Title for a malformed request (400).
        /// </summary>
        public const string BadRequest = "Bad Request";
        /// <summary>
        ///     Title for a request that conflicts with the current state (409).
        /// </summary>
        public const string Conflict = "Conflict";
        /// <summary>
        ///     Title for a well-formed request the server cannot act on (422).
        /// </summary>
        public const string UnprocessableEntity = "Unprocessable Entity";
        /// <summary>
        ///     Title for a rate-limited request (429).
        /// </summary>
        public const string TooManyRequests = "Too Many Requests";
        /// <summary>
        ///     Title for a temporarily unavailable service (503).
        /// </summary>
        public const string ServiceUnavailable = "Service Unavailable";
        /// <summary>
        ///     Title for an upstream dependency that did not answer in time (504).
        /// </summary>
        public const string GatewayTimeout = "Gateway Timeout";
        /// <summary>
        ///     Title used by the generic fallback problem, when nothing more specific is known.
        /// </summary>
        public const string Error = "Error";
        /// <summary>
        ///     Title for a required value that was null.
        /// </summary>
        public const string NullValue = "Null Value";
        /// <summary>
        ///     Title for an invalid argument.
        /// </summary>
        public const string InvalidArgument = "Invalid Argument";
        /// <summary>
        ///     Title for an argument outside its allowed range.
        /// </summary>
        public const string ArgumentOutOfRange = "Argument Out of Range";
        /// <summary>
        ///     Title for an operation that conflicts with the current state.
        /// </summary>
        public const string InvalidState = "Invalid State";
    }

    /// <summary>
    /// Standard problem detail messages.
    /// </summary>
    public static class Messages
    {
        /// <summary>
        ///     Detail for a malformed request.
        /// </summary>
        public const string BadRequest = "The request could not be understood by the server due to malformed syntax.";
        /// <summary>
        ///     Detail for a request missing authentication.
        /// </summary>
        public const string UnauthorizedAccess = "Authentication is required to access this resource.";
        /// <summary>
        ///     Detail for a request that is authenticated but not permitted.
        /// </summary>
        public const string ForbiddenAccess = "You do not have permission to access this resource.";
        /// <summary>
        ///     Detail for a missing resource.
        /// </summary>
        public const string ResourceNotFound = "The requested resource was not found.";
        /// <summary>
        ///     Detail for a validation failure; the individual errors live in the <c>errors</c> extension.
        /// </summary>
        public const string ValidationErrors = "One or more validation errors occurred.";
        /// <summary>
        ///     Detail used by the generic fallback problem.
        /// </summary>
        public const string GenericError = "An error occurred";
        /// <summary>
        ///     Detail used when a value is rejected without a more specific reason.
        /// </summary>
        public const string InvalidMessage = "Invalid";
        /// <summary>
        ///     Detail for a required value that was null.
        /// </summary>
        public const string NullValue = "A required value was null.";
        /// <summary>
        ///     Detail for an invalid argument.
        /// </summary>
        public const string InvalidArgument = "An argument was invalid.";
        /// <summary>
        ///     Detail for an argument outside its allowed range.
        /// </summary>
        public const string ArgumentOutOfRange = "An argument was outside its allowed range.";
        /// <summary>
        ///     Detail for an operation that conflicts with the current state.
        /// </summary>
        public const string InvalidState = "The operation is not valid for the current state.";
    }

    /// <summary>
    /// Stable machine-readable codes for primitive failures.
    /// </summary>
    public static class ErrorCodes
    {
        /// <summary>Error code for a required value that was null.</summary>
        public const string Null = "null";
        /// <summary>Error code for an invalid argument.</summary>
        public const string InvalidArgument = "invalid_argument";
        /// <summary>Error code for an argument outside its allowed range.</summary>
        public const string ArgumentOutOfRange = "argument_out_of_range";
        /// <summary>Error code for an operation that conflicts with the current state.</summary>
        public const string InvalidState = "invalid_state";
    }

    /// <summary>
    /// Standard problem type URIs according to RFC 7807.
    /// </summary>
    public static class Types
    {
        /// <summary>
        ///     The RFC 7807 default type, meaning "no more specific type is defined".
        /// </summary>
        public const string AboutBlank = "about:blank";
        /// <summary>
        ///     Type URI marking a validation failure. <c>Result.IsInvalid</c> tests for exactly this value.
        /// </summary>
        public const string ValidationFailed = "https://tools.ietf.org/html/rfc7231#section-6.5.1";

        /// <summary>
        ///     Builds the type URI for a status code, e.g. <c>https://httpstatuses.io/404</c>.
        /// </summary>
        /// <param name="statusCode">HTTP status code.</param>
        public static string HttpStatus(int statusCode) => $"https://httpstatuses.io/{statusCode}";
    }

    /// <summary>
    /// Keys for Problem extensions dictionary to avoid string literals.
    /// </summary>
    public static class ExtensionKeys
    {
        /// <summary>
        /// Key for validation errors in problem extensions
        /// </summary>
        public const string Errors = "errors";

        /// <summary>
        /// Key for error type (enum type name) in problem extensions
        /// </summary>
        public const string ErrorType = "errorType";

        /// <summary>
        /// Key for trace ID in problem extensions
        /// </summary>
        public const string TraceId = "traceId";

        /// <summary>
        /// Key for exception data prefix
        /// </summary>
        public const string ExceptionDataPrefix = "exception.";

        /// <summary>
        /// Key for error code in problem extensions
        /// </summary>
        public const string ErrorCode = "errorCode";

        /// <summary>
        /// Key for original exception type in problem extensions
        /// </summary>
        public const string OriginalExceptionType = "originalExceptionType";

        /// <summary>
        /// Key for the enum member name behind an error code.
        /// </summary>
        public const string ErrorCodeName = "errorCodeName";

        /// <summary>
        /// Key for the numeric enum value behind an error code.
        /// </summary>
        public const string ErrorCodeValue = "errorCodeValue";

        /// <summary>
        /// Key for an inner exception description.
        /// </summary>
        public const string InnerException = "innerException";

        /// <summary>
        /// Key for a stack trace. Only attach this outside production.
        /// </summary>
        public const string StackTrace = "stackTrace";

        /// <summary>
        /// Key for the SignalR hub method name.
        /// </summary>
        public const string HubMethod = "hubMethod";

        /// <summary>
        /// Key for the SignalR hub type name.
        /// </summary>
        public const string HubType = "hubType";

        /// <summary>
        /// Key for a server- or dependency-provided retry delay.
        /// </summary>
        public const string RetryAfter = "retryAfter";

        /// <summary>
        /// Key indicating that an authoritative retry delay exceeded the configured maximum.
        /// </summary>
        public const string RetryAfterExceedsMaximum = "retryAfterExceedsMaximum";

        /// <summary>
        /// Key for the number of retry attempts performed.
        /// </summary>
        public const string RetryAttempts = "retryAttempts";

        /// <summary>
        /// Key indicating that command execution exhausted its retry budget.
        /// </summary>
        public const string RetriesExhausted = "retriesExhausted";

        /// <summary>
        /// Key for the timeout duration in milliseconds.
        /// </summary>
        public const string TimeoutMilliseconds = "timeoutMilliseconds";

        /// <summary>
        /// Key for the command timeout kind.
        /// </summary>
        public const string TimeoutKind = "timeoutKind";

        /// <summary>
        /// Key for the UTC instant at which a command expired.
        /// </summary>
        public const string ExpiredAtUtc = "expiredAtUtc";

        /// <summary>
        /// Key for a physical command-attempt number.
        /// </summary>
        public const string Attempt = "attempt";

        /// <summary>
        /// Key for a circuit-breaker state.
        /// </summary>
        public const string CircuitState = "circuitState";

        /// <summary>
        /// Key for a circuit-breaker partition.
        /// </summary>
        public const string CircuitPartition = "circuitPartition";
    }

    /// <summary>
    /// Field names for validation errors.
    /// </summary>
    public static class ValidationFields
    {
        /// <summary>
        /// General field name for validation errors that don't belong to a specific field.
        /// </summary>
        public const string General = "_general";
    }

    /// <summary>
    /// Stable titles emitted by command execution and its adapters.
    /// </summary>
    internal static class CommandExecutionTitles
    {
        public const string InvalidCommandIdentifier = "Invalid command identifier";
        public const string CommandTimedOut = "Command timed out";
        public const string CommandExecutionInfrastructureFailure = "Command execution infrastructure failure";
        public const string InvalidCommandLifetime = "Invalid command lifetime";
        public const string CommandExpired = "Command expired";
        public const string CorruptIdempotencyOutcome = "Corrupt idempotency outcome";
        public const string IdempotencyConflict = "Idempotency conflict";
        public const string IdempotencyKeyConflict = "Idempotency key conflict";
        public const string InvalidIdempotencyState = "Invalid idempotency state";
        public const string InvalidIdempotencyResponse = "Invalid idempotency response";
        public const string IdempotencyOwnershipLost = "Idempotency ownership lost";
        public const string IndeterminateCommandOutcome = "Indeterminate command outcome";
        public const string IdempotencyClaimRenewalFailed = "Idempotency claim renewal failed";
        public const string IdempotencyReleaseFailure = "Idempotency release failure";
        public const string MissingIdempotencyScope = "Missing idempotency scope";
        public const string MissingIdempotencyFingerprint = "Missing idempotency fingerprint";
        public const string InvalidIdempotencyConfiguration = "Invalid idempotency configuration";
        public const string CommandAttemptTimedOut = "Command attempt timed out";
        public const string CommandCircuitIsOpen = "Command circuit is open";
        public const string CommandAttemptCancelled = "Command attempt cancelled";
        public const string CommandRateLimitExceeded = "Command rate limit exceeded";
        public const string CommandRateLimitCleanupFailure = "Command rate-limit cleanup failure";
        public const string CommandRateLimitCallbackFailure = "Command rate-limit callback failure";
        public const string CommandTimeoutCallbackFailure = "Command timeout callback failure";
        public const string CommandRetryCallbackFailure = "Command retry callback failure";
        public const string CommandCircuitBreakerCallbackFailure = "Command circuit-breaker callback failure";
    }

    /// <summary>
    /// Stable details emitted by command execution and its adapters.
    /// </summary>
    internal static class CommandExecutionMessages
    {
        public const string IdempotencyRequiresCommandId =
            "Idempotent command execution requires a non-empty CommandId.";

        public const string InfrastructureFailure =
            "A command execution infrastructure component failed. Inspect correlated server diagnostics.";

        public const string InvalidTimeToLive =
            "Command metadata TimeToLiveSeconds must be greater than zero when supplied.";

        public const string UnsupportedTimeToLiveRange =
            "The command timestamp and time-to-live exceed the supported UTC timestamp range.";

        public const string CommandExpired =
            "The command exceeded its declared time-to-live before execution began.";

        public const string IdempotencyKeyConflict =
            "The idempotency key is already associated with a different operation, request, or result contract.";

        public const string MissingCachedOutcome =
            "The idempotency record is terminal but its cached outcome is missing.";

        public const string CachedOutcomeContractMismatch =
            "The cached outcome does not match the declared result contract.";

        public const string UnsafeIdempotencyConflict =
            "The command cannot be executed safely with the supplied idempotency key.";

        public const string InvalidStoreAcquisition =
            "The idempotency store returned an invalid acquisition response.";

        public const string InvalidOrleansAcquisition =
            "The Orleans idempotency grain returned an invalid acquisition response.";

        public const string UnsupportedIdempotencyStateFormat =
            "The idempotency record is in unsupported state {0}.";

        public const string IdempotencyOwnershipLost =
            "The command finished, but its fenced idempotency claim was no longer valid.";

        public const string CancelledIndeterminateOutcome =
            "Execution was cancelled after the business handler started; the side-effect outcome is unknown.";

        public const string InfrastructureIndeterminateOutcome =
            "Execution infrastructure failed after ownership was acquired; the side-effect outcome may be unknown.";

        public const string RenewalRejected =
            "The idempotency store rejected renewal of the active fenced claim.";

        public const string RenewalFailed =
            "The active claim could not be renewed, so execution was cancelled to prevent an unfenced side effect.";

        public const string ClaimReleaseFailed =
            "The unstarted command claim could not be released.";

        public const string MissingScope =
            "Configure Idempotency.ScopeSelector from trusted authenticated execution context.";

        public const string MissingFingerprint =
            "Configure Idempotency.FingerprintSelector from the immutable business request payload.";

        public const string InvalidScopeOrFingerprint =
            "The configured idempotency scope and fingerprint must be non-empty.";

        public const string RateLimitExceeded =
            "The command could not acquire a rate-limit permit.";

        public const string RateLimitCleanupFailed =
            "The command outcome was preserved, but its rate-limit lease could not be released cleanly.";

        public const string TimeoutCallbackFailed =
            "The timeout outcome was preserved, but its callback failed.";

        public const string CircuitCallbackFailed =
            "The circuit transition was preserved, but its callback failed.";

        public const string PreviousExecutionIndeterminate =
            "The previous execution may have performed its side effect, but no terminal outcome was persisted.";

        public const string UnsupportedIndeterminateResolution =
            "This idempotency store does not support explicit indeterminate-state resolution.";

        public const string UnsupportedIndeterminateReset =
            "This idempotency store does not support explicit indeterminate-state reset.";
    }
}
