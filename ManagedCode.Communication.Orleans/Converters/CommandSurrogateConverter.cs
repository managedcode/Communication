using System;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Orleans.Surrogates;
using Orleans;

namespace ManagedCode.Communication.Orleans.Converters;

/// <summary>
///     Orleans converter between <c>Command</c> and its serialization surrogate.
/// </summary>
[RegisterConverter]
public sealed class CommandSurrogateConverter : IConverter<Command, CommandSurrogate>
{
    /// <summary>
    ///     Rebuilds the value from its surrogate.
    /// </summary>
    public Command ConvertFromSurrogate(in CommandSurrogate surrogate)
    {
        var command = Command.Create(surrogate.CommandType, surrogate.CommandId);
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