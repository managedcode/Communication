using Orleans;

namespace ManagedCode.Communication.Surrogates;

[Immutable]
[GenerateSerializer]
public struct CommandTSurrogate<T>
{
    [Id(0)] public string Id;
    [Id(1)] public T? Value;

    public CommandTSurrogate(string id, T? value)
    {
        Id = id;
        Value = value;
    }
}