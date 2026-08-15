using System;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Orleans.Surrogates;
using Orleans;

namespace ManagedCode.Communication.Orleans.Converters;

/// <summary>
///     Orleans converter between <c>CommandT</c> and its serialization surrogate.
/// </summary>
[RegisterConverter]
public sealed class CommandTSurrogateConverter<T> : IConverter<Command<T>, CommandTSurrogate<T>>
{
    /// <summary>
    ///     Rebuilds the value from its surrogate.
    /// </summary>
    public Command<T> ConvertFromSurrogate(in CommandTSurrogate<T> surrogate)
    {
        var command = Command<T>.Create(surrogate.CommandType, surrogate.Value!, surrogate.CommandId);
        command.Timestamp = surrogate.Timestamp;
        command.CorrelationId = surrogate.CorrelationId;
        command.CausationId = surrogate.CausationId;
        command.TraceId = surrogate.TraceId;
        command.SpanId = surrogate.SpanId;
        command.UserId = surrogate.UserId;
        command.SessionId = surrogate.SessionId;
        command.Metadata = surrogate.Metadata;
        return command;
    }

    /// <summary>
    ///     Converts the value into its surrogate for serialization.
    /// </summary>
    public CommandTSurrogate<T> ConvertToSurrogate(in Command<T> value)
    {
        return new CommandTSurrogate<T>(
            value.CommandId,
            value.CommandType,
            value.Value,
            value.Timestamp,
            value.CorrelationId,
            value.CausationId,
            value.TraceId,
            value.SpanId,
            value.UserId,
            value.SessionId,
            value.Metadata);
    }
}