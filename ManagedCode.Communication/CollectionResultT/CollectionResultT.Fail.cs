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
        return Create(false, default, 0, 0, 0);
    }

    public static CollectionResult<T> Fail(IEnumerable<T> value)
    {
        return Create(false, value.ToArray(), 0, 0, 0);
    }

    public static CollectionResult<T> Fail(T[] value)
    {
        return Create(false, value, 0, 0, 0);
    }

    public static CollectionResult<T> Fail(Problem problem)
    {
        return Create(false, default, 0, 0, 0, problem);
    }

    public static CollectionResult<T> Fail(string title)
    {
        var problem = Problem.Create(title, title, (int)HttpStatusCode.InternalServerError);
        return Create(false, default, 0, 0, 0, problem);
    }
    
    public static CollectionResult<T> Fail(string title, string detail)
    {
        var problem = Problem.Create(title, detail);
        return Create(false, default, 0, 0, 0, problem);
    }
    
    public static CollectionResult<T> Fail(string title, string detail, HttpStatusCode status)
    {
        var problem = Problem.Create(title, detail, (int)status);
        return Create(false, default, 0, 0, 0, problem);
    }

    public static CollectionResult<T> Fail(Exception exception)
    {
        return new CollectionResult<T>(false, default, 0, 0, 0, Problem.Create(exception, (int)HttpStatusCode.InternalServerError));
    }
    
    public static CollectionResult<T> Fail(Exception exception, HttpStatusCode status)
    {
        return new CollectionResult<T>(false, default, 0, 0, 0, Problem.Create(exception, (int)status));
    }

    public static CollectionResult<T> FailValidation(params (string field, string message)[] errors)
    {
        return new CollectionResult<T>(false, default, 0, 0, 0, Problem.Validation(errors));
    }

    public static CollectionResult<T> FailUnauthorized()
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.Unauthorized,
            ProblemConstants.Messages.UnauthorizedAccess,
            (int)HttpStatusCode.Unauthorized);

        return Create(false, default, 0, 0, 0, problem);
    }
    
    public static CollectionResult<T> FailUnauthorized(string detail)
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.Unauthorized,
            detail,
            (int)HttpStatusCode.Unauthorized);

        return Create(false, default, 0, 0, 0, problem);
    }

    public static CollectionResult<T> FailForbidden()
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.Forbidden,
            ProblemConstants.Messages.ForbiddenAccess,
            (int)HttpStatusCode.Forbidden);

        return Create(false, default, 0, 0, 0, problem);
    }
    
    public static CollectionResult<T> FailForbidden(string detail)
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.Forbidden,
            detail,
            (int)HttpStatusCode.Forbidden);

        return Create(false, default, 0, 0, 0, problem);
    }

    public static CollectionResult<T> FailNotFound()
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.NotFound,
            ProblemConstants.Messages.ResourceNotFound,
            (int)HttpStatusCode.NotFound);

        return Create(false, default, 0, 0, 0, problem);
    }
    
    public static CollectionResult<T> FailNotFound(string detail)
    {
        var problem = Problem.Create(
            ProblemConstants.Titles.NotFound,
            detail,
            (int)HttpStatusCode.NotFound);

        return Create(false, default, 0, 0, 0, problem);
    }

    public static CollectionResult<T> Fail<TEnum>(TEnum errorCode) where TEnum : Enum
    {
        return new CollectionResult<T>(false, default, 0, 0, 0, Problem.Create(errorCode));
    }
    
    public static CollectionResult<T> Fail<TEnum>(TEnum errorCode, string detail) where TEnum : Enum
    {
        return new CollectionResult<T>(false, default, 0, 0, 0, Problem.Create(errorCode, detail));
    }

    public static CollectionResult<T> Fail<TEnum>(TEnum errorCode, HttpStatusCode status) where TEnum : Enum
    {
        return new CollectionResult<T>(false, default, 0, 0, 0, Problem.Create(errorCode, errorCode.ToString(), (int)status));
    }
    
    public static CollectionResult<T> Fail<TEnum>(TEnum errorCode, string detail, HttpStatusCode status) where TEnum : Enum
    {
        return new CollectionResult<T>(false, default, 0, 0, 0, Problem.Create(errorCode, detail, (int)status));
    }
}