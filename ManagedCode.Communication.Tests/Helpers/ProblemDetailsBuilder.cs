using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Communication.Tests.Helpers;

/// <summary>
/// Helper class for creating ProblemDetails in tests
/// </summary>
public static class ProblemDetailsBuilder
{
    public static ProblemDetails Create(string type, string title, int status, string detail)
    {
        return new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = status,
            Detail = detail
        };
    }
    
    public static ProblemDetails Create(string type, string title, int status, string detail, string instance)
    {
        return new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = status,
            Detail = detail,
            Instance = instance
        };
    }

    public static ProblemDetails CreateWithValidationErrors(
        string title, 
        string detail, 
        int status,
        params (string field, string[] messages)[] errors)
    {
        var problemDetails = new ProblemDetails
        {
            Type = $"https://httpstatuses.io/{status}",
            Title = title,
            Status = status,
            Detail = detail
        };

        var errorDict = new Dictionary<string, List<string>>();
        foreach (var (field, messages) in errors)
        {
            errorDict[field] = new List<string>(messages);
        }
        
        problemDetails.Extensions["errors"] = errorDict;
        return problemDetails;
    }

    public static ProblemDetails CreateUnauthorized()
    {
        return Create(
            "https://httpstatuses.io/401",
            "Unauthorized",
            401,
            "Authentication is required to access this resource."
        );
    }
    
    public static ProblemDetails CreateUnauthorized(string detail)
    {
        return Create(
            "https://httpstatuses.io/401",
            "Unauthorized",
            401,
            detail
        );
    }

    public static ProblemDetails CreateForbidden()
    {
        return Create(
            "https://httpstatuses.io/403",
            "Forbidden",
            403,
            "You do not have permission to access this resource."
        );
    }
    
    public static ProblemDetails CreateForbidden(string detail)
    {
        return Create(
            "https://httpstatuses.io/403",
            "Forbidden",
            403,
            detail
        );
    }

    public static ProblemDetails CreateNotFound()
    {
        return Create(
            "https://httpstatuses.io/404",
            "Not Found",
            404,
            "The requested resource was not found."
        );
    }
    
    public static ProblemDetails CreateNotFound(string detail)
    {
        return Create(
            "https://httpstatuses.io/404",
            "Not Found",
            404,
            detail
        );
    }

    public static ProblemDetails CreateValidationFailed(params (string field, string[] messages)[] errors)
    {
        return CreateWithValidationErrors(
            "Validation Failed",
            "One or more validation errors occurred.",
            400,
            errors
        );
    }
}