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

    /// <summary>Rate-limiting behavior.</summary>
    public CommandRateLimiterOptions RateLimiter { get; } = new();
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
    public Func<Exception, bool> ShouldRetryException { get; set; } = static _ => true;

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
    ///     Explicit timeout. When omitted, <see cref="CommandMetadata.TimeoutSeconds" /> is used.
    /// </summary>
    public TimeSpan? Timeout { get; set; }
}

/// <summary>
///     Controls command-result caching and duplicate suppression.
/// </summary>
public sealed class IdempotencyOptions
{
    /// <summary>Whether an available idempotency store should be used.</summary>
    public bool Enabled { get; set; } = true;
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

/// <summary>
///     Information supplied to rate-limiter callbacks.
/// </summary>
public sealed record CommandRateLimitEvent(ICommand Command, TimeSpan QueueDuration, Problem? Problem);
