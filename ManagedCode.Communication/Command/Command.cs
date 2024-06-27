using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication;

[Serializable]
[DebuggerDisplay("CommandId: {CommandId}")]
public partial class Command : ICommand
{ 
    internal Command(string commandId, string commandType)
    {
        CommandId = commandId;
        CommandType = commandType;
    }
    
    public string CommandId { get; set; }
    public string CommandType { get; set; }
}