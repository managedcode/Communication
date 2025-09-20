using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ManagedCode.Communication.Results;

namespace ManagedCode.Communication.CollectionResultT;

public partial struct CollectionResult<T>
{
    public static CollectionResult<T> Fail() => ResultFactoryBridge<CollectionResult<T>>.Fail();

    public static CollectionResult<T> Fail(IEnumerable<T> value)
    {
        var array = value as T[] ?? value.ToArray();
        return CollectionResultFactoryBridge<CollectionResult<T>, T>.Fail(array);
    }

    public static CollectionResult<T> Fail(T[] value) => CollectionResultFactoryBridge<CollectionResult<T>, T>.Fail(value);

    public static CollectionResult<T> Fail(Problem problem) => CollectionResult<T>.CreateFailed(problem);

    public static CollectionResult<T> Fail(Problem problem, T[] items)
    {
        return CollectionResult<T>.CreateFailed(problem, items);
    }

    public static CollectionResult<T> Fail(string title) => ResultFactoryBridge<CollectionResult<T>>.Fail(title);

    public static CollectionResult<T> Fail(string title, string detail)
    {
        return ResultFactoryBridge<CollectionResult<T>>.Fail(title, detail);
    }

    public static CollectionResult<T> Fail(string title, string detail, HttpStatusCode status)
    {
        return ResultFactoryBridge<CollectionResult<T>>.Fail(title, detail, status);
    }

    public static CollectionResult<T> Fail(Exception exception)
    {
        return ResultFactoryBridge<CollectionResult<T>>.Fail(exception);
    }

    public static CollectionResult<T> Fail(Exception exception, HttpStatusCode status)
    {
        return ResultFactoryBridge<CollectionResult<T>>.Fail(exception, status);
    }

    public static CollectionResult<T> FailValidation(params (string field, string message)[] errors)
    {
        return ResultFactoryBridge<CollectionResult<T>>.FailValidation(errors);
    }

    public static CollectionResult<T> FailBadRequest(string? detail = null)
    {
        return ResultFactoryBridge<CollectionResult<T>>.FailBadRequest(detail);
    }

    public static CollectionResult<T> FailUnauthorized(string? detail = null)
    {
        return ResultFactoryBridge<CollectionResult<T>>.FailUnauthorized(detail);
    }

    public static CollectionResult<T> FailForbidden(string? detail = null)
    {
        return ResultFactoryBridge<CollectionResult<T>>.FailForbidden(detail);
    }

    public static CollectionResult<T> FailNotFound(string? detail = null)
    {
        return ResultFactoryBridge<CollectionResult<T>>.FailNotFound(detail);
    }

    public static CollectionResult<T> Fail<TEnum>(TEnum errorCode) where TEnum : Enum
    {
        return ResultFactoryBridge<CollectionResult<T>>.Fail(errorCode);
    }

    public static CollectionResult<T> Fail<TEnum>(TEnum errorCode, string detail) where TEnum : Enum
    {
        return ResultFactoryBridge<CollectionResult<T>>.Fail(errorCode, detail);
    }

    public static CollectionResult<T> Fail<TEnum>(TEnum errorCode, HttpStatusCode status) where TEnum : Enum
    {
        return ResultFactoryBridge<CollectionResult<T>>.Fail(errorCode, status);
    }

    public static CollectionResult<T> Fail<TEnum>(TEnum errorCode, string detail, HttpStatusCode status) where TEnum : Enum
    {
        return ResultFactoryBridge<CollectionResult<T>>.Fail(errorCode, detail, status);
    }
}
