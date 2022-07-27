using System;

namespace ManagedCode.Communication;

public class Result<T> : Result
{
    public T? Value { get; }

    internal Result(Exception exception) : base(exception)
    {
    }

    internal Result(string errorMessage) : base(errorMessage)
    {
    }

    internal Result(bool isSuccess, T value) : base(isSuccess)
    {
        Value = value;
    }

    internal Result(bool isSuccess) : base(isSuccess)
    {
    }

    internal Result(Error error) : base(error)
    {
    }

    public static Result<T> Succeed(T content)
    {
        return new Result<T>(true, content);
    }

    public new static Result<T> Fail()
    {
        return new Result<T>(false);
    }

    public new static Result<T> Fail(Error error)
    {
        return new Result<T>(error);
    }

    public new static Result<T> Fail(Exception exception)
    {
        return new Result<T>(exception);
    }

    public new static Result<T> Fail(string errorMessage)
    {
        return new Result<T>(errorMessage);
    }
}