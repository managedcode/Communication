using Orleans;

namespace ManagedCode.Communication.Surrogates;

[Immutable]
[GenerateSerializer]
public struct CommandSurrogate
{
    [Id(0)] public string? Id;

    public CommandSurrogate(string? id)
    {
        Id = id;
    }
}