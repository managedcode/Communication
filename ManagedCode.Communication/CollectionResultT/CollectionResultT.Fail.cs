using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ManagedCode.Communication.Results;

namespace ManagedCode.Communication.CollectionResultT;

public partial struct CollectionResult<T>
{
    /// <summary>
    ///     Creates a failure with the generic fallback problem.
    /// </summary>
    public static CollectionResult<T> Fail() => ResultFactoryBridge<CollectionResult<T>>.Fail();

    /// <summary>
    ///     Creates a failure that still carries items.
    /// </summary>
    public static CollectionResult<T> Fail(IEnumerable<T> value)
    {
        var array = value as T[] ?? value.ToArray();
        return CollectionResultFactoryBridge<CollectionResult<T>, T>.Fail(array);
    }

    /// <summary>
    ///     Creates a failure that still carries items.
    /// </summary>
    public static CollectionResult<T> Fail(T[] value) => CollectionResultFactoryBridge<CollectionResult<T>, T>.Fail(value);

    /// <summary>
    ///     Creates a failure carrying the given problem.
    /// </summary>
    public static CollectionResult<T> Fail(Problem problem) => CollectionResult<T>.CreateFailed(problem);

    /// <summary>
    ///     Creates a failure carrying both a problem and items.
    /// </summary>
    public static CollectionResult<T> Fail(Problem problem, T[] items)
    {
        return CollectionResult<T>.CreateFailed(problem, items);
    }

    /// <summary>
    ///     Creates a failure with the given title and a 500 status.
    /// </summary>
    public static CollectionResult<T> Fail(string title) => ResultFactoryBridge<CollectionResult<T>>.Fail(title);

    /// <summary>
    ///     Creates a failure with the given title and detail.
    /// </summary>
    public static CollectionResult<T> Fail(string title, string detail)
    {
        return ResultFactoryBridge<CollectionResult<T>>.Fail(title, detail);
    }

    /// <summary>
    ///     Creates a failure with the given title, detail and status code.
    /// </summary>
    public static CollectionResult<T> Fail(string title, string detail, HttpStatusCode status)
    {
        return ResultFactoryBridge<CollectionResult<T>>.Fail(title, detail, status);
    }

    /// <summary>
    ///     Creates a failure from an exception. Only its type name and message are kept — pass the exception to telemetry separately if you need the stack trace.
    /// </summary>
    public static CollectionResult<T> Fail(Exception exception)
    {
        return ResultFactoryBridge<CollectionResult<T>>.Fail(exception);
    }

    /// <summary>
    ///     Creates a failure from an exception with an explicit status code.
    /// </summary>
    public static CollectionResult<T> Fail(Exception exception, HttpStatusCode status)
    {
        return ResultFactoryBridge<CollectionResult<T>>.Fail(exception, status);
    }

    /// <summary>
    ///     Creates a validation failure from field/message pairs. The result reports <c>IsInvalid</c>.
    /// </summary>
    public static CollectionResult<T> FailValidation(params (string field, string message)[] errors)
    {
        return ResultFactoryBridge<CollectionResult<T>>.FailValidation(errors);
    }

    /// <summary>
    ///     Creates a 400 Bad Request failure.
    /// </summary>
    public static CollectionResult<T> FailBadRequest(string? detail = null)
    {
        return ResultFactoryBridge<CollectionResult<T>>.FailBadRequest(detail);
    }

    /// <summary>
    ///     Creates a 401 Unauthorized failure.
    /// </summary>
    public static CollectionResult<T> FailUnauthorized(string? detail = null)
    {
        return ResultFactoryBridge<CollectionResult<T>>.FailUnauthorized(detail);
    }

    /// <summary>
    ///     Creates a 403 Forbidden failure.
    /// </summary>
    public static CollectionResult<T> FailForbidden(string? detail = null)
    {
        return ResultFactoryBridge<CollectionResult<T>>.FailForbidden(detail);
    }

    /// <summary>
    ///     Creates a 404 Not Found failure.
    /// </summary>
    public static CollectionResult<T> FailNotFound(string? detail = null)
    {
        return ResultFactoryBridge<CollectionResult<T>>.FailNotFound(detail);
    }

    /// <summary>
    ///     Creates a failure identified by an enum error code, stored in the problem's <c>errorCode</c> extension.
    /// </summary>
    public static CollectionResult<T> Fail<TEnum>(TEnum errorCode) where TEnum : Enum
    {
        return ResultFactoryBridge<CollectionResult<T>>.Fail(errorCode);
    }

    /// <summary>
    ///     Creates a failure identified by an enum error code, with a detail message.
    /// </summary>
    public static CollectionResult<T> Fail<TEnum>(TEnum errorCode, string detail) where TEnum : Enum
    {
        return ResultFactoryBridge<CollectionResult<T>>.Fail(errorCode, detail);
    }

    /// <summary>
    ///     Creates a failure identified by an enum error code, with an explicit status code.
    /// </summary>
    public static CollectionResult<T> Fail<TEnum>(TEnum errorCode, HttpStatusCode status) where TEnum : Enum
    {
        return ResultFactoryBridge<CollectionResult<T>>.Fail(errorCode, status);
    }

    /// <summary>
    ///     Creates a failure identified by an enum error code, with a detail message and status code.
    /// </summary>
    public static CollectionResult<T> Fail<TEnum>(TEnum errorCode, string detail, HttpStatusCode status) where TEnum : Enum
    {
        return ResultFactoryBridge<CollectionResult<T>>.Fail(errorCode, detail, status);
    }
}
