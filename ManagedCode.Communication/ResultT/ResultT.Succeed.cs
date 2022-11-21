using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    public static Result<T> Succeed()
    {
        return new Result<T>(true);
    }
    public static Result<T> Succeed(Enum code)
    {
        return new Result<T>(true, code);
    }
    
    public static Result<T> Succeed(T value)
    {
        return new Result<T>(true, value);
    }
    
    public static Result<T> Succeed(T value, Enum code)
    {
        return new Result<T>(true,code, value);
    }
}