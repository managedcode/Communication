using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManagedCode.Communication;

public sealed partial class Result<T> : BaseResult<T, ErrorCode>
{
    internal Result(T value) : base(value)
    {
    }

    internal Result(bool isSuccess) : base(isSuccess)
    {
    }

    internal Result(bool isSuccess, T value) : base(isSuccess, value)
    {
    }

    internal Result(Error<ErrorCode> error) : base(error)
    {
    }

    internal Result(Error<ErrorCode> error, T value) : base(error, value)
    {
    }

    internal Result(List<Error<ErrorCode>> errors) : base(errors)
    {
    }

    internal Result(List<Error<ErrorCode>> errors, T value) : base(errors, value)
    {
    }

    public Result(bool isSuccess, List<Error<ErrorCode>> errors, T value) : base(isSuccess, errors, value)
    {
    }
    
    public Task<Result<T>> AsTask()
    {
        return Task.FromResult(this);
    }
    
    
#if NET6_0_OR_GREATER
    
    public ValueTask<Result<T>> AsValueTask()
    {
        return ValueTask.FromResult(this);
    }

    
#endif

    public static implicit operator Result<T>(T value)
    {
        return new Result<T>(value);
    }

    public static implicit operator Result<T>(Error<ErrorCode> error)
    {
        return new Result<T>(error);
    }

    public static implicit operator Result<T>(List<Error<ErrorCode>> errors)
    {
        return new Result<T>(errors);
    }

    public static implicit operator Result<T>(Exception? exception)
    {
        return new Result<T>(Error<ErrorCode>.FromException(exception));
    }

    public static Result<T> Succeed(T value)
    {
        return new Result<T>(value);
    }
}