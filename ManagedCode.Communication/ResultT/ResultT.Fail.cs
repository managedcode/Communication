using System;
using System.Net;
using ManagedCode.Communication.Results;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    public static Result<T> Fail() => ResultFactoryBridge<Result<T>>.Fail();

    public static Result<T> Fail(T value) => CreateFailed(Problem.GenericError(), value);

    public static Result<T> Fail(Problem problem) => CreateFailed(problem);

    public static Result<T> Fail(string title) => ResultFactoryBridge<Result<T>>.Fail(title);

    public static Result<T> Fail(string title, string detail)
    {
        return ResultFactoryBridge<Result<T>>.Fail(title, detail);
    }

    public static Result<T> Fail(string title, string detail, HttpStatusCode status)
    {
        return ResultFactoryBridge<Result<T>>.Fail(title, detail, status);
    }

    public static Result<T> Fail(Exception exception) => ResultFactoryBridge<Result<T>>.Fail(exception);

    public static Result<T> Fail(Exception exception, HttpStatusCode status)
    {
        return ResultFactoryBridge<Result<T>>.Fail(exception, status);
    }

    public static Result<T> FailValidation(params (string field, string message)[] errors)
    {
        return ResultFactoryBridge<Result<T>>.FailValidation(errors);
    }

    public static Result<T> FailBadRequest(string? detail = null)
    {
        return ResultFactoryBridge<Result<T>>.FailBadRequest(detail);
    }

    public static Result<T> FailUnauthorized(string? detail = null)
    {
        return ResultFactoryBridge<Result<T>>.FailUnauthorized(detail);
    }

    public static Result<T> FailForbidden(string? detail = null)
    {
        return ResultFactoryBridge<Result<T>>.FailForbidden(detail);
    }

    public static Result<T> FailNotFound(string? detail = null)
    {
        return ResultFactoryBridge<Result<T>>.FailNotFound(detail);
    }

    public static Result<T> Fail<TEnum>(TEnum errorCode) where TEnum : Enum
    {
        return ResultFactoryBridge<Result<T>>.Fail(errorCode);
    }

    public static Result<T> Fail<TEnum>(TEnum errorCode, string detail) where TEnum : Enum
    {
        return ResultFactoryBridge<Result<T>>.Fail(errorCode, detail);
    }

    public static Result<T> Fail<TEnum>(TEnum errorCode, HttpStatusCode status) where TEnum : Enum
    {
        return ResultFactoryBridge<Result<T>>.Fail(errorCode, status);
    }

    public static Result<T> Fail<TEnum>(TEnum errorCode, string detail, HttpStatusCode status) where TEnum : Enum
    {
        return ResultFactoryBridge<Result<T>>.Fail(errorCode, detail, status);
    }
}
