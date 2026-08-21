using System;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication.Commands.Execution;

/// <summary>
///     Runtime services and options used by a command execution.
/// </summary>
public sealed class CommandExecutionRuntime
{
    private readonly CommandExecutionOptions _optionsSnapshot;

    /// <summary>
    ///     Creates a command execution runtime.
    /// </summary>
    public CommandExecutionRuntime(
        CommandExecutionOptions? options = null,
        ICommandIdempotencyStore? idempotencyStore = null,
        ICommandRateLimiter? rateLimiter = null,
        TimeProvider? timeProvider = null,
        ILogger? logger = null,
        ICommandCircuitBreaker? circuitBreaker = null)
    {
        var optionsSnapshot = CommandExecutionOptions.CreateSnapshot(options ?? new CommandExecutionOptions());
        optionsSnapshot.Validate();
        _optionsSnapshot = optionsSnapshot;
        IdempotencyStore = idempotencyStore;
        RateLimiter = rateLimiter;
        TimeProvider = timeProvider ?? TimeProvider.System;
        Logger = logger;
        CircuitBreaker = circuitBreaker ?? (_optionsSnapshot.CircuitBreaker.Enabled
            ? new PartitionedCommandCircuitBreaker(_optionsSnapshot.CircuitBreaker, TimeProvider)
            : null);
    }

    /// <summary>A detached copy of the validated execution options.</summary>
    public CommandExecutionOptions Options => CommandExecutionOptions.CreateSnapshot(_optionsSnapshot);

    internal CommandExecutionOptions OptionsSnapshot => _optionsSnapshot;

    /// <summary>Optional idempotency backend.</summary>
    public ICommandIdempotencyStore? IdempotencyStore { get; }

    /// <summary>Optional local or distributed rate limiter.</summary>
    public ICommandRateLimiter? RateLimiter { get; }

    /// <summary>Optional stateful circuit breaker shared by command executions.</summary>
    public ICommandCircuitBreaker? CircuitBreaker { get; }

    /// <summary>Clock used by timeout, retry, and telemetry logic.</summary>
    public TimeProvider TimeProvider { get; }

    /// <summary>Logger that receives original exceptions.</summary>
    public ILogger? Logger { get; }
}
