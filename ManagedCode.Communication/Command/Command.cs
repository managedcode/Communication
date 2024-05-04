using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication;

[Serializable]
[DebuggerDisplay("Id: {Id}")]
public partial struct Command : ICommand
{ 
    internal Command(string id, string commandType)
    {
        Id = id;
        CommandType = commandType;
    }
    
    public string Id { get; set; }
    public string CommandType { get; set; }
}