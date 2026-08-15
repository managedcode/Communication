using System;
using System.Collections.Generic;
using ManagedCode.Communication.Commands;
using Orleans;

namespace ManagedCode.Communication.Orleans.Surrogates;

/// <summary>
///     Orleans serialization surrogate for <c>CommandMetadata</c>.
/// </summary>
[Immutable]
[GenerateSerializer]
public struct CommandMetadataSurrogate
{
    /// <summary>
    ///     Who or what issued the command.
    /// </summary>
    [Id(0)] public string? InitiatedBy;
    /// <summary>
    ///     System the command came from.
    /// </summary>
    [Id(1)] public string? Source;
    /// <summary>
    ///     System the command is aimed at.
    /// </summary>
    [Id(2)] public string? Target;
    /// <summary>
    ///     Caller's IP address.
    /// </summary>
    [Id(3)] public string? IpAddress;
    /// <summary>
    ///     Caller's user agent.
    /// </summary>
    [Id(4)] public string? UserAgent;
    /// <summary>
    ///     Session the command belongs to.
    /// </summary>
    [Id(5)] public string? SessionId;
    /// <summary>
    ///     Distributed-tracing trace identifier.
    /// </summary>
    [Id(6)] public string? TraceId;
    /// <summary>
    ///     Distributed-tracing span identifier.
    /// </summary>
    [Id(7)] public string? SpanId;
    /// <summary>
    ///     Metadata schema version.
    /// </summary>
    [Id(8)] public uint Version;
    /// <summary>
    ///     Relative execution priority.
    /// </summary>
    [Id(9)] public CommandPriority Priority;
    /// <summary>
    ///     Retries already attempted.
    /// </summary>
    [Id(10)] public int RetryCount;
    /// <summary>
    ///     Retry budget.
    /// </summary>
    [Id(11)] public int MaxRetries;
    /// <summary>
    ///     Execution timeout, in seconds.
    /// </summary>
    [Id(12)] public int TimeoutSeconds;
    /// <summary>
    ///     How long execution took.
    /// </summary>
    [Id(13)] public TimeSpan ExecutionTime;
    /// <summary>
    ///     How long the command stays valid.
    /// </summary>
    [Id(14)] public int? TimeToLiveSeconds;
    /// <summary>
    ///     String tags.
    /// </summary>
    [Id(15)] public Dictionary<string, string>? Tags;
    /// <summary>
    ///     Extension data.
    /// </summary>
    [Id(16)] public Dictionary<string, object?>? Extensions;
}

/// <summary>
///     Orleans serialization surrogate for <c>CommandMetadata</c>.
/// </summary>
[RegisterConverter]
public sealed class CommandMetadataSurrogateConverter : IConverter<CommandMetadata, CommandMetadataSurrogate>
{
    /// <summary>
    ///     Rebuilds the value from its surrogate.
    /// </summary>
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

    /// <summary>
    ///     Converts the value into its surrogate for serialization.
    /// </summary>
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