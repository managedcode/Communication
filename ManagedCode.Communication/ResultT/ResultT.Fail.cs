using System;
using System.Collections.Generic;
using System.Linq;

namespace ManagedCode.Communication;

public sealed partial class Result<T>
{
    public static Result<T> Fail()
    {
        return new Result<T>(false);
    }

    public static Result<T> Fail(T result)
    {
        return new Result<T>(false);
    }

    public static Result<T> Fail(Error error)
    {
        return new Result<T>(error);
    }

    public static Result<T> Fail(Error error, T value)
    {
        return new Result<T>(error, value);
    }

    public static Result<T> Fail(Error[] errors)
    {
        return new Result<T>(errors);
    }

    public static Result<T> Fail(Error[] errors, T value)
    {
        return new Result<T>(errors, value);
    }

    public static Result<T> Fail(Exception? exception)
    {
        return new Result<T>(Error.FromException(exception));
    }

    public static Result<T> Fail(Exception? exception, T value)
    {
        return new Result<T>(Error.FromException(exception), value);
    }

    public Result<T> WithError(Error error)
    {
        if (IsSuccess)
        {
            throw new InvalidOperationException("Cannot add error to success result");
        }

        Errors = Errors != null
            ? Errors.Concat(new[] { error }).ToArray() 
            : new[] { error };

        return this;
    }
}