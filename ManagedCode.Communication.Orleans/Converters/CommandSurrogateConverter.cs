using System;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Surrogates;
using Orleans;

namespace ManagedCode.Communication.Converters;

[RegisterConverter]
public sealed class CommandSurrogateConverter : IConverter<Command, CommandSurrogate>
{
    public Command ConvertFromSurrogate(in CommandSurrogate surrogate)
    {
        var command = Command.Create(surrogate.CommandId, surrogate.CommandType);
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

    public CommandSurrogate ConvertToSurrogate(in Command value)
    {
        return new CommandSurrogate(
            value.CommandId,
            value.CommandType,
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