using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication;

/// <summary>
///     Defines a contract for a result that contains a value of a specific type.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public interface IResult<out T> : IResult
{
    /// <summary>
    ///     Gets the value from the result.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(2)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    T? Value { get; }

    /// <summary>
    ///     Gets a value indicating whether the result value is empty (null).
    /// </summary>
    [JsonIgnore]
    [MemberNotNullWhen(false, nameof(Value))]
    bool IsEmpty { get; }

    /// <summary>
    ///     Gets a value indicating whether the result has a non-empty value.
    /// </summary>
    [JsonIgnore]
    [MemberNotNullWhen(true, nameof(Value))]
    bool HasValue => !IsEmpty;
}