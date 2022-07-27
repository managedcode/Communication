using System;

namespace ManagedCode.Communication;

public class Result<T> : Result
{
    public T? Value { get; }

    public Result(Exception exception) : base(exception)
    {
    }

    public Result(string errorMessage) : base(errorMessage)
    {
    }

    public Result(bool isSuccess, T value) : base(isSuccess)
    {
        Value = value;
    }

    public Result(bool isSuccess) : base(isSuccess)
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

    public new static Result<T> Fail(Exception exception)
    {
        return new Result<T>(exception);
    }

    public new static Result<T> Fail(string errorMessage)
    {
        return new Result<T>(errorMessage);
    }
}