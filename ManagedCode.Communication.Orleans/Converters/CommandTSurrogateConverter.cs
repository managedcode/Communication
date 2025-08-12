using System;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Surrogates;
using Orleans;

namespace ManagedCode.Communication.Converters;

[RegisterConverter]
public sealed class CommandTSurrogateConverter<T> : IConverter<Command<T>, CommandTSurrogate<T>>
{
    public Command<T> ConvertFromSurrogate(in CommandTSurrogate<T> surrogate)
    {
        var command = Command<T>.Create(surrogate.CommandId, surrogate.CommandType, surrogate.Value!);
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