using System;

namespace ManagedCode.Communication;

public partial struct Result
{
    public static Result<T> Fail<T>()
    {
        return Result<T>.Fail();
    }

    public static Result<T> Fail<T>(string message)
    {
        return Result<T>.Fail(message);
    }

    public static Result<T> Fail<T, TEnum>(TEnum code) where TEnum : Enum
    {
        return Result<T>.Fail(code);
    }

    public static Result<T> Fail<T, TEnum>(TEnum code, T value) where TEnum : Enum
    {
        return Result<T>.Fail(code, value);
    }

    public static Result<T> Fail<T, TEnum>(string message, TEnum code) where TEnum : Enum
    {
        return Result<T>.Fail(message, code);
    }

    public static Result<T> Fail<T, TEnum>(string message, TEnum code, T value) where TEnum : Enum
    {
        return Result<T>.Fail(message, code, value);
    }

    public static Result<T> Fail<T, TEnum>(TEnum code, string message) where TEnum : Enum
    {
        return Result<T>.Fail(code, message);
    }

    public static Result<T> Fail<T, TEnum>(TEnum code, string message, T value) where TEnum : Enum
    {
        return Result<T>.Fail(code, message, value);
    }

    public static Result<T> Fail<T>(Error error)
    {
        return Result<T>.Fail(error);
    }

    public static Result<T> Fail<T>(Error? error)
    {
        if (error.HasValue)
            return new Result<T>(false, default, new[] { error.Value }, default);

        return new Result<T>(false, default, default, default);
    }

    public static Result<T> Fail<T>(Error[] errors)
    {
        return Result<T>.Fail(errors);
    }

    public static Result<T> Fail<T>(Exception? exception)
    {
        return Result<T>.Fail(exception);
    }

    public static Result<T> Fail<T>(Exception? exception, T value)
    {
        return Result<T>.Fail(exception, value);
    }
}