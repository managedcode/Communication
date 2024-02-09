using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;
using ManagedCode.Communication.Extensions;

namespace ManagedCode.Communication;

/// <summary>
/// Represents a result of an operation.
/// </summary>
[Serializable]
[DebuggerDisplay("IsSuccess: {IsSuccess}; {GetError().HasValue ? \" Error code: \" + GetError()!.Value.ErrorCode : string.Empty}")]
public partial struct Result : IResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> struct.
    /// </summary>
    /// <param name="isSuccess">Indicates whether the operation was successful.</param>
    /// <param name="errors">The errors that occurred during the operation.</param>
    /// <param name="invalidObject">The invalid object that caused the operation to fail.</param>
    internal Result(bool isSuccess, Error[]? errors, Dictionary<string, string>? invalidObject)
    {
        IsSuccess = isSuccess;
        Errors = errors;
        InvalidObject = invalidObject;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> struct with an exception.
    /// </summary>
    /// <param name="exception">The exception that caused the operation to fail.</param>
    internal Result(Exception exception) : this(false, new[] { Error.FromException(exception) }, default)
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    [JsonIgnore]
    public bool IsFailed => !IsSuccess;

    /// <summary>
    /// Adds an error to the result.
    /// </summary>
    /// <param name="error">The error to add.</param>
    public void AddError(Error error)
    {
        if (Errors == null)
        {
            Errors = new[] { error };
        }
        else
        {
            var list = Errors.ToList();
            list.Add(error);
            Errors = list.ToArray();
        }
    }

    /// <summary>
    /// Throws an exception if the result indicates a failure.
    /// </summary>
    public void ThrowIfFail()
    {
        if (Errors?.Any() is not true)
        {
            if(IsFailed)
                throw new Exception(nameof(IsFailed));
            
            return;
        }

        var exceptions = Errors.Select(s => s.Exception() ?? new Exception(StringExtension.JoinFilter(';', s.ErrorCode, s.Message)));
        if (Errors.Length == 1)
            throw exceptions.First();

        throw new AggregateException(exceptions);
    }

    /// <summary>
    /// Gets the error code as a specific enumeration type.
    /// </summary>
    /// <typeparam name="TEnum">The type of the enumeration.</typeparam>
    /// <returns>The error code as the specified enumeration type, or null if the result does not contain an error code.</returns>
    public TEnum? ErrorCodeAs<TEnum>() where TEnum : Enum
    {
        return GetError().HasValue ? GetError()!.Value.ErrorCodeAs<TEnum>() : default;
    }

    /// <summary>
    /// Determines whether the error code is a specific value.
    /// </summary>
    /// <param name="value">The value to compare with the error code.</param>
    /// <returns>true if the error code is the specified value; otherwise, false.</returns>
    public bool IsErrorCode(Enum value)
    {
        return GetError()?.IsErrorCode(value) ?? false;
    }

    /// <summary>
    /// Determines whether the error code is not a specific value.
    /// </summary>
    /// <param name="value">The value to compare with the error code.</param>
    /// <returns>true if the error code is not the specified value; otherwise, false.</returns>
    public bool IsNotErrorCode(Enum value)
    {
        return GetError()?.IsNotErrorCode(value) ?? false;
    }

    /// <summary>
    /// Gets the first error from the result.
    /// </summary>
    /// <returns>The first error, or null if the result does not contain any errors.</returns>
    public Error? GetError()
    {
        if (Errors == null || Errors.Length == 0)
            return null;

        return Errors[0];
    }

    /// <summary>
    /// Gets or sets the errors that occurred during the operation.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Error[]? Errors { get; set; }

    /// <summary>
    /// Gets or sets the invalid object that caused the operation to fail.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<string, string>? InvalidObject { get; set; }

    /// <summary>
    /// Gets a value indicating whether the result is invalid.
    /// </summary>
    [JsonIgnore]
    public bool IsInvalid => !IsSuccess && InvalidObject?.Any() is true;

    /// <summary>
    /// Adds an invalid message to the result.
    /// </summary>
    /// <param name="message">The invalid message to add.</param>
    public void AddInvalidMessage(string message)
    {
        InvalidObject ??= new Dictionary<string, string>();
        InvalidObject[nameof(message)] = message;
    }

    /// <summary>
    /// Adds an invalid message to the result with a specific key.
    /// </summary>
    /// <param name="key">The key of the invalid message.</param>
    /// <param name="value">The value of the invalid message.</param>
    public void AddInvalidMessage(string key, string value)
    {
        InvalidObject ??= new Dictionary<string, string>();
        InvalidObject[key] = value;
    }
}