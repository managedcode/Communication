using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication.Commands;
/// <summary>
///     A command carrying a payload.
/// </summary>

[Serializable]
[DebuggerDisplay("CommandId: {CommandId}; {Value?.ToString()}")]
public partial class Command<T> : ICommand<T>, ICommandValueFactory<Command<T>, T>
{
    /// <summary>
    ///     Creates an empty command. Present for serializers; use the factory methods.
    /// </summary>
    [JsonConstructor]
    protected Command()
    {
        CommandType = typeof(T).Name;
    }

    /// <summary>
    ///     Creates a command with the given identity and payload, named after the payload type.
    /// </summary>
    protected Command(Guid commandId, T? value)
    {
        CommandId = commandId;
        Value = value;
        CommandType = Value?.GetType()
            .Name ?? typeof(T).Name;
    }

    /// <summary>
    ///     Creates a command with the given identity, type and payload.
    /// </summary>
    protected Command(Guid commandId, string commandType, T? value)
    {
        CommandId = commandId;
        Value = value;
        CommandType = commandType;
    }
    /// <summary>
    ///     Identity of the command. Generated as a time-ordered UUIDv7 unless supplied.
    /// </summary>

    [JsonPropertyName("commandId")]
    [JsonPropertyOrder(1)]
    public Guid CommandId { get; set; }
    /// <summary>
    ///     Logical name of the command.
    /// </summary>

    [JsonPropertyName("commandType")]
    [JsonPropertyOrder(2)]
    public string CommandType { get; set; }
    /// <summary>
    ///     The payload.
    /// </summary>

    [JsonPropertyName("value")]
    [JsonPropertyOrder(3)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public T? Value { get; set; }
    /// <summary>
    ///     When the command was created (UTC).
    /// </summary>

    [JsonPropertyName("timestamp")]
    [JsonPropertyOrder(4)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    /// <summary>
    ///     Ties this command to every other operation in the same logical flow. Not generated — set it yourself.
    /// </summary>

    [JsonPropertyName("correlationId")]
    [JsonPropertyOrder(5)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CorrelationId { get; set; }
    /// <summary>
    ///     Identity of the command that caused this one. Not generated — set it yourself.
    /// </summary>

    [JsonPropertyName("causationId")]
    [JsonPropertyOrder(6)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CausationId { get; set; }
    /// <summary>
    ///     Distributed-tracing trace identifier.
    /// </summary>

    [JsonPropertyName("traceId")]
    [JsonPropertyOrder(7)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TraceId { get; set; }
    /// <summary>
    ///     Distributed-tracing span identifier.
    /// </summary>

    [JsonPropertyName("spanId")]
    [JsonPropertyOrder(8)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SpanId { get; set; }
    /// <summary>
    ///     User on whose behalf the command runs.
    /// </summary>

    [JsonPropertyName("userId")]
    [JsonPropertyOrder(9)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UserId { get; set; }
    /// <summary>
    ///     Session the command belongs to.
    /// </summary>

    [JsonPropertyName("sessionId")]
    [JsonPropertyOrder(10)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SessionId { get; set; }
    /// <summary>
    ///     Execution policy and free-form metadata.
    /// </summary>

    [JsonPropertyName("metadata")]
    [JsonPropertyOrder(11)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CommandMetadata? Metadata { get; set; }
    /// <summary>
    ///     Whether the command carries no payload.
    /// </summary>

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
