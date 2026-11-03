using System;
using ManagedCode.Communication.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace ManagedCode.Communication.AspNetCore.Extensions;

/// <summary>
///     Converts results into MVC and Minimal API responses.
/// </summary>
public static class ControllerExtensions
{
    private const string DefaultProblemTitle = "Operation failed";
    private const string DefaultProblemDetail = "Unknown error occurred";
    private static readonly string DefaultProblemType = ProblemConstants.Types.HttpStatus(StatusCodes.Status500InternalServerError);

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
            return HttpResults.Ok(result.Value);

        return ToProblemHttpResult(result.GetProblemNoFallback());
    }

    /// <summary>
    ///     Converts the result into a Minimal API response, mapping a failure onto its status code.
    /// </summary>
    public static Microsoft.AspNetCore.Http.IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
            return HttpResults.NoContent();

        return ToProblemHttpResult(result.GetProblemNoFallback());
    }

    internal static Microsoft.AspNetCore.Http.IResult ToProblemHttpResult(Problem? problem)
    {
        if (problem is null || IsDefaultProblem(problem))
        {
            return HttpResults.Problem(
                title: DefaultProblemTitle,
                detail: DefaultProblemDetail,
                statusCode: StatusCodes.Status500InternalServerError,
                type: DefaultProblemType);
        }

        return HttpResults.Problem(
            title: problem.Title,
            detail: problem.Detail,
            statusCode: problem.StatusCode,
            type: problem.Type,
            instance: problem.Instance,
            extensions: problem.Extensions);
    }

    private static Problem NormalizeProblem(Problem? problem)
    {
        if (problem is null || IsDefaultProblem(problem))
        {
            return Problem.Create(DefaultProblemTitle, DefaultProblemDetail, StatusCodes.Status500InternalServerError);
        }

        return problem;
    }

    private static bool IsDefaultProblem(Problem problem)
    {
        return problem.StatusCode == StatusCodes.Status500InternalServerError
               && string.Equals(problem.Type, DefaultProblemType, StringComparison.Ordinal)
               && problem.Instance is null
               && problem.Extensions.Count == 0
               && string.Equals(problem.Title, ProblemConstants.Titles.Error, StringComparison.OrdinalIgnoreCase)
               && string.Equals(problem.Detail, ProblemConstants.Messages.GenericError, StringComparison.OrdinalIgnoreCase);
    }
}
