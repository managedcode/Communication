using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication;

/// <summary>
///     Defines a comprehensive contract for a result in the system with standardized validation properties and JSON serialization.
/// </summary>
public interface IResult : IResultProblem, IResultInvalid
{
    /// <summary>
    ///     Gets a value indicating whether the operation was successful.
    /// </summary>
    /// <value>true if the operation was successful; otherwise, false.</value>
    [JsonPropertyName("isSuccess")]
    [JsonPropertyOrder(1)]
    bool IsSuccess { get; }

    /// <summary>
    ///     Gets a value indicating whether the operation failed.
    /// </summary>
    /// <value>true if the operation failed; otherwise, false.</value>
    [JsonIgnore]
    bool IsFailed { get; }

    /// <summary>
    ///     Gets a value indicating whether the result is valid (successful and has no problems).
    /// </summary>
    /// <value>true if the result is valid; otherwise, false.</value>
    [JsonIgnore]
    bool IsValid => IsSuccess && !HasProblem;

    /// <summary>
    ///     Gets a value indicating whether the result is not invalid (equivalent to IsValid for consistency).
    /// </summary>
    /// <value>true if the result is not invalid; otherwise, false.</value>
    [JsonIgnore]
    bool IsNotInvalid => !IsInvalid;

    /// <summary>
    ///     Gets the validation errors dictionary for JSON serialization.
    /// </summary>
    /// <value>Dictionary containing validation errors, or null if no validation errors exist.</value>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    Dictionary<string, List<string>>? InvalidObject { get; }

    /// <summary>
    ///     Checks if a specific field has validation errors.
    /// </summary>
    /// <param name="fieldName">The name of the field to check.</param>
    /// <returns>true if the field has validation errors; otherwise, false.</returns>
    bool InvalidField(string fieldName);

    /// <summary>
    ///     Gets the validation error message for a specific field.
    /// </summary>
    /// <param name="fieldName">The name of the field to get errors for.</param>
    /// <returns>A concatenated string of all error messages for the field, or empty string if no errors.</returns>
    string InvalidFieldError(string fieldName);
}