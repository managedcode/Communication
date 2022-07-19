using System;

namespace ManagedCode.Communication;

public class Result<T> : Result
{
    public T? Value { get; }

    public Result(Exception? error) : base(error)
    {
    }

    public Result(bool isSuccess, T value, Exception? error) : base(isSuccess, error)
    {
        Value = value;
    }

    public Result(bool isSuccess, T value) : base(isSuccess)
    {
        Value = value;
    }

    public static Result<T> Succeed(T content)
    {
        return new Result<T>(true, content);
    }

    public new static Result<T> Fail()
    {
        return new Result<T>(null);
    }

    public new static Result<T> Fail(Exception? error)
    {
        return new Result<T>(error);
    }
}