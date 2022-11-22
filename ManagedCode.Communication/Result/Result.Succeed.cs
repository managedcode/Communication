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
        return new Result(true, null);
    }
}