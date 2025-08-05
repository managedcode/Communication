using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication.Commands;

[Serializable]
[DebuggerDisplay("CommandId: {CommandId}")]
public partial class Command : ICommand
{
    internal Command(string commandId, string commandType)
    {
        CommandId = commandId;
        CommandType = commandType;
    }

    [JsonPropertyName("commandId")]
    [JsonPropertyOrder(1)]
    public string CommandId { get; set; }

    [JsonPropertyName("commandType")]
    [JsonPropertyOrder(2)]
    public string CommandType { get; set; }
}