using System;
using System.Collections.Generic;

namespace ManagedCode.Communication;

public sealed partial class Result : BaseResult<ErrorCode>
{
    internal Result(bool isSuccess) : base(isSuccess)
    {
    }

    internal Result(Error<ErrorCode> error) : base(error)
    {
    }

    internal Result(List<Error<ErrorCode>> errors) : base(errors)
    {
    }

    public static implicit operator Result(Error<ErrorCode> error)
    {
        return new Result(error);
    }

    public static implicit operator Result(List<Error<ErrorCode>> errors)
    {
        return new Result(errors);
    }

    public static implicit operator Result(Exception? exception)
    {
        return new Result(Error<ErrorCode>.FromException(exception));
    }

    public static Result Succeed()
    {
        return new Result(true);
    }

    public static Result Fail()
    {
        return new Result(false);
    }

    public static Result Fail(Error<ErrorCode> error)
    {
        return new Result(error);
    }

    public static Result Fail(List<Error<ErrorCode>> errors)
    {
        return new Result(errors);
    }

    public static Result Fail(Exception? exception)
    {
        return new Result(Error<ErrorCode>.FromException(exception));
    }
}

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