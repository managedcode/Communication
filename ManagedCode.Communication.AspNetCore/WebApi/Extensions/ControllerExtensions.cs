using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ManagedCode.Communication;
using ManagedCode.Communication.Constants;
using ManagedCode.Communication.Results.Extensions;

namespace ManagedCode.Communication.AspNetCore.Extensions;

/// <summary>
///     Converts results into MVC and Minimal API responses.
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    ///     Converts the result into an MVC action result, mapping a failure onto its status code.
    /// </summary>
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);
        
        var problem = NormalizeProblem(result.GetProblemNoFallback());
        return new ObjectResult(problem)
        {
            StatusCode = problem.StatusCode
        };
    }

    /// <summary>
    ///     Converts the result into an MVC action result, mapping a failure onto its status code.
    /// </summary>
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new NoContentResult();
        
        var problem = NormalizeProblem(result.GetProblemNoFallback());
        return new ObjectResult(problem)
        {
            StatusCode = problem.StatusCode
        };
    }

    /// <summary>
    ///     Converts the result into a Minimal API response, mapping a failure onto its status code.
    /// </summary>
    public static Microsoft.AspNetCore.Http.IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return Microsoft.AspNetCore.Http.Results.Ok(result.Value);
        
        var problem = NormalizeProblem(result.GetProblemNoFallback());
        return Microsoft.AspNetCore.Http.Results.Problem(
            title: problem.Title,
            detail: problem.Detail,
            statusCode: problem.StatusCode,
            type: problem.Type,
            instance: problem.Instance,
            extensions: problem.Extensions
        );
    }

    /// <summary>
    ///     Converts the result into a Minimal API response, mapping a failure onto its status code.
    /// </summary>
    public static Microsoft.AspNetCore.Http.IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
            return Microsoft.AspNetCore.Http.Results.NoContent();
        
        var problem = NormalizeProblem(result.GetProblemNoFallback());
        return Microsoft.AspNetCore.Http.Results.Problem(
            title: problem.Title,
            detail: problem.Detail,
            statusCode: problem.StatusCode,
            type: problem.Type,
            instance: problem.Instance,
            extensions: problem.Extensions
        );
    }

    private static Problem NormalizeProblem(Problem? problem)
    {
        if (problem is null || IsGeneric(problem))
        {
            return Problem.Create("Operation failed", "Unknown error occurred", 500);
        }

        return problem;
    }

    private static bool IsGeneric(Problem problem)
    {
        return string.Equals(problem.Title, ProblemConstants.Titles.Error, StringComparison.OrdinalIgnoreCase)
               && string.Equals(problem.Detail, ProblemConstants.Messages.GenericError, StringComparison.OrdinalIgnoreCase);
    }
}
