using Orleans;

namespace ManagedCode.Communication.Surrogates;

[Immutable]
[GenerateSerializer]
public struct CommandSurrogate
{
    [Id(0)] public string Id;
    [Id(1)] public string CommandType;

    public CommandSurrogate(string id, string commandType)
    {
        Id = id;
        CommandType = commandType;
    }
}