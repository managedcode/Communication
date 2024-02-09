using Orleans;

namespace ManagedCode.Communication.Surrogates;

[Immutable]
[GenerateSerializer]
public struct CommandTSurrogate<T>
{
    [Id(0)] public string Id;
    [Id(1)] public string CommandType;
    [Id(2)] public T? Value;

    public CommandTSurrogate(string id, string commandType, T? value)
    {
        Id = id;
        CommandType = commandType;
        Value = value;
    }
}