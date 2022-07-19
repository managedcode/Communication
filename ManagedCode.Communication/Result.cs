using System;

namespace ManagedCode.Communication;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsError => !IsSuccess;
    public Exception? Error { get; }

    public Result(bool isSuccess, Exception? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public Result(bool isSuccess)
    {
        IsSuccess = isSuccess;
        Error = null;
    }

    public Result(Exception? error)
    {
        IsSuccess = false;
        Error = error;
    }

    public static Result Succeed()
    {
        return new Result(true);
    }

    public static Result<T> Succeed<T>(T result)
    {
        return new Result<T>(true, result);
    }

    public static Result Fail(Exception? error)
    {
        return new Result(error);
    }

    public static Result<T> Fail<T>(T result, Exception? error)
    {
        return new Result<T>(true, result);
    }

    public static Result Fail()
    {
        return new Result(null);
    }
}