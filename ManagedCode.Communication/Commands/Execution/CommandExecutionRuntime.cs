using System;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication.Commands.Execution;

/// <summary>
///     Runtime services and options used by a command execution.
/// </summary>
public sealed class CommandExecutionRuntime
{
    /// <summary>
    ///     Creates a command execution runtime.
    /// </summary>
    public CommandExecutionRuntime(
        CommandExecutionOptions? options = null,
        ICommandIdempotencyStore? idempotencyStore = null,
        ICommandRateLimiter? rateLimiter = null,
        TimeProvider? timeProvider = null,
        ILogger? logger = null)
    {
        Options = options ?? new CommandExecutionOptions();
        IdempotencyStore = idempotencyStore;
        RateLimiter = rateLimiter;
        TimeProvider = timeProvider ?? TimeProvider.System;
        Logger = logger;
    }

    /// <summary>Execution options.</summary>
    public CommandExecutionOptions Options { get; }

    /// <summary>Optional idempotency backend.</summary>
    public ICommandIdempotencyStore? IdempotencyStore { get; }

    /// <summary>Optional local or distributed rate limiter.</summary>
    public ICommandRateLimiter? RateLimiter { get; }

    /// <summary>Clock used by timeout, retry, and telemetry logic.</summary>
    public TimeProvider TimeProvider { get; }

    /// <summary>Logger that receives original exceptions.</summary>
    public ILogger? Logger { get; }
}
