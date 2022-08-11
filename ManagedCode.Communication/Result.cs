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


    public static implicit operator Result(Error<ErrorCode> error) => new(error);
    public static implicit operator Result(List<Error<ErrorCode>> errors) => new(errors);
    public static implicit operator Result(Exception exception) => new(Error<ErrorCode>.FromException(exception));


    public static Result Succeed() => new(true);
    public static Result Fail() => new(false);
    public static Result Fail(Error<ErrorCode> error) => new(error);
    public static Result Fail(List<Error<ErrorCode>> errors) => new(errors);
    public static Result Fail(Exception exception) => new(Error<ErrorCode>.FromException(exception));
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


    public static implicit operator Result<T>(T value) => new(value);
    public static implicit operator Result<T>(Error<ErrorCode> error) => new(error);
    public static implicit operator Result<T>(List<Error<ErrorCode>> errors) => new(errors);
    public static implicit operator Result<T>(Exception exception) => new(Error<ErrorCode>.FromException(exception));


    public static Result<T> Succeed(T value) => new(value);
    public static Result<T> Fail() => new(false);
    public static Result<T> Fail(Error<ErrorCode> error) => new(error);
    public static Result<T> Fail(List<Error<ErrorCode>> errors) => new(errors);
    public static Result<T> Fail(Exception exception) => new(Error<ErrorCode>.FromException(exception));


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