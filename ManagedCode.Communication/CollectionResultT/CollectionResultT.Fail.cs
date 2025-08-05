using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace ManagedCode.Communication.CollectionResultT;

public partial struct CollectionResult<T>
{
    public static CollectionResult<T> Fail()
    {
        return new CollectionResult<T>(false, default, 0, 0, 0);
    }

    public static CollectionResult<T> Fail(IEnumerable<T> value)
    {
        return new CollectionResult<T>(false, value.ToArray(), 0, 0, 0);
    }

    public static CollectionResult<T> Fail(T[] value)
    {
        return new CollectionResult<T>(false, value, 0, 0, 0);
    }

    public static CollectionResult<T> Fail(Problem problem)
    {
        return new CollectionResult<T>(false, default, 0, 0, 0, problem);
    }

    public static CollectionResult<T> Fail(string title, string? detail = null, HttpStatusCode status = HttpStatusCode.InternalServerError)
    {
        var problem = new Problem
        {
            Title = title,
            Detail = detail,
            StatusCode = (int)status
        };
        return new CollectionResult<T>(false, default, 0, 0, 0, problem);
    }

    public static CollectionResult<T> Fail(Exception exception, HttpStatusCode status = HttpStatusCode.InternalServerError)
    {
        var problem = new Problem
        {
            Type = $"https://httpstatuses.io/{(int)status}",
            Title = exception.GetType()
                .Name,
            Detail = exception.Message,
            StatusCode = (int)status
        };

        if (exception.Data.Count > 0)
        {
            foreach (var key in exception.Data.Keys)
            {
                if (key != null)
                {
                    problem.Extensions[key.ToString()!] = exception.Data[key];
                }
            }
        }

        return new CollectionResult<T>(false, default, 0, 0, 0, problem);
    }

    public static CollectionResult<T> FailValidation(params (string field, string message)[] errors)
    {
        return new CollectionResult<T>(false, default, 0, 0, 0, Problem.Validation(errors));
    }

    public static CollectionResult<T> FailUnauthorized(string? detail = null)
    {
        var problem = new Problem
        {
            Type = "https://httpstatuses.io/401",
            Title = "Unauthorized",
            StatusCode = (int)HttpStatusCode.Unauthorized,
            Detail = detail ?? "Authentication is required to access this resource."
        };

        return new CollectionResult<T>(false, default, 0, 0, 0, problem);
    }

    public static CollectionResult<T> FailForbidden(string? detail = null)
    {
        var problem = new Problem
        {
            Type = "https://httpstatuses.io/403",
            Title = "Forbidden",
            StatusCode = (int)HttpStatusCode.Forbidden,
            Detail = detail ?? "You do not have permission to access this resource."
        };

        return new CollectionResult<T>(false, default, 0, 0, 0, problem);
    }

    public static CollectionResult<T> FailNotFound(string? detail = null)
    {
        var problem = new Problem
        {
            Type = "https://httpstatuses.io/404",
            Title = "Not Found",
            StatusCode = (int)HttpStatusCode.NotFound,
            Detail = detail ?? "The requested resource was not found."
        };

        return new CollectionResult<T>(false, default, 0, 0, 0, problem);
    }

    public static CollectionResult<T> Fail<TEnum>(TEnum errorCode, string? detail = null) where TEnum : Enum
    {
        return new CollectionResult<T>(false, default, 0, 0, 0, Problem.FromEnum(errorCode, detail));
    }

    public static CollectionResult<T> Fail<TEnum>(TEnum errorCode, HttpStatusCode status, string? detail = null) where TEnum : Enum
    {
        var problem = Problem.FromEnum(errorCode, detail, (int)status);
        return new CollectionResult<T>(false, default, 0, 0, 0, problem);
    }
}