using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ManagedCode.Communication;

public partial struct Result
{
    public static Result Fail()
    {
        return new Result(false, Enum.GetName(typeof(HttpStatusCode),HttpStatusCode.InternalServerError), null);
    }
    
    public static Result Fail(Enum code)
    {
        return new Result(false,  Enum.GetName(code.GetType(),code), null);
    }

    public static Result Fail(Error error)
    {
        return new Result(false,  Enum.GetName(typeof(HttpStatusCode),HttpStatusCode.InternalServerError), new[] {error});
    }

    public static Result Fail(Error[] errors)
    {
        return new Result(false,  Enum.GetName(typeof(HttpStatusCode),HttpStatusCode.InternalServerError), errors);
    }

    public static Result Fail(Exception? exception)
    {
        return new Result(false, Enum.GetName(typeof(HttpStatusCode),HttpStatusCode.InternalServerError), 
            new[] {Error.FromException(exception)});
    }

    public void ThrowException()
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