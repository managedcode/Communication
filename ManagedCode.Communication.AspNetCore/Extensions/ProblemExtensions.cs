using Microsoft.AspNetCore.Mvc;
using ManagedCode.Communication.Constants;

namespace ManagedCode.Communication.AspNetCore;

/// <summary>
///     Extension methods for Problem and ProblemDetails conversion.
/// </summary>
public static class ProblemExtensions
{
    /// <summary>
    ///     Converts Problem to ProblemDetails.
    /// </summary>
    public static ProblemDetails ToProblemDetails(this Problem problem)
    {
        var problemDetails = new ProblemDetails
        {
            Type = problem.Type,
            Title = problem.Title,
            Status = problem.StatusCode == 0 ? null : problem.StatusCode,
            Detail = problem.Detail,
            Instance = problem.Instance
        };

        foreach (var kvp in problem.Extensions)
        {
            problemDetails.Extensions[kvp.Key] = kvp.Value;
        }

        return problemDetails;
    }

    /// <summary>
    ///     Creates a Problem from ProblemDetails.
    /// </summary>
    public static Problem FromProblemDetails(ProblemDetails problemDetails)
    {
        var statusCode = problemDetails.Status ?? 0;
        var problem = new Problem
        {
            Type = problemDetails.Type ?? ProblemConstants.Types.AboutBlank,
            Title = problemDetails.Title,
            StatusCode = statusCode,
            Detail = problemDetails.Detail,
            Instance = problemDetails.Instance
        };
        
        // If Type was null in the original ProblemDetails, keep it null
        if (problemDetails.Type == null)
        {
            problem.Type = null!;
        }

        foreach (var kvp in problemDetails.Extensions)
        {
            problem.Extensions[kvp.Key] = kvp.Value;
        }

        return problem;
    }

    /// <summary>
    ///     Converts Problem to ProblemDetails.
    /// </summary>
    public static ProblemDetails AsProblemDetails(this Problem problem)
    {
        return problem.ToProblemDetails();
    }

    /// <summary>
    ///     Converts ProblemDetails to Problem.
    /// </summary>
    public static Problem AsProblem(this ProblemDetails problemDetails)
    {
        return FromProblemDetails(problemDetails);
    }

    /// <summary>
    ///     Creates a failed Result from ProblemDetails.
    /// </summary>
    public static Result ToFailedResult(this ProblemDetails problemDetails)
    {
        return Result.Fail(problemDetails.AsProblem());
    }

    /// <summary>
    ///     Creates a failed Result with value from ProblemDetails.
    /// </summary>
    public static Result<T> ToFailedResult<T>(this ProblemDetails problemDetails)
    {
        return Result<T>.Fail(problemDetails.AsProblem());
    }

    /// <summary>
    ///     Creates a failed Result from Problem.
    /// </summary>
    public static Result ToFailedResult(this Problem problem)
    {
        return Result.Fail(problem);
    }

    /// <summary>
    ///     Creates a failed Result with value from Problem.
    /// </summary>
    public static Result<T> ToFailedResult<T>(this Problem problem)
    {
        return Result<T>.Fail(problem);
    }
}