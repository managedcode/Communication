using System;
using ManagedCode.Communication.Results.Factories;

namespace ManagedCode.Communication;

public partial struct Result
{
    public static Result<T> Fail<T>()
    {
        return ResultFactory.Failure<T>();
    }

    public static Result<T> Fail<T>(string message)
    {
        return ResultFactory.Failure<T>(message);
    }

    public static Result<T> Fail<T>(Problem problem)
    {
        return ResultFactory.Failure<T>(problem);
    }

    public static Result<T> Fail<T, TEnum>(TEnum code) where TEnum : Enum
    {
        return ResultFactory.Failure<T, TEnum>(code);
    }

    public static Result<T> Fail<T, TEnum>(TEnum code, string detail) where TEnum : Enum
    {
        return ResultFactory.Failure<T, TEnum>(code, detail);
    }

    public static Result<T> Fail<T>(Exception exception)
    {
        return ResultFactory.Failure<T>(exception);
    }

    public static Result<T> FailValidation<T>(params (string field, string message)[] errors)
    {
        return ResultFactory.FailureValidation<T>(errors);
    }

    public static Result<T> FailUnauthorized<T>()
    {
        return ResultFactory.FailureUnauthorized<T>();
    }
    
    public static Result<T> FailUnauthorized<T>(string detail)
    {
        return ResultFactory.FailureUnauthorized<T>(detail);
    }

    public static Result<T> FailForbidden<T>()
    {
        return ResultFactory.FailureForbidden<T>();
    }
    
    public static Result<T> FailForbidden<T>(string detail)
    {
        return ResultFactory.FailureForbidden<T>(detail);
    }

    public static Result<T> FailNotFound<T>()
    {
        return ResultFactory.FailureNotFound<T>();
    }
    
    public static Result<T> FailNotFound<T>(string detail)
    {
        return ResultFactory.FailureNotFound<T>(detail);
    }
}
