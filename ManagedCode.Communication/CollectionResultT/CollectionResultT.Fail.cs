using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ManagedCode.Communication.Constants;

namespace ManagedCode.Communication.CollectionResultT;

public partial struct CollectionResult<T>
{
    public static CollectionResult<T> Fail()
    {
        return CreateFailed(Problem.GenericError());
    }

    public static CollectionResult<T> Fail(IEnumerable<T> value)
    {
        return CreateFailed(Problem.GenericError(), value.ToArray());
    }

    public static CollectionResult<T> Fail(T[] value)
    {
        return CreateFailed(Problem.GenericError(), value);
    }

    public static CollectionResult<T> Fail(Problem problem)
    {
        return CreateFailed(problem);
    }

    public static CollectionResult<T> Fail(string title)
    {
        var problem = Problem.Create(title, title, (int)HttpStatusCode.InternalServerError);
        return CreateFailed(problem);
    }

    public static CollectionResult<T> Fail(string title, string detail)
    {
        var problem = Problem.Create(title, detail);
        return CreateFailed(problem);
    }

    public static CollectionResult<T> Fail(string title, string detail, HttpStatusCode status)
    {
        var problem = Problem.Create(title, detail, (int)status);
        return CreateFailed(problem);
    }

    public static CollectionResult<T> Fail(Exception exception)
    {
        return CreateFailed(Problem.Create(exception, (int)HttpStatusCode.InternalServerError));
    }

    public static CollectionResult<T> Fail(Exception exception, HttpStatusCode status)
    {
        return CreateFailed(Problem.Create(exception, (int)status));
    }

    public static CollectionResult<T> FailValidation(params (string field, string message)[] errors)
    {
        return CreateFailed(Problem.Validation(errors));
    }

    public static CollectionResult<T> FailBadRequest()
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.BadRequest,
            ProblemConstants.Messages.BadRequest,
            (int)HttpStatusCode.BadRequest);

        return CreateFailed(problem);
    }

    public static CollectionResult<T> FailBadRequest(string detail)
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.BadRequest,
            detail,
            (int)HttpStatusCode.BadRequest);

        return CreateFailed(problem);
    }

    public static CollectionResult<T> FailUnauthorized()
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.Unauthorized,
            ProblemConstants.Messages.UnauthorizedAccess,
            (int)HttpStatusCode.Unauthorized);

        return CreateFailed(problem);
    }

    public static CollectionResult<T> FailUnauthorized(string detail)
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.Unauthorized,
            detail,
            (int)HttpStatusCode.Unauthorized);

        return CreateFailed(problem);
    }

    public static CollectionResult<T> FailForbidden()
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.Forbidden,
            ProblemConstants.Messages.ForbiddenAccess,
            (int)HttpStatusCode.Forbidden);

        return CreateFailed(problem);
    }

    public static CollectionResult<T> FailForbidden(string detail)
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.Forbidden,
            detail,
            (int)HttpStatusCode.Forbidden);

        return CreateFailed(problem);
    }

    public static CollectionResult<T> FailNotFound()
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.NotFound,
            ProblemConstants.Messages.ResourceNotFound,
            (int)HttpStatusCode.NotFound);

        return CreateFailed(problem);
    }

    public static CollectionResult<T> FailNotFound(string detail)
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.NotFound,
            detail,
            (int)HttpStatusCode.NotFound);

        return CreateFailed(problem);
    }

    public static CollectionResult<T> Fail<TEnum>(TEnum errorCode) where TEnum : Enum
    {
        return CreateFailed(Problem.Create(errorCode));
    }

    public static CollectionResult<T> Fail<TEnum>(TEnum errorCode, string detail) where TEnum : Enum
    {
        return CreateFailed(Problem.Create(errorCode, detail));
    }

    public static CollectionResult<T> Fail<TEnum>(TEnum errorCode, HttpStatusCode status) where TEnum : Enum
    {
        return CreateFailed(Problem.Create(errorCode, errorCode.ToString(), (int)status));
    }

    public static CollectionResult<T> Fail<TEnum>(TEnum errorCode, string detail, HttpStatusCode status) where TEnum : Enum
    {
        return CreateFailed(Problem.Create(errorCode, detail, (int)status));
    }
}
