using System;

namespace ManagedCode.Communication;

/// <summary>
///     Defines a contract for a result that contains invalid data.
/// </summary>
public interface IResultInvalid
{
    /// <summary>
    ///     Gets a value indicating whether the result is invalid.
    /// </summary>
    /// <value>true if the result is invalid; otherwise, false.</value>
    bool IsInvalid { get; }

    /// <summary>
    ///     Adds an invalid message to the result.
    /// </summary>
    /// <param name="message">The invalid message to add.</param>
    [Obsolete("Use Problem.AddValidationError instead")]
    void AddInvalidMessage(string message);

    /// <summary>
    ///     Adds an invalid message to the result with a specific key.
    /// </summary>
    /// <param name="key">The key of the invalid message.</param>
    /// <param name="value">The value of the invalid message.</param>
    [Obsolete("Use Problem.AddValidationError instead")]
    void AddInvalidMessage(string key, string value);
}
