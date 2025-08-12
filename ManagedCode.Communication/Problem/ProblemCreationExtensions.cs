using System;
using System.Net;

namespace ManagedCode.Communication.Extensions;

/// <summary>
///     Extension methods for creating Problems from various sources
/// </summary>
public static class ProblemCreationExtensions
{
    /// <summary>
    ///     Creates a Problem from an exception
    /// </summary>
    public static Problem ToProblem(this Exception exception)
    {
        return Problem.Create(exception);
    }
    
    /// <summary>
    ///     Creates a Problem from an exception with status code
    /// </summary>
    public static Problem ToProblem(this Exception exception, int statusCode)
    {
        return Problem.Create(exception, statusCode);
    }

    /// <summary>
    ///     Creates a Problem from an exception with HttpStatusCode
    /// </summary>
    public static Problem ToProblem(this Exception exception, HttpStatusCode statusCode)
    {
        return Problem.Create(exception, (int)statusCode);
    }

    /// <summary>
    ///     Creates a Problem from an enum
    /// </summary>
    public static Problem ToProblem<TEnum>(this TEnum errorCode) where TEnum : Enum
    {
        return Problem.Create(errorCode);
    }
    
    /// <summary>
    ///     Creates a Problem from an enum with detail
    /// </summary>
    public static Problem ToProblem<TEnum>(this TEnum errorCode, string detail) where TEnum : Enum
    {
        return Problem.Create(errorCode, detail);
    }
    
    /// <summary>
    ///     Creates a Problem from an enum with detail and status code
    /// </summary>
    public static Problem ToProblem<TEnum>(this TEnum errorCode, string detail, int statusCode) where TEnum : Enum
    {
        return Problem.Create(errorCode, detail, statusCode);
    }

    /// <summary>
    ///     Creates a Problem from an enum with detail and HttpStatusCode
    /// </summary>
    public static Problem ToProblem<TEnum>(this TEnum errorCode, string detail, HttpStatusCode statusCode) where TEnum : Enum
    {
        return Problem.Create(errorCode, detail, (int)statusCode);
    }

    /// <summary>
    ///     Converts a Problem to an exception
    /// </summary>
    public static Exception ToException(this Problem problem)
    {
        return new ProblemException(problem);
    }
}