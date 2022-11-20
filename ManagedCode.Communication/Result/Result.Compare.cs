using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManagedCode.Communication;

public partial class Result
{
    public static bool operator ==(Result obj1, bool obj2)
    {
        return obj1.IsSuccess == obj2;
    }

    public static bool operator !=(Result obj1, bool obj2)
    {
        return obj1.IsSuccess != obj2;
    }
    
    public static implicit operator bool(Result result)
    {
        return result.IsSuccess;
    }
    
    public static implicit operator Exception?(Result result)
    {
        return result.Error?.Exception;
    }
    
    public static implicit operator Result(Error error)
    {
        return new Result(error);
    }

    public static implicit operator Result(List<Error> errors)
    {
        return new Result(errors);
    }

    public static implicit operator Result(Exception? exception)
    {
        return new Result(Error.FromException(exception));
    }
}