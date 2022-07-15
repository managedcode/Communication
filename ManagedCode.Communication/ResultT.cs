using System;

namespace ManagedCode.Communication;

public class Result<T> : Result
{
    protected Result(Exception? error, ResultState status) : base(error, status)
    {
    }

    public Result(bool isSucceeded, T value, ResultState status, Exception? error) : base(isSucceeded, status, error)
    {
        Value = value;
    }

    public Result(bool isSucceeded, T value, ResultState status) : base(isSucceeded, status)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Succeeded(T content, ResultState status = ResultState.Success)
    {
        return new Result<T>(true, content, status);
    }

    public new static Result<T> Failed(ResultState status, Exception? error = null)
    {
        return new Result<T>(error, status);
    }

    public new static Result<T> Failed(Exception? error, ResultState status = ResultState.Failed)
    {
        return new Result<T>(error, status);
    }

    public new static Result<T> Failed()
    {
        return new Result<T>(null, ResultState.Failed);
    }
}