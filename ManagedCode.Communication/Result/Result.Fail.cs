using System;
using System.Net;
using ManagedCode.Communication.Results;

namespace ManagedCode.Communication;

public partial struct Result
{
    public static Result Fail() => ResultFactoryBridge<Result>.Fail();

    public static Result Fail(Problem problem) => CreateFailed(problem);

    public static Result Fail(string title) => ResultFactoryBridge<Result>.Fail(title);

    public static Result Fail(string title, string detail) => ResultFactoryBridge<Result>.Fail(title, detail);

    public static Result Fail(string title, string detail, HttpStatusCode status)
    {
        return ResultFactoryBridge<Result>.Fail(title, detail, status);
    }

    public static Result Fail(Exception exception) => ResultFactoryBridge<Result>.Fail(exception);

    public static Result Fail(Exception exception, HttpStatusCode status)
    {
        return ResultFactoryBridge<Result>.Fail(exception, status);
    }

    public static Result FailValidation(params (string field, string message)[] errors)
    {
        return ResultFactoryBridge<Result>.FailValidation(errors);
    }

    public static Result FailBadRequest(string? detail = null) => ResultFactoryBridge<Result>.FailBadRequest(detail);

    public static Result FailUnauthorized(string? detail = null) => ResultFactoryBridge<Result>.FailUnauthorized(detail);

    public static Result FailForbidden(string? detail = null) => ResultFactoryBridge<Result>.FailForbidden(detail);

    public static Result FailNotFound(string? detail = null) => ResultFactoryBridge<Result>.FailNotFound(detail);

    public static Result Fail<TEnum>(TEnum errorCode) where TEnum : Enum => ResultFactoryBridge<Result>.Fail(errorCode);

    public static Result Fail<TEnum>(TEnum errorCode, string detail) where TEnum : Enum
    {
        return ResultFactoryBridge<Result>.Fail(errorCode, detail);
    }

    public static Result Fail<TEnum>(TEnum errorCode, HttpStatusCode status) where TEnum : Enum
    {
        return ResultFactoryBridge<Result>.Fail(errorCode, status);
    }

    public static Result Fail<TEnum>(TEnum errorCode, string detail, HttpStatusCode status) where TEnum : Enum
    {
        return ResultFactoryBridge<Result>.Fail(errorCode, detail, status);
    }
}
