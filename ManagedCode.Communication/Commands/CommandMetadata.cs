using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication.Commands;

[Serializable]
public class CommandMetadata
{
    public CommandMetadata()
    {
        Properties = new Dictionary<string, object?>();
        Tags = new Dictionary<string, string>();
        Extensions = new Dictionary<string, object?>();
    }

    [JsonPropertyName("version")]
    public uint Version { get; set; } = 1;

    [JsonPropertyName("properties")]
    public Dictionary<string, object?> Properties { get; set; }

    [JsonPropertyName("initiatedBy")]
    public string? InitiatedBy { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("target")]
    public string? Target { get; set; }

    [JsonPropertyName("ipAddress")]
    public string? IpAddress { get; set; }

    [JsonPropertyName("userAgent")]
    public string? UserAgent { get; set; }

    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }

    [JsonPropertyName("spanId")]
    public string? SpanId { get; set; }

    [JsonPropertyName("priority")]
    public CommandPriority Priority { get; set; } = CommandPriority.Normal;

    [JsonPropertyName("retryCount")]
    public int RetryCount { get; set; }

    [JsonPropertyName("maxRetries")]
    public int MaxRetries { get; set; } = 3;

    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; } = 30;

    [JsonPropertyName("executionTime")]
    public TimeSpan ExecutionTime { get; set; }

    [JsonPropertyName("timeToLiveSeconds")]
    public int? TimeToLiveSeconds { get; set; }

    [JsonPropertyName("tags")]
    public Dictionary<string, string> Tags { get; set; }

    [JsonPropertyName("extensions")]
    public Dictionary<string, object?> Extensions { get; set; }
}