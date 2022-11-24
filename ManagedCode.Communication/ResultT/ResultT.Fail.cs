using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    public static Result<T> Fail()
    {
        return new Result<T>(false, default, null);
    }
    
    public static Result<T> Fail(Enum code)
    {
        return new Result<T>(false, default,  null);
    }

    public static Result<T> Fail(Error error)
    {
        return new Result<T>(false, default, new[] {error});
    }

    public static Result<T> Fail(Error[]? errors)
    {
        return new Result<T>(false, default, errors);
    }

    public static Result<T> Fail(Exception? exception)
    {
        return new Result<T>(false, default, new[] {Error.FromException(exception)});
    }

    public void ThrowExceptionIfFailed()
    {
        if (IsSuccess)
            return;

        if (GetError() is { Exception: { } })
        {
            throw GetError().Value.Exception;
        }

        throw new Exception(GetError().Value.Message ?? nameof(HttpStatusCode.InternalServerError));
    }
}