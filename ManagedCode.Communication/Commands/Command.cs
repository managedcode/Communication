using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication.Commands;

[Serializable]
[DebuggerDisplay("CommandId: {CommandId}")]
public partial class Command : ICommand, ICommandFactory<Command>
{
    [JsonConstructor]
    protected Command()
    {
        CommandType = string.Empty;
    }
    
    protected Command(Guid commandId, string commandType)
    {
        CommandId = commandId;
        CommandType = commandType;
        Timestamp = DateTime.UtcNow;
    }

    [JsonPropertyName("commandId")]
    [JsonPropertyOrder(1)]
    public Guid CommandId { get; set; }

    [JsonPropertyName("commandType")]
    [JsonPropertyOrder(2)]
    public string CommandType { get; set; }

    [JsonPropertyName("timestamp")]
    [JsonPropertyOrder(3)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("correlationId")]
    [JsonPropertyOrder(4)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CorrelationId { get; set; }

    [JsonPropertyName("causationId")]
    [JsonPropertyOrder(5)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CausationId { get; set; }

    [JsonPropertyName("traceId")]
    [JsonPropertyOrder(6)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TraceId { get; set; }

    [JsonPropertyName("spanId")]
    [JsonPropertyOrder(7)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SpanId { get; set; }

    [JsonPropertyName("userId")]
    [JsonPropertyOrder(8)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UserId { get; set; }

    [JsonPropertyName("sessionId")]
    [JsonPropertyOrder(9)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SessionId { get; set; }

    [JsonPropertyName("metadata")]
    [JsonPropertyOrder(10)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CommandMetadata? Metadata { get; set; }

    /// <summary>
    /// Creates a new command with generated ID and specified type
    /// </summary>
    public static Command Create(string commandType)
    {
        return CommandFactoryBridge.Create<Command>(commandType);
    }

    /// <summary>
    /// Creates a new command with a generated identifier using an enum value as the command type.
    /// </summary>
    /// <typeparam name="TEnum">Enum that represents the command type.</typeparam>
    /// <param name="commandType">Enum value converted to the command type string.</param>
    public static Command Create<TEnum>(TEnum commandType)
        where TEnum : Enum
    {
        return CommandFactoryBridge.Create<Command, TEnum>(commandType);
    }
    
    /// <summary>
    /// Creates a new command with a specific identifier and command type.
    /// </summary>
    /// <param name="commandId">Unique command identifier.</param>
    /// <param name="commandType">Logical command type.</param>
    public static Command Create(Guid commandId, string commandType)
    {
        if (string.IsNullOrWhiteSpace(commandType))
        {
            throw new ArgumentException("Command type must be provided.", nameof(commandType));
        }

        return new Command(commandId, commandType);
    }

    /// <summary>
    /// Creates a new command with a specific identifier using an enum value as the command type.
    /// </summary>
    /// <typeparam name="TEnum">Enum that represents the command type.</typeparam>
    /// <param name="commandId">Unique command identifier.</param>
    /// <param name="commandType">Enum value converted to the command type string.</param>
    public static Command Create<TEnum>(Guid commandId, TEnum commandType)
        where TEnum : Enum
    {
        return CommandFactoryBridge.Create<Command, TEnum>(commandId, commandType);
    }

    /// <summary>
    /// Try to convert CommandType string to an enum value
    /// </summary>
    public Result<TEnum> GetCommandTypeAsEnum<TEnum>() where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(CommandType, true, out var result))
        {
            return Result<TEnum>.Succeed(result);
        }
        return Result<TEnum>.Fail("InvalidCommandType", $"Cannot convert '{CommandType}' to enum {typeof(TEnum).Name}");
    }
}
