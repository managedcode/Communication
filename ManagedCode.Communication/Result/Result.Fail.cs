using System;
using System.Net;

namespace ManagedCode.Communication;

public partial struct Result
{
    public static Result Fail()
    {
        return new Result(false, null);
    }

    public static Result Fail(string message)
    {
        return new Result(false, new[] { Error.Create(message) });
    }

    public static Result Fail<TEnum>(TEnum code) where TEnum : Enum
    {
        return new Result(false, new[] { Error.Create(code) });
    }

    public static Result Fail<TEnum>(string message, TEnum code) where TEnum : Enum
    {
        return new Result(false, new[] { Error.Create(message, code) });
    }

    public static Result Fail<TEnum>(TEnum code, string message) where TEnum : Enum
    {
        return new Result(false, new[] { Error.Create(message, code) });
    }

    public static Result Fail(Error error)
    {
        return new Result(false, new[] { error });
    }
    
    public static Result Fail(Error[] errors)
    {
        return new Result(false, errors);
    }

    public static Result Fail(Exception? exception)
    {
        return new Result(false, new[] { Error.FromException(exception) });
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