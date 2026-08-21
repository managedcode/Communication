using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication.Commands;

/// <summary>
///     Ancillary information about a command: who issued it, how it should be executed, and free-form tags. The identity and correlation fields live on the command itself.
/// </summary>
[Serializable]
public class CommandMetadata
{
    /// <summary>
    ///     Creates metadata with the default execution policy: normal priority, 3 retries, a 30 second timeout.
    /// </summary>
    public CommandMetadata()
    {
        Properties = new Dictionary<string, object?>();
        Tags = new Dictionary<string, string>();
        Extensions = new Dictionary<string, object?>();
    }

    /// <summary>
    ///     Schema version of this metadata, so consumers can evolve the shape.
    /// </summary>
    [JsonPropertyName(CommunicationJsonNames.Version)]
    public uint Version { get; set; } = 1;

    /// <summary>
    ///     Arbitrary properties attached by the application.
    /// </summary>
    [JsonPropertyName(CommunicationJsonNames.Properties)]
    public Dictionary<string, object?> Properties { get; set; }

    /// <summary>
    ///     Who or what issued the command.
    /// </summary>
    [JsonPropertyName(CommunicationJsonNames.InitiatedBy)]
    public string? InitiatedBy { get; set; }

    /// <summary>
    ///     System or component the command came from.
    /// </summary>
    [JsonPropertyName(CommunicationJsonNames.Source)]
    public string? Source { get; set; }

    /// <summary>
    ///     System or component the command is aimed at.
    /// </summary>
    [JsonPropertyName(CommunicationJsonNames.Target)]
    public string? Target { get; set; }

    /// <summary>
    ///     Caller's IP address, when known.
    /// </summary>
    [JsonPropertyName(CommunicationJsonNames.IpAddress)]
    public string? IpAddress { get; set; }

    /// <summary>
    ///     Caller's user agent, when known.
    /// </summary>
    [JsonPropertyName(CommunicationJsonNames.UserAgent)]
    public string? UserAgent { get; set; }

    /// <summary>
    ///     Session the command belongs to.
    /// </summary>
    [JsonPropertyName(CommunicationJsonNames.SessionId)]
    public string? SessionId { get; set; }

    /// <summary>
    ///     Distributed-tracing trace identifier.
    /// </summary>
    [JsonPropertyName(CommunicationJsonNames.TraceId)]
    public string? TraceId { get; set; }

    /// <summary>
    ///     Distributed-tracing span identifier.
    /// </summary>
    [JsonPropertyName(CommunicationJsonNames.SpanId)]
    public string? SpanId { get; set; }

    /// <summary>
    ///     W3C <c>tracestate</c> propagated with <see cref="TraceId" /> and <see cref="SpanId" />.
    /// </summary>
    [JsonPropertyName(CommunicationJsonNames.TraceState)]
    public string? TraceState { get; set; }

    /// <summary>Whether the propagated W3C parent requested recording.</summary>
    [JsonPropertyName(CommunicationJsonNames.TraceRecorded)]
    public bool TraceRecorded { get; set; }

    /// <summary>
    ///     Relative execution priority. Defaults to <c>Normal</c>.
    /// </summary>
    [JsonPropertyName(CommunicationJsonNames.Priority)]
    public CommandPriority Priority { get; set; } = CommandPriority.Normal;

    /// <summary>
    ///     How many times execution has already been retried.
    /// </summary>
    [JsonPropertyName(CommunicationJsonNames.RetryCount)]
    public int RetryCount { get; set; }

    /// <summary>
    ///     Retry budget before the command is abandoned. Defaults to 3.
    /// </summary>
    [JsonPropertyName(CommunicationJsonNames.MaxRetries)]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    ///     Execution timeout in seconds. Defaults to 30.
    /// </summary>
    [JsonPropertyName(CommunicationJsonNames.TimeoutSeconds)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    ///     How long execution took, filled in once the command completes.
    /// </summary>
    [JsonPropertyName(CommunicationJsonNames.ExecutionTime)]
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    ///     How long the command stays valid; <c>null</c> means it does not expire.
    /// </summary>
    [JsonPropertyName(CommunicationJsonNames.TimeToLiveSeconds)]
    public int? TimeToLiveSeconds { get; set; }

    /// <summary>
    ///     String tags for routing, filtering or reporting.
    /// </summary>
    [JsonPropertyName(CommunicationJsonNames.Tags)]
    public Dictionary<string, string> Tags { get; set; }

    /// <summary>
    ///     Extension slot for data that does not fit the fields above.
    /// </summary>
    [JsonPropertyName(CommunicationJsonNames.Extensions)]
    public Dictionary<string, object?> Extensions { get; set; }
}
