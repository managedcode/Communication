using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManagedCode.Communication;

public partial class Result
{
    public static Result Succeed()
    {
        return new Result(true);
    }
    public static Result Succeed(Enum code)
    {
        return new Result(true);
    }
}