using System;

namespace ManagedCode.Communication;

/// <summary>
/// Defines a contract for a result that contains an error.
/// </summary>
public interface IResultError
{
    /// <summary>
    /// Adds an error to the result.
    /// </summary>
    /// <param name="error">The error to add.</param>
    void AddError(Error error);

    /// <summary>
    /// Gets the error from the result.
    /// </summary>
    /// <returns>The error, or null if the result does not contain an error.</returns>
    Error? GetError();

    /// <summary>
    /// Throws an exception if the result indicates a failure.
    /// </summary>
    void ThrowIfFail();

    /// <summary>
    /// Throws an exception with stack trace preserved if the result indicates a failure.
    /// </summary>
    void ThrowIfFailWithStackPreserved();

    /// <summary>
    /// Gets the error code as a specific enumeration type.
    /// </summary>
    /// <typeparam name="TEnum">The type of the enumeration.</typeparam>
    /// <returns>The error code as the specified enumeration type, or null if the result does not contain an error code.</returns>
    TEnum? ErrorCodeAs<TEnum>() where TEnum : Enum;

    /// <summary>
    /// Determines whether the error code is a specific value.
    /// </summary>
    /// <param name="value">The value to compare with the error code.</param>
    /// <returns>true if the error code is the specified value; otherwise, false.</returns>
    bool IsErrorCode(Enum value);

    /// <summary>
    /// Determines whether the error code is not a specific value.
    /// </summary>
    /// <param name="value">The value to compare with the error code.</param>
    /// <returns>true if the error code is not the specified value; otherwise, false.</returns>
    bool IsNotErrorCode(Enum value);
}