using System;
using System.Collections.Generic;
using ManagedCode.Communication.Commands;
using Orleans;

namespace ManagedCode.Communication.Orleans.Surrogates;

[Immutable]
[GenerateSerializer]
public struct CommandMetadataSurrogate
{
    [Id(0)] public string? InitiatedBy;
    [Id(1)] public string? Source;
    [Id(2)] public string? Target;
    [Id(3)] public string? IpAddress;
    [Id(4)] public string? UserAgent;
    [Id(5)] public string? SessionId;
    [Id(6)] public string? TraceId;
    [Id(7)] public string? SpanId;
    [Id(8)] public uint Version;
    [Id(9)] public CommandPriority Priority;
    [Id(10)] public int RetryCount;
    [Id(11)] public int MaxRetries;
    [Id(12)] public int TimeoutSeconds;
    [Id(13)] public TimeSpan ExecutionTime;
    [Id(14)] public int? TimeToLiveSeconds;
    [Id(15)] public Dictionary<string, string>? Tags;
    [Id(16)] public Dictionary<string, object?>? Extensions;
}

[RegisterConverter]
public sealed class CommandMetadataSurrogateConverter : IConverter<CommandMetadata, CommandMetadataSurrogate>
{
    public CommandMetadata ConvertFromSurrogate(in CommandMetadataSurrogate surrogate)
    {
        return new CommandMetadata
        {
            InitiatedBy = surrogate.InitiatedBy,
            Source = surrogate.Source,
            Target = surrogate.Target,
            IpAddress = surrogate.IpAddress,
            UserAgent = surrogate.UserAgent,
            SessionId = surrogate.SessionId,
            TraceId = surrogate.TraceId,
            SpanId = surrogate.SpanId,
            Version = surrogate.Version,
            Priority = surrogate.Priority,
            RetryCount = surrogate.RetryCount,
            MaxRetries = surrogate.MaxRetries,
            TimeoutSeconds = surrogate.TimeoutSeconds,
            ExecutionTime = surrogate.ExecutionTime,
            TimeToLiveSeconds = surrogate.TimeToLiveSeconds,
            Tags = surrogate.Tags ?? new Dictionary<string, string>(),
            Extensions = surrogate.Extensions ?? new Dictionary<string, object?>()
        };
    }

    public CommandMetadataSurrogate ConvertToSurrogate(in CommandMetadata value)
    {
        return new CommandMetadataSurrogate
        {
            InitiatedBy = value.InitiatedBy,
            Source = value.Source,
            Target = value.Target,
            IpAddress = value.IpAddress,
            UserAgent = value.UserAgent,
            SessionId = value.SessionId,
            TraceId = value.TraceId,
            SpanId = value.SpanId,
            Version = value.Version,
            Priority = value.Priority,
            RetryCount = value.RetryCount,
            MaxRetries = value.MaxRetries,
            TimeoutSeconds = value.TimeoutSeconds,
            ExecutionTime = value.ExecutionTime,
            TimeToLiveSeconds = value.TimeToLiveSeconds,
            Tags = value.Tags,
            Extensions = value.Extensions
        };
    }
}