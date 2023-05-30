using ManagedCode.Communication.Surrogates;
using Orleans;

namespace ManagedCode.Communication.Converters;

[RegisterConverter]
public sealed class CommandSurrogateConverter : IConverter<Command, CommandSurrogate>
{
    public Command ConvertFromSurrogate(in CommandSurrogate surrogate)
    {
        return new Command(surrogate.Id);
    }

    public CommandSurrogate ConvertToSurrogate(in Command value)
    {
        return new CommandSurrogate(value.Id);
    }
}