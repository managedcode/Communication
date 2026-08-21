using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Communication.Commands.Execution;

/// <summary>
///     Options for native command execution.
/// </summary>
public sealed class CommandExecutionOptions
{
    /// <summary>Retry behavior.</summary>
    public RetryOptions Retry { get; } = new();

    /// <summary>Timeout behavior.</summary>
    public TimeoutOptions Timeout { get; } = new();

    /// <summary>Idempotency behavior.</summary>
    public IdempotencyOptions Idempotency { get; } = new();

    /// <summary>Circuit-breaker behavior.</summary>
    public CircuitBreakerOptions CircuitBreaker { get; } = new();

    /// <summary>Rate-limiting behavior.</summary>
    public CommandRateLimiterOptions RateLimiter { get; } = new();

    internal static CommandExecutionOptions CreateSnapshot(CommandExecutionOptions source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var snapshot = new CommandExecutionOptions();

        snapshot.Retry.Enabled = source.Retry.Enabled;
        snapshot.Retry.MaxRetries = source.Retry.MaxRetries;
        snapshot.Retry.Delay = source.Retry.Delay;
        snapshot.Retry.MaxDelay = source.Retry.MaxDelay;
        snapshot.Retry.MaxRetryAfter = source.Retry.MaxRetryAfter;
        snapshot.Retry.BackoffType = source.Retry.BackoffType;
        snapshot.Retry.UseJitter = source.Retry.UseJitter;
        snapshot.Retry.Randomizer = source.Retry.Randomizer;
        snapshot.Retry.ShouldRetry = source.Retry.ShouldRetry;
        snapshot.Retry.ShouldRetryException = source.Retry.ShouldRetryException;
        snapshot.Retry.ShouldRetryAsync = source.Retry.ShouldRetryAsync;
        snapshot.Retry.DelayGenerator = source.Retry.DelayGenerator;
        snapshot.Retry.OnRetry = source.Retry.OnRetry;
        snapshot.Retry.OnRetriesExhausted = source.Retry.OnRetriesExhausted;

        snapshot.Timeout.Enabled = source.Timeout.Enabled;
        snapshot.Timeout.TotalTimeout = source.Timeout.TotalTimeout;
        snapshot.Timeout.AttemptTimeout = source.Timeout.AttemptTimeout;
        snapshot.Timeout.TotalTimeoutGenerator = source.Timeout.TotalTimeoutGenerator;
        snapshot.Timeout.AttemptTimeoutGenerator = source.Timeout.AttemptTimeoutGenerator;
        snapshot.Timeout.OnTimeout = source.Timeout.OnTimeout;

        snapshot.Idempotency.Enabled = source.Idempotency.Enabled;
        snapshot.Idempotency.ScopeSelector = source.Idempotency.ScopeSelector;
        snapshot.Idempotency.FingerprintSelector = source.Idempotency.FingerprintSelector;
        snapshot.Idempotency.ClaimLease = source.Idempotency.ClaimLease;
        snapshot.Idempotency.OutcomeRetention = source.Idempotency.OutcomeRetention;
        snapshot.Idempotency.FinalizationTimeout = source.Idempotency.FinalizationTimeout;
        snapshot.Idempotency.DuplicatePollInterval = source.Idempotency.DuplicatePollInterval;
        snapshot.Idempotency.ShouldCacheOutcome = source.Idempotency.ShouldCacheOutcome;

        snapshot.CircuitBreaker.Enabled = source.CircuitBreaker.Enabled;
        snapshot.CircuitBreaker.FailureRatio = source.CircuitBreaker.FailureRatio;
        snapshot.CircuitBreaker.SamplingDuration = source.CircuitBreaker.SamplingDuration;
        snapshot.CircuitBreaker.MinimumThroughput = source.CircuitBreaker.MinimumThroughput;
        snapshot.CircuitBreaker.BreakDuration = source.CircuitBreaker.BreakDuration;
        snapshot.CircuitBreaker.PartitionKeySelector = source.CircuitBreaker.PartitionKeySelector;
        snapshot.CircuitBreaker.ShouldBreak = source.CircuitBreaker.ShouldBreak;
        snapshot.CircuitBreaker.ShouldBreakException = source.CircuitBreaker.ShouldBreakException;
        snapshot.CircuitBreaker.OnOpened = source.CircuitBreaker.OnOpened;
        snapshot.CircuitBreaker.OnHalfOpened = source.CircuitBreaker.OnHalfOpened;
        snapshot.CircuitBreaker.OnClosed = source.CircuitBreaker.OnClosed;

        snapshot.RateLimiter.Enabled = source.RateLimiter.Enabled;
        snapshot.RateLimiter.OnQueued = source.RateLimiter.OnQueued;
        snapshot.RateLimiter.OnRejected = source.RateLimiter.OnRejected;
        return snapshot;
    }

