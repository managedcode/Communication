using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ManagedCode.Communication.CollectionResults.Factories;
using ManagedCode.Communication.Constants;

namespace ManagedCode.Communication.CollectionResultT;

public partial struct CollectionResult<T>
{
    public static CollectionResult<T> Fail()
    {
        return CollectionResultFactory.Failure<T>();
    }

    public static CollectionResult<T> Fail(IEnumerable<T> value)
    {
        return CollectionResultFactory.Failure(value);
    }

    public static CollectionResult<T> Fail(T[] value)
    {
        return CollectionResultFactory.Failure(value);
    }

    public static CollectionResult<T> Fail(Problem problem)
    {
        return CollectionResultFactory.Failure<T>(problem);
    }

    public static CollectionResult<T> Fail(string title)
    {
        return CollectionResultFactory.Failure<T>(title);
    }

    public static CollectionResult<T> Fail(string title, string detail)
    {
        return CollectionResultFactory.Failure<T>(title, detail);
    }

    public static CollectionResult<T> Fail(string title, string detail, HttpStatusCode status)
    {
        return CollectionResultFactory.Failure<T>(title, detail, status);
    }

    public static CollectionResult<T> Fail(Exception exception)
    {
        return CollectionResultFactory.Failure<T>(exception);
    }

    public static CollectionResult<T> Fail(Exception exception, HttpStatusCode status)
    {
        return CollectionResultFactory.Failure<T>(exception, status);
    }

    public static CollectionResult<T> FailValidation(params (string field, string message)[] errors)
    {
        return CollectionResultFactory.FailureValidation<T>(errors);
    }

    public static CollectionResult<T> FailBadRequest()
    {
        return CollectionResultFactory.FailureBadRequest<T>();
    }

    public static CollectionResult<T> FailBadRequest(string detail)
    {
        return CollectionResultFactory.FailureBadRequest<T>(detail);
    }

    public static CollectionResult<T> FailUnauthorized()
    {
        return CollectionResultFactory.FailureUnauthorized<T>();
    }

    public static CollectionResult<T> FailUnauthorized(string detail)
    {
        return CollectionResultFactory.FailureUnauthorized<T>(detail);
    }

    public static CollectionResult<T> FailForbidden()
    {
        return CollectionResultFactory.FailureForbidden<T>();
    }

    public static CollectionResult<T> FailForbidden(string detail)
    {
        return CollectionResultFactory.FailureForbidden<T>(detail);
    }

    public static CollectionResult<T> FailNotFound()
    {
        return CollectionResultFactory.FailureNotFound<T>();
    }

    public static CollectionResult<T> FailNotFound(string detail)
    {
        return CollectionResultFactory.FailureNotFound<T>(detail);
    }

    public static CollectionResult<T> Fail<TEnum>(TEnum errorCode) where TEnum : Enum
    {
        return CollectionResultFactory.Failure<T, TEnum>(errorCode);
    }

    public static CollectionResult<T> Fail<TEnum>(TEnum errorCode, string detail) where TEnum : Enum
    {
        return CollectionResultFactory.Failure<T, TEnum>(errorCode, detail);
    }

    public static CollectionResult<T> Fail<TEnum>(TEnum errorCode, HttpStatusCode status) where TEnum : Enum
    {
        return CollectionResultFactory.Failure<T, TEnum>(errorCode, status);
    }

    public static CollectionResult<T> Fail<TEnum>(TEnum errorCode, string detail, HttpStatusCode status) where TEnum : Enum
    {
        return CollectionResultFactory.Failure<T, TEnum>(errorCode, detail, status);
    }
}
