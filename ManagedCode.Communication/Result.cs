using System;
using System.Collections.Generic;

namespace ManagedCode.Communication;

public class Result : BaseResult<ErrorCode>
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
        return new(error);
    }

    public static implicit operator Result(List<Error<ErrorCode>> errors)
    {
        return new(errors);
    }

    public static implicit operator Result(Exception exception)
    {
        return new(Error<ErrorCode>.FromException(exception));
    }

    public static Result Succeed()
    {
        return new(true);
    }

    public static Result Fail()
    {
        return new(false);
    }

    public static Result Fail(Error<ErrorCode> error)
    {
        return new(error);
    }

    public static Result Fail(List<Error<ErrorCode>> errors)
    {
        return new(errors);
    }

    public static Result Fail(Exception exception)
    {
        return new(Error<ErrorCode>.FromException(exception));
    }
}

public class Result<T> : BaseResult<T, ErrorCode>
{
    internal Result(T value) : base(value)
    {
    }

    internal Result(bool isSuccess) : base(isSuccess)
    {
    }

    internal Result(Error<ErrorCode> error) : base(error)
    {
    }

    internal Result(List<Error<ErrorCode>> errors) : base(errors)
    {
    }

    public static implicit operator Result<T>(T value)
    {
        return new(value);
    }

    public static implicit operator Result<T>(Error<ErrorCode> error)
    {
        return new(error);
    }

    public static implicit operator Result<T>(List<Error<ErrorCode>> errors)
    {
        return new(errors);
    }

    public static implicit operator Result<T>(Exception exception)
    {
        return new(Error<ErrorCode>.FromException(exception));
    }

    public static Result<T> Succeed(T value)
    {
        return new(value);
    }

    public static Result<T> Fail()
    {
        return new(false);
    }

    public static Result<T> Fail(Error<ErrorCode> error)
    {
        return new(error);
    }

    public static Result<T> Fail(List<Error<ErrorCode>> errors)
    {
        return new(errors);
    }

    public static Result<T> Fail(Exception exception)
    {
        return new(Error<ErrorCode>.FromException(exception));
    }

    public Result<T> WithError(Error<ErrorCode> error)
    {
        if (IsSuccess)
        {
            throw new InvalidOperationException("Cannot add error to success result");
        }

        Errors!.Add(error);
        return this;
    }
}