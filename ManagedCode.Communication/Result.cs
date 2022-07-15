using System;

namespace ManagedCode.Communication;

public class Result
{
    public Result(bool isSucceeded, ResultState status, Exception? error)
    {
        IsSucceeded = isSucceeded;
        Status = status;
        Error = error;
    }

    public Result(bool isSucceeded, ResultState status)
    {
        IsSucceeded = isSucceeded;
        Status = status;
        Error = null;
    }

    public Result(Exception? error, ResultState status)
    {
        IsSucceeded = false;
        Error = error;
        Status = status;
    }

    public bool IsSucceeded { get; }
    public bool IsError => !IsSucceeded;
    public Exception? Error { get; }
    public ResultState Status { get; }

    public static Result Succeeded(ResultState status = ResultState.Success)
    {
        return new Result(true, status);
    }

    public static Result Failed(ResultState status, Exception? error = null)
    {
        return new Result(error, status);
    }

    public static Result Failed(Exception? error, ResultState status = ResultState.Failed)
    {
        return new Result(error, status);
    }

    public static Result Failed()
    {
        return new Result(null, ResultState.Failed);
    }
}