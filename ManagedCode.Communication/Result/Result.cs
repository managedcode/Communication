using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManagedCode.Communication.ZALIPA.Result;

public partial class Result
{
    internal Result(bool isSuccess)
    {
        IsSuccess = isSuccess;
    }

    internal Result(Error error)
    {
        IsSuccess = false;
        Errors = new List<Error> { error };
    }

    internal Result(List<Error> errors)
    {
        IsSuccess = false;
        Errors = errors;
    }

    internal Result(bool isSuccess, List<Error> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public bool IsFail => !IsSuccess;
    public Error? Error => Errors?.FirstOrDefault();
    public List<Error>? Errors { get; }
    
    public static bool operator== (Result obj1, bool obj2)
    {
        return obj1.IsSuccess == obj2;
    }
    
    public static bool operator!= (Result obj1, bool obj2)
    {
        return obj1.IsSuccess != obj2;
    }
    
    public Task<Result> AsTask()
    {
        return Task.FromResult(this);
    }
    
    
#if NET6_0_OR_GREATER
    
    public ValueTask<Result> AsValueTask()
    {
        return ValueTask.FromResult(this);
    }

    
#endif

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

    public static Result Succeed()
    {
        return new Result(true);
    }

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