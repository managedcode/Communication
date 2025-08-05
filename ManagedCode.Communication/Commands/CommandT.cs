using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication.Commands;

[Serializable]
[DebuggerDisplay("CommandId: {CommandId}; {Value?.ToString()}")]
public partial class Command<T> : ICommand<T>
{
    internal Command(string commandId, T? value)
    {
        CommandId = commandId;
        Value = value;
        CommandType = Value?.GetType()
            .Name ?? string.Empty;
    }

    internal Command(string commandId, string commandType, T? value)
    {
        CommandId = commandId;
        Value = value;
        CommandType = commandType;
    }

    [JsonPropertyName("commandId")]
    [JsonPropertyOrder(1)]
    public string CommandId { get; set; }

    [JsonPropertyName("commandType")]
    [JsonPropertyOrder(2)]
    public string CommandType { get; set; }

    [JsonPropertyName("value")]
    [JsonPropertyOrder(3)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public T? Value { get; set; }

    [JsonIgnore]
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsEmpty => Value is null;
}