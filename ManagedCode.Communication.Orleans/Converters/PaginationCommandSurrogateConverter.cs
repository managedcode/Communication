using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Orleans.Surrogates;
using Orleans;

namespace ManagedCode.Communication.Orleans.Converters;

[RegisterConverter]
public sealed class PaginationCommandSurrogateConverter : IConverter<PaginationCommand, CommandTSurrogate<PaginationRequest>>
{
    public PaginationCommand ConvertFromSurrogate(in CommandTSurrogate<PaginationRequest> surrogate)
    {
        var value = surrogate.Value ?? new PaginationRequest(0, 0);
        var command = PaginationCommand.Create(value, options: null, commandId: surrogate.CommandId);
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

    public CommandTSurrogate<PaginationRequest> ConvertToSurrogate(in PaginationCommand value)
    {
        return new CommandTSurrogate<PaginationRequest>(
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
