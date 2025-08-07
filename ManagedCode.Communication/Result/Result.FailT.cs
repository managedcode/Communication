using System;

namespace ManagedCode.Communication;

public partial struct Result
{
    public static Result<T> Fail<T>()
    {
        return Result<T>.Fail();
    }

    public static Result<T> Fail<T>(string message)
    {
        return Result<T>.Fail(message);
    }

    public static Result<T> Fail<T>(Problem problem)
    {
        return Result<T>.Fail(problem);
    }

    public static Result<T> Fail<T, TEnum>(TEnum code) where TEnum : Enum
    {
        return Result<T>.Fail(code);
    }

    public static Result<T> Fail<T, TEnum>(TEnum code, string detail) where TEnum : Enum
    {
        return Result<T>.Fail(code, detail);
    }

    public static Result<T> Fail<T>(Exception exception)
    {
        return Result<T>.Fail(exception);
    }

    public static Result<T> FailValidation<T>(params (string field, string message)[] errors)
    {
        return Result<T>.FailValidation(errors);
    }

    public static Result<T> FailUnauthorized<T>()
    {
        return Result<T>.FailUnauthorized();
    }
    
    public static Result<T> FailUnauthorized<T>(string detail)
    {
        return Result<T>.FailUnauthorized(detail);
    }

    public static Result<T> FailForbidden<T>()
    {
        return Result<T>.FailForbidden();
    }
    
    public static Result<T> FailForbidden<T>(string detail)
    {
        return Result<T>.FailForbidden(detail);
    }

    public static Result<T> FailNotFound<T>()
    {
        return Result<T>.FailNotFound();
    }
    
    public static Result<T> FailNotFound<T>(string detail)
    {
        return Result<T>.FailNotFound(detail);
    }
}