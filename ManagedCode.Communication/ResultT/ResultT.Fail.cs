using System;
using System.Collections.Generic;
using System.Net;

namespace ManagedCode.Communication;

/// <summary>
/// Represents a result of an operation with a specific type.
/// This partial class contains methods for creating failed results.
/// </summary>
public partial struct Result<T>
{
    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static Result<T> Fail()
    {
        return new Result<T>(false, default);
    }

    /// <summary>
    /// Creates a failed result with a specific value.
    /// </summary>
    public static Result<T> Fail(T value)
    {
        return new Result<T>(false, value);
    }

    /// <summary>
    /// Creates a failed result with a problem.
    /// </summary>
    public static Result<T> Fail(Problem problem)
    {
        return new Result<T>(false, default, problem);
    }


    /// <summary>
    /// Creates a failed result with a title and optional detail.
    /// </summary>
    public static Result<T> Fail(string title, string? detail = null, HttpStatusCode status = HttpStatusCode.InternalServerError)
    {
        var problem = new Problem
        {
            Title = title,
            Detail = detail,
            StatusCode = (int)status
        };
        return new Result<T>(false, default, problem);
    }

    /// <summary>
    /// Creates a failed result from an exception.
    /// </summary>
    public static Result<T> Fail(Exception exception, HttpStatusCode status = HttpStatusCode.InternalServerError)
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
        
        return new Result<T>(false, default, problem);
    }

    /// <summary>
    /// Creates a failed result with validation errors.
    /// </summary>
    public static Result<T> FailValidation(params (string field, string message)[] errors)
    {
        return new Result<T>(false, default, Problem.Validation(errors));
    }

    /// <summary>
    /// Creates a failed result for unauthorized access.
    /// </summary>
    public static Result<T> FailUnauthorized(string? detail = null)
    {
        var problem = new Problem
        {
            Type = "https://httpstatuses.io/401",
            Title = "Unauthorized",
            StatusCode = (int)HttpStatusCode.Unauthorized,
            Detail = detail ?? "Authentication is required to access this resource."
        };
        
        return new Result<T>(false, default, problem);
    }

    /// <summary>
    /// Creates a failed result for forbidden access.
    /// </summary>
    public static Result<T> FailForbidden(string? detail = null)
    {
        var problem = new Problem
        {
            Type = "https://httpstatuses.io/403",
            Title = "Forbidden",
            StatusCode = (int)HttpStatusCode.Forbidden,
            Detail = detail ?? "You do not have permission to access this resource."
        };
        
        return new Result<T>(false, default, problem);
    }

    /// <summary>
    /// Creates a failed result for not found.
    /// </summary>
    public static Result<T> FailNotFound(string? detail = null)
    {
        var problem = new Problem
        {
            Type = "https://httpstatuses.io/404",
            Title = "Not Found",
            StatusCode = (int)HttpStatusCode.NotFound,
            Detail = detail ?? "The requested resource was not found."
        };
        
        return new Result<T>(false, default, problem);
    }
    
    /// <summary>
    /// Creates a failed result from a custom error enum.
    /// </summary>
    public static Result<T> Fail<TEnum>(TEnum errorCode, string? detail = null) where TEnum : Enum
    {
        return new Result<T>(false, default, Problem.FromEnum(errorCode, detail));
    }
    
    /// <summary>
    /// Creates a failed result from a custom error enum with specific HTTP status.
    /// </summary>
    public static Result<T> Fail<TEnum>(TEnum errorCode, HttpStatusCode status, string? detail = null) where TEnum : Enum
    {
        var problem = Problem.FromEnum(errorCode, detail, (int)status);
        return new Result<T>(false, default, problem);
    }
}