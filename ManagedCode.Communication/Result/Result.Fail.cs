using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManagedCode.Communication;

public partial class Result
{
    public static Result Fail()
    {
        return new Result(false);
    }

    public static Result Fail(Error error)
    {
        return new Result(error);
    }

    public static Result Fail(List<Error> errors)
    {
        return new Result(errors);
    }

    public static Result Fail(Exception? exception)
    {
        return new Result(Error.FromException(exception));
    }
}