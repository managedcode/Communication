using System;
using ManagedCode.Communication.Commands;

namespace ManagedCode.Communication;

/// <summary>
///     Defines a contract for a Commands in the system.
/// </summary>
public interface ICommand
{
    /// <summary>
    ///     Gets the unique identifier for the command.
    /// </summary>
    /// <value>The unique identifier for the command.</value>
    Guid CommandId { get; set; }

    /// <summary>
    ///     Gets the type of the command.
    /// </summary>
    /// <value>The type of the command.</value>
    string CommandType { get; set; }

    /// <summary>
    ///     Gets or sets the UTC timestamp when the command was created.
    /// </summary>
    DateTime Timestamp { get; set; }

    /// <summary>
    ///     Gets or sets the correlation identifier for tracking related commands.
    /// </summary>
    string? CorrelationId { get; set; }

    /// <summary>
    ///     Gets or sets the causation identifier indicating what caused this command.
    /// </summary>
    string? CausationId { get; set; }

    /// <summary>
    ///     Gets or sets the trace identifier for distributed tracing.
    /// </summary>
    string? TraceId { get; set; }

    /// <summary>
    ///     Gets or sets the span identifier for distributed tracing.
    /// </summary>
    string? SpanId { get; set; }

    /// <summary>
    ///     Gets or sets the user identifier who initiated the command.
    /// </summary>
    string? UserId { get; set; }

    /// <summary>
    ///     Gets or sets the session identifier for the command execution context.
    /// </summary>
    string? SessionId { get; set; }

    /// <summary>
    ///     Gets or sets additional metadata for the command.
    /// </summary>
    CommandMetadata? Metadata { get; set; }
}
