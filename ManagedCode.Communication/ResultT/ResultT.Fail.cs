using System;
using System.Net;
using ManagedCode.Communication.Results;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    /// <summary>
    ///     Creates a failure with the generic fallback problem.
    /// </summary>
    public static Result<T> Fail() => ResultFactoryBridge<Result<T>>.Fail();

    /// <summary>
    ///     Creates a failure that still carries a value, using the generic fallback problem.
    /// </summary>
    public static Result<T> Fail(T value) => CreateFailed(Problem.GenericError(), value);

    /// <summary>
    ///     Creates a failure carrying the given problem.
    /// </summary>
    public static Result<T> Fail(Problem problem) => CreateFailed(problem);

    /// <summary>
    ///     Creates a failure with the given title and a 500 status.
    /// </summary>
    public static Result<T> Fail(string title) => ResultFactoryBridge<Result<T>>.Fail(title);

    /// <summary>
    ///     Creates a failure with the given title and detail.
    /// </summary>
    public static Result<T> Fail(string title, string detail)
    {
        return ResultFactoryBridge<Result<T>>.Fail(title, detail);
    }

    /// <summary>
    ///     Creates a failure with the given title, detail and status code.
    /// </summary>
    public static Result<T> Fail(string title, string detail, HttpStatusCode status)
    {
        return ResultFactoryBridge<Result<T>>.Fail(title, detail, status);
    }

    /// <summary>
    ///     Creates a failure from an exception. Only its type name and message are kept — pass the exception to telemetry separately if you need the stack trace.
    /// </summary>
    public static Result<T> Fail(Exception exception) => ResultFactoryBridge<Result<T>>.Fail(exception);

    /// <summary>
    ///     Creates a failure from an exception with an explicit status code.
    /// </summary>
    public static Result<T> Fail(Exception exception, HttpStatusCode status)
    {
        return ResultFactoryBridge<Result<T>>.Fail(exception, status);
    }

    /// <summary>
    ///     Creates a validation failure from field/message pairs. The result reports <c>IsInvalid</c>.
    /// </summary>
    public static Result<T> FailValidation(params (string field, string message)[] errors)
    {
        return ResultFactoryBridge<Result<T>>.FailValidation(errors);
    }

    /// <summary>
    ///     Creates a 400 Bad Request failure.
    /// </summary>
    public static Result<T> FailBadRequest(string? detail = null)
    {
        return ResultFactoryBridge<Result<T>>.FailBadRequest(detail);
    }

    /// <summary>
    ///     Creates a 401 Unauthorized failure.
    /// </summary>
    public static Result<T> FailUnauthorized(string? detail = null)
    {
        return ResultFactoryBridge<Result<T>>.FailUnauthorized(detail);
    }

    /// <summary>
    ///     Creates a 403 Forbidden failure.
    /// </summary>
    public static Result<T> FailForbidden(string? detail = null)
    {
        return ResultFactoryBridge<Result<T>>.FailForbidden(detail);
    }

    /// <summary>
    ///     Creates a 404 Not Found failure.
    /// </summary>
    public static Result<T> FailNotFound(string? detail = null)
    {
        return ResultFactoryBridge<Result<T>>.FailNotFound(detail);
    }

    /// <summary>
    ///     Creates a failure identified by an enum error code, stored in the problem's <c>errorCode</c> extension.
    /// </summary>
    public static Result<T> Fail<TEnum>(TEnum errorCode) where TEnum : Enum
    {
        return ResultFactoryBridge<Result<T>>.Fail(errorCode);
    }

    /// <summary>
    ///     Creates a failure identified by an enum error code, with a detail message.
    /// </summary>
    public static Result<T> Fail<TEnum>(TEnum errorCode, string detail) where TEnum : Enum
    {
        return ResultFactoryBridge<Result<T>>.Fail(errorCode, detail);
    }

    /// <summary>
    ///     Creates a failure identified by an enum error code, with an explicit status code.
    /// </summary>
    public static Result<T> Fail<TEnum>(TEnum errorCode, HttpStatusCode status) where TEnum : Enum
    {
        return ResultFactoryBridge<Result<T>>.Fail(errorCode, status);
    }

    /// <summary>
    ///     Creates a failure identified by an enum error code, with a detail message and status code.
    /// </summary>
    public static Result<T> Fail<TEnum>(TEnum errorCode, string detail, HttpStatusCode status) where TEnum : Enum
    {
        return ResultFactoryBridge<Result<T>>.Fail(errorCode, detail, status);
    }
}
