using System;
using ManagedCode.Communication.Results;

namespace ManagedCode.Communication;

public partial struct Result
{
    public static Result<T> Fail<T>() => ResultFactoryBridge<Result<T>>.Fail();

    public static Result<T> Fail<T>(string message) => ResultFactoryBridge<Result<T>>.Fail(message);

    public static Result<T> Fail<T>(Problem problem) => Result<T>.CreateFailed(problem);

    public static Result<T> Fail<T, TEnum>(TEnum code) where TEnum : Enum => ResultFactoryBridge<Result<T>>.Fail(code);

    public static Result<T> Fail<T, TEnum>(TEnum code, string detail) where TEnum : Enum
    {
        return ResultFactoryBridge<Result<T>>.Fail(code, detail);
    }

    public static Result<T> Fail<T>(Exception exception) => ResultFactoryBridge<Result<T>>.Fail(exception);

    public static Result<T> FailValidation<T>(params (string field, string message)[] errors)
    {
        return ResultFactoryBridge<Result<T>>.FailValidation(errors);
    }

    public static Result<T> FailUnauthorized<T>(string? detail = null)
    {
        return ResultFactoryBridge<Result<T>>.FailUnauthorized(detail);
    }

    public static Result<T> FailForbidden<T>(string? detail = null)
    {
        return ResultFactoryBridge<Result<T>>.FailForbidden(detail);
    }

    public static Result<T> FailNotFound<T>(string? detail = null)
    {
        return ResultFactoryBridge<Result<T>>.FailNotFound(detail);
    }
}
