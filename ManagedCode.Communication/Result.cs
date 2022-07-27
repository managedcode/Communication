using System;

namespace ManagedCode.Communication;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsError => !IsSuccess;
    public Error? Error { get; }

    protected Result(Exception exception)
    {
        IsSuccess = false;
        Error = new Error(exception);
    }

    protected Result(string errorMessage)
    {
        IsSuccess = false;
        Error = new Error(errorMessage);
    }

    protected Result(bool isSuccess)
    {
        IsSuccess = isSuccess;
        Error = null;
    }

    protected Result(Error error)
    {
        IsSuccess = false;
        Error = error;
    }

    public static Result Succeed()
    {
        return new Result(true);
    }

    public static Result Fail()
    {
        return new Result(false);
    }

    public static Result Fail(Error error)
    {
        return new Result(error);
    }

    public static Result Fail(Exception error)
    {
        return new Result(error);
    }

    public static Result Fail(string errorMessage)
    {
        return new Result(errorMessage);
    }

    public static Result<T> Succeed<T>(T content)
    {
        return new Result<T>(true, content);
    }

    public static Result<T> Fail<T>()
    {
        return new Result<T>(false);
    }

    public static Result<T> Fail<T>(Error error)
    {
        return new Result<T>(error);
    }

    public static Result<T> Fail<T>(Exception exception)
    {
        return new Result<T>(exception);
    }

    public static Result<T> Fail<T>(string errorMessage)
    {
        return new Result<T>(errorMessage);
    }
}