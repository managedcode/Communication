using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication;

[Serializable]
[DebuggerDisplay("Id: {Id}; {Value?.ToString()}")]
public partial struct Command<T> : ICommand<T>
{ 
    internal Command(string id, T? value)
    {
        Id = id;
        Value = value;
        CommandType = Value?.GetType().Name ?? string.Empty;
    }
    
    internal Command(string id, string commandType, T? value)
    {
        Id = id;
        Value = value;
        CommandType = commandType;
    }
    
    public string Id { get; set; }
    public string CommandType { get; set; }

    public T? Value { get; set; }

    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsEmpty => Value is null;
}