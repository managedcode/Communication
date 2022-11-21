using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ManagedCode.Communication;

public partial struct Result
{
    public static Result Succeed()
    {
        return new Result(true, Enum.GetName(typeof(HttpStatusCode),HttpStatusCode.OK), null);
    }
    public static Result Succeed(Enum code)
    {
        return new Result(true, Enum.GetName(code.GetType(),code), null);
    }
}