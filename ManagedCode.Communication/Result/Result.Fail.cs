using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManagedCode.Communication;

public partial struct Result
{
    public static Result Fail()
    {
        return new Result(false);
    }
    
    public static Result Fail(Enum code)
    {
        return new Result(false, code);
    }

    public static Result Fail(Error error)
    {
        return new Result(error);
    }

    public static Result Fail(Error[] errors)
    {
        return new Result(errors);
    }

    public static Result Fail(Exception? exception)
    {
        return new Result(Error.FromException(exception));
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