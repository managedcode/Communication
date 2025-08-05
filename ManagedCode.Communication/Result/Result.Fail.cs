using System;
using System.Collections.Generic;
using System.Net;

namespace ManagedCode.Communication;

public partial struct Result
{
    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static Result Fail()
    {
        return new Result(false);
    }

    /// <summary>
    /// Creates a failed result with a problem.
    /// </summary>
    public static Result Fail(Problem problem)
    {
        return new Result(false, problem);
    }


    /// <summary>
    /// Creates a failed result with a title and optional detail.
    /// </summary>
    public static Result Fail(string title, string? detail = null, HttpStatusCode status = HttpStatusCode.InternalServerError)
    {
        var problem = new Problem
        {
            Title = title,
            Detail = detail,
            StatusCode = (int)status
        };
        return new Result(false, problem);
    }

    /// <summary>
    /// Creates a failed result from an exception.
    /// </summary>
    public static Result Fail(Exception exception, HttpStatusCode status = HttpStatusCode.InternalServerError)
    {
        var problem = new Problem
        {
            Type = $"https://httpstatuses.io/{(int)status}",
            Title = exception.GetType().Name,
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
        
        return new Result(false, problem);
    }

    /// <summary>
    /// Creates a failed result with validation errors.
    /// </summary>
    public static Result FailValidation(params (string field, string message)[] errors)
    {
        return new Result(false, Problem.Validation(errors));
    }

    /// <summary>
    /// Creates a failed result for unauthorized access.
    /// </summary>
    public static Result FailUnauthorized(string? detail = null)
    {
        var problem = new Problem
        {
            Type = "https://httpstatuses.io/401",
            Title = "Unauthorized",
            StatusCode = (int)HttpStatusCode.Unauthorized,
            Detail = detail ?? "Authentication is required to access this resource."
        };
        
        return new Result(false, problem);
    }

    /// <summary>
    /// Creates a failed result for forbidden access.
    /// </summary>
    public static Result FailForbidden(string? detail = null)
    {
        var problem = new Problem
        {
            Type = "https://httpstatuses.io/403",
            Title = "Forbidden",
            StatusCode = (int)HttpStatusCode.Forbidden,
            Detail = detail ?? "You do not have permission to access this resource."
        };
        
        return new Result(false, problem);
    }

    /// <summary>
    /// Creates a failed result for not found.
    /// </summary>
    public static Result FailNotFound(string? detail = null)
    {
        var problem = new Problem
        {
            Type = "https://httpstatuses.io/404",
            Title = "Not Found",
            StatusCode = (int)HttpStatusCode.NotFound,
            Detail = detail ?? "The requested resource was not found."
        };
        
        return new Result(false, problem);
    }
    
    /// <summary>
    /// Creates a failed result from a custom error enum.
    /// </summary>
    public static Result Fail<TEnum>(TEnum errorCode, string? detail = null) where TEnum : Enum
    {
        return new Result(false, Problem.FromEnum(errorCode, detail));
    }
    
    /// <summary>
    /// Creates a failed result from a custom error enum with specific HTTP status.
    /// </summary>
    public static Result Fail<TEnum>(TEnum errorCode, HttpStatusCode status, string? detail = null) where TEnum : Enum
    {
        var problem = Problem.FromEnum(errorCode, detail, (int)status);
        return new Result(false, problem);
    }
}