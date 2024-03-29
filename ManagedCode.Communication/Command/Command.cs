using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication;

[Serializable]
[DebuggerDisplay("Id: {Id}")]
public partial struct Command : ICommand
{ 
    internal Command(string? id)
    {
        Id = id;
    }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Id { get; set; }
    
}