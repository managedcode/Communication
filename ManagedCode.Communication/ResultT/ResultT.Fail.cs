using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    public static Result<T> Fail()
    {
        return new Result<T>(false);
    }
    
    public static Result<T> Fail(Enum code)
    {
        return new Result<T>(false, code);
    }

    public static Result<T> Fail(Error error)
    {
        return new Result<T>(error);
    }

    public static Result<T> Fail(Error[] errors)
    {
        return new Result<T>(errors);
    }

    public static Result<T> Fail(Exception? exception)
    {
        return new Result<T>(Error.FromException(exception));
    }

    public void ThrowException()
    {
        if (IsSuccess)
            return;

        if (Error is { Exception: { } })
        {
            throw Error.Exception;
        }

        throw new Exception(Error?.Message ?? "Result is failed.");
    }
}