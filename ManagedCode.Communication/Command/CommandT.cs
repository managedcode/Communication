using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication;

[Serializable]
[DebuggerDisplay("CommandId: {CommandId}; {Value?.ToString()}")]
public partial class Command<T> : ICommand<T>
{ 
    internal Command(string commandId, T? value)
    {
        CommandId = commandId;
        Value = value;
        CommandType = Value?.GetType().Name ?? string.Empty;
    }
    
    internal Command(string commandId, string commandType, T? value)
    {
        CommandId = commandId;
        Value = value;
        CommandType = commandType;
    }
    
    public string CommandId { get; set; }
    public string CommandType { get; set; }

    public T? Value { get; set; }

    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsEmpty => Value is null;
}