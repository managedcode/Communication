using System;
using System.Net;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    public static Result<T> Fail()
    {
        return new Result<T>(false, default, null);
    }

    public static Result<T> Fail<TEnum>(TEnum code) where TEnum : Enum
    {
        return new Result<T>(false, default, new[] { Error.Create(code) });
    }

    public static Result<T> Fail(string message)
    {
        return new Result<T>(false, default, new[] { Error.Create(message) });
    }

    public static Result<T> Fail<TEnum>(string message, TEnum code) where TEnum : Enum
    {
        return new Result<T>(false, default, new[] { Error.Create(message, code) });
    }

    public static Result<T> Fail<TEnum>(TEnum code, string message) where TEnum : Enum
    {
        return new Result<T>(false, default, new[] { Error.Create(message, code) });
    }

    public static Result<T> Fail(Error error)
    {
        return new Result<T>(false, default, new[] { error });
    }

    public static Result<T> Fail(Error[]? errors)
    {
        return new Result<T>(false, default, errors);
    }

    public static Result<T> Fail(Exception? exception)
    {
        return new Result<T>(false, default, new[] { Error.FromException(exception) });
    }

    public void ThrowExceptionIfFailed()
    {
        if (IsSuccess)
        {
            return;
        }

        if (GetError() is { Exception: { } })
        {
            throw GetError().Value.Exception;
        }

        throw new Exception(GetError().Value.Message ?? nameof(HttpStatusCode.InternalServerError));
    }
}