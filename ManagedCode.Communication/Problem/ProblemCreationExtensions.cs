using System;
using System.Net;

namespace ManagedCode.Communication;

/// <summary>
///     Extension methods for creating Problems from various sources
/// </summary>
public static class ProblemCreationExtensions
{
    /// <summary>
    ///     Creates a Problem from an exception
    /// </summary>
    public static Problem ToProblem(this Exception exception, int statusCode = 500)
    {
        return Problem.FromException(exception, statusCode);
    }

    /// <summary>
    ///     Creates a Problem from an exception with HttpStatusCode
    /// </summary>
    public static Problem ToProblem(this Exception exception, HttpStatusCode statusCode)
    {
        return Problem.FromException(exception, (int)statusCode);
    }

    /// <summary>
    ///     Creates a Problem from an enum
    /// </summary>
    public static Problem ToProblem<TEnum>(this TEnum errorCode, string? detail = null, int statusCode = 400) where TEnum : Enum
    {
        return Problem.FromEnum(errorCode, detail, statusCode);
    }

    /// <summary>
    ///     Creates a Problem from an enum with HttpStatusCode
    /// </summary>
    public static Problem ToProblem<TEnum>(this TEnum errorCode, string? detail, HttpStatusCode statusCode) where TEnum : Enum
    {
        return Problem.FromEnum(errorCode, detail, (int)statusCode);
    }

    /// <summary>
    ///     Converts a Problem to an exception
    /// </summary>
    public static Exception ToException(this Problem problem)
    {
        return new ProblemException(problem);
    }
}