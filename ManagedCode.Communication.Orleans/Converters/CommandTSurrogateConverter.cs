using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Surrogates;
using Orleans;

namespace ManagedCode.Communication.Converters;

[RegisterConverter]
public sealed class CommandTSurrogateConverter<T> : IConverter<Command<T>, CommandTSurrogate<T>>
{
    public Command<T> ConvertFromSurrogate(in CommandTSurrogate<T> surrogate)
    {
        return new Command<T>(surrogate.Id, surrogate.CommandType, surrogate.Value);
    }

    public CommandTSurrogate<T> ConvertToSurrogate(in Command<T> value)
    {
        return new CommandTSurrogate<T>(value.CommandId, value.CommandType, value.Value);
    }
}