    internal void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(Retry.MaxRetries);
        ArgumentOutOfRangeException.ThrowIfLessThan(Retry.Delay, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(Retry.MaxDelay, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(Retry.MaxRetryAfter, TimeSpan.Zero);
        ArgumentNullException.ThrowIfNull(Retry.ShouldRetry);
        ArgumentNullException.ThrowIfNull(Retry.ShouldRetryException);
        ArgumentNullException.ThrowIfNull(Retry.Randomizer);
        ArgumentNullException.ThrowIfNull(Idempotency.ShouldCacheOutcome);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(Idempotency.ClaimLease, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(Idempotency.OutcomeRetention, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(Idempotency.FinalizationTimeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(Idempotency.DuplicatePollInterval, TimeSpan.Zero);
        CircuitBreaker.Validate();

        if (Timeout.Enabled && Timeout.TotalTimeout is { } totalTimeout
            && totalTimeout != System.Threading.Timeout.InfiniteTimeSpan
            && totalTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(Timeout.TotalTimeout));
        }

        if (Timeout.Enabled && Timeout.AttemptTimeout is { } attemptTimeout
            && attemptTimeout != System.Threading.Timeout.InfiniteTimeSpan
            && attemptTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(Timeout.AttemptTimeout));
        }
    }
}

/// <summary>Controls the native partitioned command circuit breaker.</summary>
public sealed class CircuitBreakerOptions
{
    /// <summary>Whether circuit breaking is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Failure ratio that opens a sufficiently populated sampling window.</summary>
    public double FailureRatio { get; set; } = 0.5D;

    /// <summary>Rolling duration used for failure sampling.</summary>
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Minimum number of sampled outcomes before ratio-based opening is allowed.</summary>
    public int MinimumThroughput { get; set; } = 10;

    /// <summary>How long an open circuit rejects executions before one half-open probe.</summary>
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Partitions breaker state by dependency target or another stable bounded identifier.</summary>
    public Func<ICommand, string> PartitionKeySelector { get; set; } = static command =>
        command.GetType().FullName ?? command.GetType().Name;

    /// <summary>Decides which failed Results contribute breaker failures.</summary>
    public Func<Problem, bool> ShouldBreak { get; set; } = static problem =>
        problem.StatusCode is (int)HttpStatusCode.InternalServerError
            or (int)HttpStatusCode.BadGateway
            or (int)HttpStatusCode.ServiceUnavailable
            or (int)HttpStatusCode.GatewayTimeout;

    /// <summary>Decides which exceptions contribute breaker failures.</summary>
    public Func<Exception, bool> ShouldBreakException { get; set; } = static exception =>
        exception is TimeoutException or System.Net.Http.HttpRequestException or System.IO.IOException;

    /// <summary>Called after a partition opens or is manually isolated.</summary>
    public Func<CommandCircuitBreakerEvent, CancellationToken, ValueTask>? OnOpened { get; set; }

    /// <summary>Called when an elapsed break duration admits one half-open probe.</summary>
    public Func<CommandCircuitBreakerEvent, CancellationToken, ValueTask>? OnHalfOpened { get; set; }

    /// <summary>Called when a successful probe or manual reset closes a partition.</summary>
    public Func<CommandCircuitBreakerEvent, CancellationToken, ValueTask>? OnClosed { get; set; }

    internal static CircuitBreakerOptions CreateSnapshot(CircuitBreakerOptions source)
    {
        return new CircuitBreakerOptions
        {
            Enabled = source.Enabled,
            FailureRatio = source.FailureRatio,
            SamplingDuration = source.SamplingDuration,
            MinimumThroughput = source.MinimumThroughput,
            BreakDuration = source.BreakDuration,
            PartitionKeySelector = source.PartitionKeySelector,
            ShouldBreak = source.ShouldBreak,
            ShouldBreakException = source.ShouldBreakException,
            OnOpened = source.OnOpened,
            OnHalfOpened = source.OnHalfOpened,
            OnClosed = source.OnClosed
        };
    }

    internal void Validate()
    {
        if (!double.IsFinite(FailureRatio) || FailureRatio <= 0D || FailureRatio > 1D)
        {
            throw new ArgumentOutOfRangeException(nameof(FailureRatio));
        }

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(SamplingDuration, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MinimumThroughput);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(BreakDuration, TimeSpan.Zero);
        ArgumentNullException.ThrowIfNull(PartitionKeySelector);
        ArgumentNullException.ThrowIfNull(ShouldBreak);
        ArgumentNullException.ThrowIfNull(ShouldBreakException);
    }
}

/// <summary>
///     Controls retries for failed command attempts.
/// </summary>
public sealed class RetryOptions
{
    /// <summary>Whether retries are enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Maximum number of retries after the first attempt.</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Base delay between attempts.</summary>
    public TimeSpan Delay { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>Maximum calculated delay.</summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Maximum authoritative server/limiter delay the executor is willing to wait. When exceeded, the current
    ///     failure is returned without retrying; the executor never retries earlier than the supplied hint.
    /// </summary>
    public TimeSpan MaxRetryAfter { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Delay growth algorithm.</summary>
    public RetryBackoffType BackoffType { get; set; } = RetryBackoffType.Exponential;

    /// <summary>Whether to randomize calculated delays to avoid synchronized retries.</summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>Supplies a value from 0 through 1 for jitter. Replace it for deterministic tests.</summary>
    public Func<double> Randomizer { get; set; } = Random.Shared.NextDouble;

    /// <summary>
    ///     Optional decision for failed results. The default retries request timeout, rate-limit, and server failures.
    /// </summary>
    public Func<Problem, bool> ShouldRetry { get; set; } = static problem =>
        problem.StatusCode is (int)HttpStatusCode.RequestTimeout
            or (int)HttpStatusCode.TooManyRequests
            or (int)HttpStatusCode.InternalServerError
            or (int)HttpStatusCode.BadGateway
            or (int)HttpStatusCode.ServiceUnavailable
            or (int)HttpStatusCode.GatewayTimeout;

    /// <summary>Optional decision for exceptions. Caller cancellation is never passed to this delegate.</summary>
    public Func<Exception, bool> ShouldRetryException { get; set; } = static exception =>
        exception is TimeoutException or System.Net.Http.HttpRequestException or System.IO.IOException;

    /// <summary>Optional asynchronous command-aware retry decision.</summary>
    public Func<CommandRetryDecisionContext, CancellationToken, ValueTask<bool>>? ShouldRetryAsync { get; set; }

    /// <summary>
    ///     Optional asynchronous command-aware delay generator. Return <see langword="null" /> or a negative delay
    ///     to use the built-in backoff calculation.
    /// </summary>
    public Func<CommandRetryDecisionContext, CancellationToken, ValueTask<TimeSpan?>>? DelayGenerator { get; set; }

    /// <summary>Called before a retry delay begins.</summary>
    public Func<CommandRetryEvent, CancellationToken, ValueTask>? OnRetry { get; set; }

    /// <summary>Called when a retryable failure consumes the complete retry budget.</summary>
    public Func<CommandRetryEvent, CancellationToken, ValueTask>? OnRetriesExhausted { get; set; }
}

/// <summary>
///     Controls the total command execution timeout.
/// </summary>
public sealed class TimeoutOptions
{
    /// <summary>Whether a timeout is enforced.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     Explicit total timeout. When omitted, <see cref="CommandMetadata.TimeoutSeconds" /> is used.
    /// </summary>
    public TimeSpan? TotalTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Optional cooperative timeout for each physical handler attempt.</summary>
    public TimeSpan? AttemptTimeout { get; set; }

    /// <summary>Optional command-aware total-timeout selector.</summary>
    public Func<ICommand, TimeSpan?>? TotalTimeoutGenerator { get; set; }

    /// <summary>Optional command-aware per-attempt-timeout selector.</summary>
    public Func<ICommand, TimeSpan?>? AttemptTimeoutGenerator { get; set; }

    /// <summary>Called when a total or per-attempt cooperative timeout is observed.</summary>
    public Func<CommandTimeoutEvent, CancellationToken, ValueTask>? OnTimeout { get; set; }
}

/// <summary>
///     Controls command-result caching and duplicate suppression.
/// </summary>
public sealed class IdempotencyOptions
{
    /// <summary>Whether an available idempotency store should be used.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     Selects the trusted owner/tenant scope for the command. Configure this from authenticated execution
    ///     context; do not trust a caller-provided command field for tenant isolation.
    /// </summary>
    public Func<ICommand, string>? ScopeSelector { get; set; }

    /// <summary>
    ///     Selects an application request fingerprint. The operation and result contract are bound separately.
    /// </summary>
    public Func<ICommand, string>? FingerprintSelector { get; set; }

    /// <summary>How long an owner claim remains valid without renewal.</summary>
    public TimeSpan ClaimLease { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>How long a terminal outcome remains replayable.</summary>
    public TimeSpan OutcomeRetention { get; set; } = TimeSpan.FromHours(24);

    /// <summary>Maximum time allowed for atomic completion/release after execution stops.</summary>
    public TimeSpan FinalizationTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Delay before checking an outcome owned by another executor again.</summary>
    public TimeSpan DuplicatePollInterval { get; set; } = TimeSpan.FromMilliseconds(25);

    /// <summary>
    ///     Decides whether a returned outcome is terminal and replayable. The default caches only outcomes produced
    ///     after the business handler was invoked, so admission failures such as a pre-handler 429 remain retryable.
    /// </summary>
    public Func<CommandIdempotencyOutcomeContext, bool> ShouldCacheOutcome { get; set; } =
        static context => context.HandlerInvoked;
}

/// <summary>
///     Controls rate-limiter callbacks.
/// </summary>
public sealed class CommandRateLimiterOptions
{
    /// <summary>Whether an available rate limiter should be used.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Called when the limiter queues a command.</summary>
    public Func<CommandRateLimitEvent, CancellationToken, ValueTask>? OnQueued { get; set; }

    /// <summary>Called when the limiter rejects a command.</summary>
    public Func<CommandRateLimitEvent, CancellationToken, ValueTask>? OnRejected { get; set; }
}

/// <summary>
///     Information supplied to retry callbacks.
/// </summary>
public sealed record CommandRetryEvent(
    ICommand Command,
    int Attempt,
    int RetryNumber,
    TimeSpan Delay,
    Problem? Problem,
    Exception? Exception);

/// <summary>Context for command-aware retry decisions and delay generation.</summary>
public sealed record CommandRetryDecisionContext(
    ICommand Command,
    int Attempt,
    int RetryNumber,
    Problem Problem,
    Exception? Exception);

/// <summary>
///     Information supplied to rate-limiter callbacks.
/// </summary>
public sealed record CommandRateLimitEvent(ICommand Command, TimeSpan QueueDuration, Problem? Problem);

/// <summary>Context used to decide whether an idempotent outcome should be cached.</summary>
public sealed record CommandIdempotencyOutcomeContext(
    ICommand Command,
    IResult Outcome,
    bool HandlerInvoked);

/// <summary>Identifies which timeout budget expired.</summary>
public enum CommandTimeoutKind
{
    /// <summary>The complete command execution budget expired.</summary>
    Total,

    /// <summary>One physical command attempt budget expired.</summary>
    Attempt
}

/// <summary>Timeout callback context.</summary>
public sealed record CommandTimeoutEvent(
    ICommand Command,
    CommandTimeoutKind Kind,
    TimeSpan Timeout,
    int? Attempt,
    Problem Problem);
