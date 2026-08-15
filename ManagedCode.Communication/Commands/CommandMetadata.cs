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
    [JsonPropertyName("version")]
    public uint Version { get; set; } = 1;

    /// <summary>
    ///     Arbitrary properties attached by the application.
    /// </summary>
    [JsonPropertyName("properties")]
    public Dictionary<string, object?> Properties { get; set; }

    /// <summary>
    ///     Who or what issued the command.
    /// </summary>
    [JsonPropertyName("initiatedBy")]
    public string? InitiatedBy { get; set; }

    /// <summary>
    ///     System or component the command came from.
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>
    ///     System or component the command is aimed at.
    /// </summary>
    [JsonPropertyName("target")]
    public string? Target { get; set; }

    /// <summary>
    ///     Caller's IP address, when known.
    /// </summary>
    [JsonPropertyName("ipAddress")]
    public string? IpAddress { get; set; }

    /// <summary>
    ///     Caller's user agent, when known.
    /// </summary>
    [JsonPropertyName("userAgent")]
    public string? UserAgent { get; set; }

    /// <summary>
    ///     Session the command belongs to.
    /// </summary>
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    /// <summary>
    ///     Distributed-tracing trace identifier.
    /// </summary>
    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }

    /// <summary>
    ///     Distributed-tracing span identifier.
    /// </summary>
    [JsonPropertyName("spanId")]
    public string? SpanId { get; set; }

    /// <summary>
    ///     Relative execution priority. Defaults to <c>Normal</c>.
    /// </summary>
    [JsonPropertyName("priority")]
    public CommandPriority Priority { get; set; } = CommandPriority.Normal;

    /// <summary>
    ///     How many times execution has already been retried.
    /// </summary>
    [JsonPropertyName("retryCount")]
    public int RetryCount { get; set; }

    /// <summary>
    ///     Retry budget before the command is abandoned. Defaults to 3.
    /// </summary>
    [JsonPropertyName("maxRetries")]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    ///     Execution timeout in seconds. Defaults to 30.
    /// </summary>
    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    ///     How long execution took, filled in once the command completes.
    /// </summary>
    [JsonPropertyName("executionTime")]
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    ///     How long the command stays valid; <c>null</c> means it does not expire.
    /// </summary>
    [JsonPropertyName("timeToLiveSeconds")]
    public int? TimeToLiveSeconds { get; set; }

    /// <summary>
    ///     String tags for routing, filtering or reporting.
    /// </summary>
    [JsonPropertyName("tags")]
    public Dictionary<string, string> Tags { get; set; }

    /// <summary>
    ///     Extension slot for data that does not fit the fields above.
    /// </summary>
    [JsonPropertyName("extensions")]
    public Dictionary<string, object?> Extensions { get; set; }
}