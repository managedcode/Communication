using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication.Commands;

[Serializable]
[DebuggerDisplay("CommandId: {CommandId}; {Value?.ToString()}")]
public partial class Command<T> : ICommand<T>, ICommandValueFactory<Command<T>, T>
{
    [JsonConstructor]
    protected Command()
    {
        CommandType = typeof(T).Name;
    }

    protected Command(Guid commandId, T? value)
    {
        CommandId = commandId;
        Value = value;
        CommandType = Value?.GetType()
            .Name ?? typeof(T).Name;
    }

    protected Command(Guid commandId, string commandType, T? value)
    {
        CommandId = commandId;
        Value = value;
        CommandType = commandType;
    }

    [JsonPropertyName("commandId")]
    [JsonPropertyOrder(1)]
    public Guid CommandId { get; set; }

    [JsonPropertyName("commandType")]
    [JsonPropertyOrder(2)]
    public string CommandType { get; set; }

    [JsonPropertyName("value")]
    [JsonPropertyOrder(3)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public T? Value { get; set; }

    [JsonPropertyName("timestamp")]
    [JsonPropertyOrder(4)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("correlationId")]
    [JsonPropertyOrder(5)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CorrelationId { get; set; }

    [JsonPropertyName("causationId")]
    [JsonPropertyOrder(6)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CausationId { get; set; }

    [JsonPropertyName("traceId")]
    [JsonPropertyOrder(7)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TraceId { get; set; }

    [JsonPropertyName("spanId")]
    [JsonPropertyOrder(8)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SpanId { get; set; }

    [JsonPropertyName("userId")]
    [JsonPropertyOrder(9)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UserId { get; set; }

    [JsonPropertyName("sessionId")]
    [JsonPropertyOrder(10)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SessionId { get; set; }

    [JsonPropertyName("metadata")]
    [JsonPropertyOrder(11)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CommandMetadata? Metadata { get; set; }

    [JsonIgnore]
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsEmpty => Value is null;

    /// <summary>
    /// Try to convert CommandType string to an enum value
    /// </summary>
    public Result<TEnum> GetCommandTypeAsEnum<TEnum>() where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(CommandType, true, out TEnum result))
        {
            return Result<TEnum>.Succeed(result);
        }
        return Result<TEnum>.Fail("InvalidCommandType", $"Cannot convert '{CommandType}' to enum {typeof(TEnum).Name}");
    }
}
