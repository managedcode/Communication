using System;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    public static Result<T> Fail()
    {
        return new Result<T>(false, default, null, default);
    }

    public static Result<T> Fail(T value)
    {
        return new Result<T>(false, value, null, default);
    }

    public static Result<T> Fail<TEnum>(TEnum code) where TEnum : Enum
    {
        return new Result<T>(false, default, new[] { Error.Create(code) }, default);
    }

    public static Result<T> Fail<TEnum>(TEnum code, T value) where TEnum : Enum
    {
        return new Result<T>(false, value, new[] { Error.Create(code) }, default);
    }

    public static Result<T> Fail(string message)
    {
        return new Result<T>(false, default, new[] { Error.Create(message) }, default);
    }

    public static Result<T> Fail(string message, T value)
    {
        return new Result<T>(false, value, new[] { Error.Create(message) }, default);
    }

    public static Result<T> Fail<TEnum>(string message, TEnum code) where TEnum : Enum
    {
        return new Result<T>(false, default, new[] { Error.Create(message, code) }, default);
    }

    public static Result<T> Fail<TEnum>(string message, TEnum code, T value) where TEnum : Enum
    {
        return new Result<T>(false, value, new[] { Error.Create(message, code) }, default);
    }

    public static Result<T> Fail<TEnum>(TEnum code, string message) where TEnum : Enum
    {
        return new Result<T>(false, default, new[] { Error.Create(message, code) }, default);
    }

    public static Result<T> Fail<TEnum>(TEnum code, string message, T value) where TEnum : Enum
    {
        return new Result<T>(false, value, new[] { Error.Create(message, code) }, default);
    }

    public static Result<T> Fail(Error error)
    {
        return new Result<T>(false, default, new[] { error }, default);
    }

    public static Result<T> Fail(Error? error)
    {
        if (error.HasValue)
            return new Result<T>(false, default, new[] { error.Value }, default);

        return new Result<T>(false, default, default, default);
    }

    public static Result<T> Fail(Error[]? errors)
    {
        return new Result<T>(false, default, errors, default);
    }

    public static Result<T> Fail(Exception? exception)
    {
        return new Result<T>(false, default, new[] { Error.FromException(exception) }, default);
    }

    public static Result<T> Fail(Exception? exception, T value)
    {
        return new Result<T>(false, value, new[] { Error.FromException(exception) }, default);
    }
}