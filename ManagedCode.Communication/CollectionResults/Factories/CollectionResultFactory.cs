using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ManagedCode.Communication;
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Constants;

namespace ManagedCode.Communication.CollectionResults.Factories;

internal static class CollectionResultFactory
{
    public static CollectionResult<T> Success<T>(T[] items, int pageNumber, int pageSize, int totalItems)
    {
        return CollectionResult<T>.CreateSuccess(items, pageNumber, pageSize, totalItems);
    }

    public static CollectionResult<T> Success<T>(IEnumerable<T> items, int pageNumber, int pageSize, int totalItems)
    {
        var array = items as T[] ?? items.ToArray();
        return CollectionResult<T>.CreateSuccess(array, pageNumber, pageSize, totalItems);
    }

    public static CollectionResult<T> Success<T>(T[] items)
    {
        var length = items.Length;
        return CollectionResult<T>.CreateSuccess(items, 1, length, length);
    }

    public static CollectionResult<T> Success<T>(IEnumerable<T> items)
    {
        var array = items as T[] ?? items.ToArray();
        var length = array.Length;
        return CollectionResult<T>.CreateSuccess(array, 1, length, length);
    }

    public static CollectionResult<T> Empty<T>()
    {
        return CollectionResult<T>.CreateSuccess(Array.Empty<T>(), 0, 0, 0);
    }

    public static CollectionResult<T> Failure<T>()
    {
        return CollectionResult<T>.CreateFailed(Problem.GenericError());
    }

    public static CollectionResult<T> Failure<T>(Problem problem)
    {
        return CollectionResult<T>.CreateFailed(problem);
    }

    public static CollectionResult<T> Failure<T>(IEnumerable<T> items)
    {
        var array = items as T[] ?? items.ToArray();
        return CollectionResult<T>.CreateFailed(Problem.GenericError(), array);
    }

    public static CollectionResult<T> Failure<T>(T[] items)
    {
        return CollectionResult<T>.CreateFailed(Problem.GenericError(), items);
    }

    public static CollectionResult<T> Failure<T>(string title)
    {
        return CollectionResult<T>.CreateFailed(Problem.Create(title, title, (int)HttpStatusCode.InternalServerError));
    }

    public static CollectionResult<T> Failure<T>(string title, string detail)
    {
        return CollectionResult<T>.CreateFailed(Problem.Create(title, detail));
    }

    public static CollectionResult<T> Failure<T>(string title, string detail, HttpStatusCode status)
    {
        return CollectionResult<T>.CreateFailed(Problem.Create(title, detail, (int)status));
    }

    public static CollectionResult<T> Failure<T>(Exception exception)
    {
        return CollectionResult<T>.CreateFailed(Problem.Create(exception, (int)HttpStatusCode.InternalServerError));
    }

    public static CollectionResult<T> Failure<T>(Exception exception, HttpStatusCode status)
    {
        return CollectionResult<T>.CreateFailed(Problem.Create(exception, (int)status));
    }

    public static CollectionResult<T> FailureValidation<T>(params (string field, string message)[] errors)
    {
        return CollectionResult<T>.CreateFailed(Problem.Validation(errors));
    }

    public static CollectionResult<T> FailureBadRequest<T>(string? detail = null)
    {
        return CollectionResult<T>.CreateFailed(Problem.Create(
            ProblemConstants.Titles.BadRequest,
            detail ?? ProblemConstants.Messages.BadRequest,
            (int)HttpStatusCode.BadRequest));
    }

    public static CollectionResult<T> FailureUnauthorized<T>(string? detail = null)
    {
        return CollectionResult<T>.CreateFailed(Problem.Create(
            ProblemConstants.Titles.Unauthorized,
            detail ?? ProblemConstants.Messages.UnauthorizedAccess,
            (int)HttpStatusCode.Unauthorized));
    }

    public static CollectionResult<T> FailureForbidden<T>(string? detail = null)
    {
        return CollectionResult<T>.CreateFailed(Problem.Create(
            ProblemConstants.Titles.Forbidden,
            detail ?? ProblemConstants.Messages.ForbiddenAccess,
            (int)HttpStatusCode.Forbidden));
    }

    public static CollectionResult<T> FailureNotFound<T>(string? detail = null)
    {
        return CollectionResult<T>.CreateFailed(Problem.Create(
            ProblemConstants.Titles.NotFound,
            detail ?? ProblemConstants.Messages.ResourceNotFound,
            (int)HttpStatusCode.NotFound));
    }

    public static CollectionResult<T> Failure<T, TEnum>(TEnum errorCode) where TEnum : Enum
    {
        return CollectionResult<T>.CreateFailed(Problem.Create(errorCode));
    }

    public static CollectionResult<T> Failure<T, TEnum>(TEnum errorCode, string detail) where TEnum : Enum
    {
        return CollectionResult<T>.CreateFailed(Problem.Create(errorCode, detail));
    }

    public static CollectionResult<T> Failure<T, TEnum>(TEnum errorCode, HttpStatusCode status) where TEnum : Enum
    {
        return CollectionResult<T>.CreateFailed(Problem.Create(errorCode, errorCode.ToString(), (int)status));
    }

    public static CollectionResult<T> Failure<T, TEnum>(TEnum errorCode, string detail, HttpStatusCode status) where TEnum : Enum
    {
        return CollectionResult<T>.CreateFailed(Problem.Create(errorCode, detail, (int)status));
    }
}
