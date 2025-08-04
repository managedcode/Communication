using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text.Json.Serialization;
using ManagedCode.Communication.Extensions;

namespace ManagedCode.Communication;

/// <summary>
/// Represents a result from an operation that can either succeed or fail.
/// </summary>
/// <typeparam name="T">The type of the result value.</typeparam>
[Serializable]
[DebuggerDisplay("IsSuccess: {IsSuccess}; {GetError().HasValue ? \" Error code: \" + GetError()!.Value.ErrorCode : string.Empty}")]
public partial struct Result<T> : IResult<T>
{
    /// <summary>
    /// Initializes a new instance of the Result struct.
    /// </summary>
    internal Result(bool isSuccess, T? value, Error[]? errors, Dictionary<string, string>? invalidObject)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
        InvalidObject = invalidObject;
    }

    /// <summary>
    /// Initializes a new instance of the Result struct with an exception.
    /// </summary>
    internal Result(Exception exception) : this(false, default, new[] { Error.FromException(exception) }, default)
    {
    }

    /// <summary>
    /// Adds an error to the result.
    /// </summary>
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
    /// Throws an exception if the result is a failure.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Value))]
    public bool ThrowIfFail()
    {
        if(IsSuccess)
            return false;
        
        if (Errors?.Any() is not true)
        {
            if(IsFailed)
                throw new Exception(nameof(IsFailed));

            return false;
        }

        var exceptions = Errors.Select(s => s.Exception() ?? new Exception(StringExtension.JoinFilter(';', s.ErrorCode, s.Message)));

        if (Errors.Length == 1)
            throw exceptions.First();

        throw new AggregateException(exceptions);
    }

    /// <summary>
    /// Throws an exception with stack trace preserved if the result indicates a failure.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Value))]
    public bool ThrowIfFailWithStackPreserved()
    {
        if (Errors?.Any() is not true)
        {
            if (IsFailed)
                throw new Exception(nameof(IsFailed));

            return false;
        }

        var exceptions = Errors.Select(s => s.ExceptionInfo() ?? ExceptionDispatchInfo.Capture(new Exception(StringExtension.JoinFilter(';', s.ErrorCode, s.Message))));

        if (Errors.Length == 1)
        {
            exceptions.First().Throw();
        }

        throw new AggregateException(exceptions.Select(e => e.SourceException));
    }

    /// <summary>
    /// Gets a value indicating whether the result is a success.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets a value indicating whether the result is empty.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsEmpty => Value is null;

    /// <summary>
    /// Gets a value indicating whether the result is a failure.
    /// </summary>
    [JsonIgnore]
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsFailed => !IsSuccess;

    /// <summary>
    /// Gets or sets the value of the result.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public T? Value { get; set; }

    /// <summary>
    /// Gets the first error of the result.
    /// </summary>
    public Error? GetError()
    {
        if (Errors == null || Errors.Length == 0)
            return null;

        return Errors[0];
    }

    /// <summary>
    /// Gets or sets the errors of the result.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Error[]? Errors { get; set; }

    /// <summary>
    /// Gets or sets the invalid object of the result.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<string, string>? InvalidObject { get; set; }

    /// <summary>
    /// Gets a value indicating whether the result is invalid.
    /// </summary>
    [JsonIgnore]
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsInvalid => !IsSuccess || InvalidObject?.Any() is true;

    /// <summary>
    /// Gets the error code as a specific enum type.
    /// </summary>
    public TEnum? ErrorCodeAs<TEnum>() where TEnum : Enum
    {
        return GetError().HasValue ? GetError()!.Value.ErrorCodeAs<TEnum>() : default;
    }

    /// <summary>
    /// Checks if the error code is a specific value.
    /// </summary>
    public bool IsErrorCode(Enum value)
    {
        return GetError()?.IsErrorCode(value) ?? false;
    }

    /// <summary>
    /// Checks if the error code is not a specific value.
    /// </summary>
    public bool IsNotErrorCode(Enum value)
    {
        return GetError()?.IsNotErrorCode(value) ?? false;
    }

    /// <summary>
    /// Adds an invalid message to the result.
    /// </summary>
    public void AddInvalidMessage(string message)
    {
        InvalidObject ??= new Dictionary<string, string>();
        InvalidObject[nameof(message)] = message;
    }

    /// <summary>
    /// Adds an invalid message with a specific key to the result.
    /// </summary>
    public void AddInvalidMessage(string key, string value)
    {
        InvalidObject ??= new Dictionary<string, string>();
        InvalidObject[key] = value;
    }
}