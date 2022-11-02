using System;
using System.Collections.Generic;

namespace ManagedCode.Communication;

public sealed partial class Result<T> : BaseResult<T, ErrorCode>
{
    public static Result<T> Fail()
    {
        return new Result<T>(false);
    }

    public static Result<T> Fail(T result)
    {
        return new Result<T>(false);
    }

    public static Result<T> Fail(Error<ErrorCode> error)
    {
        return new Result<T>(error);
    }

    public static Result<T> Fail(Error<ErrorCode> error, T value)
    {
        return new Result<T>(error, value);
    }

    public static Result<T> Fail(List<Error<ErrorCode>> errors)
    {
        return new Result<T>(errors);
    }

    public static Result<T> Fail(List<Error<ErrorCode>> errors, T value)
    {
        return new Result<T>(errors, value);
    }

    public static Result<T> Fail(Exception? exception)
    {
        return new Result<T>(Error<ErrorCode>.FromException(exception));
    }

    public static Result<T> Fail(Exception? exception, T value)
    {
        return new Result<T>(Error<ErrorCode>.FromException(exception), value);
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