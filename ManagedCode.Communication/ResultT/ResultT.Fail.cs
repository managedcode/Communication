using System;

namespace ManagedCode.Communication;

/// <summary>
/// Represents a result of an operation with a specific type.
/// This partial class contains methods for creating failed results.
/// </summary>
public partial struct Result<T>
{
    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static Result<T> Fail()
    {
        return new Result<T>(false, default, null, default);
    }

    /// <summary>
    /// Creates a failed result with a specific value.
    /// </summary>
    public static Result<T> Fail(T value)
    {
        return new Result<T>(false, value, null, default);
    }

    /// <summary>
    /// Creates a failed result with a specific error code.
    /// </summary>
    public static Result<T> Fail<TEnum>(TEnum code) where TEnum : Enum
    {
        return new Result<T>(false, default, new[] { Error.Create(code) }, default);
    }

    /// <summary>
    /// Creates a failed result with a specific error code and value.
    /// </summary>
    public static Result<T> Fail<TEnum>(TEnum code, T value) where TEnum : Enum
    {
        return new Result<T>(false, value, new[] { Error.Create(code) }, default);
    }

    /// <summary>
    /// Creates a failed result with a specific message.
    /// </summary>
    public static Result<T> Fail(string message)
    {
        return new Result<T>(false, default, new[] { Error.Create(message) }, default);
    }

    /// <summary>
    /// Creates a failed result with a specific message and value.
    /// </summary>
    public static Result<T> Fail(string message, T value)
    {
        return new Result<T>(false, value, new[] { Error.Create(message) }, default);
    }

    /// <summary>
    /// Creates a failed result with a specific message and error code.
    /// </summary>
    public static Result<T> Fail<TEnum>(string message, TEnum code) where TEnum : Enum
    {
        return new Result<T>(false, default, new[] { Error.Create(message, code) }, default);
    }

    /// <summary>
    /// Creates a failed result with a specific message, error code, and value.
    /// </summary>
    public static Result<T> Fail<TEnum>(string message, TEnum code, T value) where TEnum : Enum
    {
        return new Result<T>(false, value, new[] { Error.Create(message, code) }, default);
    }

    /// <summary>
    /// Creates a failed result with a specific error code and message.
    /// </summary>
    public static Result<T> Fail<TEnum>(TEnum code, string message) where TEnum : Enum
    {
        return new Result<T>(false, default, new[] { Error.Create(message, code) }, default);
    }

    /// <summary>
    /// Creates a failed result with a specific error code, message, and value.
    /// </summary>
    public static Result<T> Fail<TEnum>(TEnum code, string message, T value) where TEnum : Enum
    {
        return new Result<T>(false, value, new[] { Error.Create(message, code) }, default);
    }

    /// <summary>
    /// Creates a failed result with a specific error.
    /// </summary>
    public static Result<T> Fail(Error error)
    {
        return new Result<T>(false, default, new[] { error }, default);
    }

    /// <summary>
    /// Creates a failed result with a specific error.
    /// </summary>
    public static Result<T> Fail(Error? error)
    {
        if (error.HasValue)
            return new Result<T>(false, default, new[] { error.Value }, default);

        return new Result<T>(false, default, default, default);
    }

    /// <summary>
    /// Creates a failed result with a specific array of errors.
    /// </summary>
    public static Result<T> Fail(Error[]? errors)
    {
        return new Result<T>(false, default, errors, default);
    }

    /// <summary>
    /// Creates a failed result with a specific exception.
    /// </summary>
    public static Result<T> Fail(Exception? exception)
    {
        return new Result<T>(false, default, new[] { Error.FromException(exception) }, default);
    }

    /// <summary>
    /// Creates a failed result with a specific exception and value.
    /// </summary>
    public static Result<T> Fail(Exception? exception, T value)
    {
        return new Result<T>(false, value, new[] { Error.FromException(exception) }, default);
    }
}