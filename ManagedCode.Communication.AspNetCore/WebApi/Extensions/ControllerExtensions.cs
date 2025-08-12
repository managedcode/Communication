using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Communication.AspNetCore.Extensions;

public static class ControllerExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);
        
        var problem = result.Problem ?? Problem.Create("Operation failed", "Unknown error occurred", 500);
        return new ObjectResult(problem)
        {
            StatusCode = problem.StatusCode
        };
    }

    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new NoContentResult();
        
        var problem = result.Problem ?? Problem.Create("Operation failed", "Unknown error occurred", 500);
        return new ObjectResult(problem)
        {
            StatusCode = problem.StatusCode
        };
    }

    public static Microsoft.AspNetCore.Http.IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return Results.Ok(result.Value);
        
        var problem = result.Problem ?? Problem.Create("Operation failed", "Unknown error occurred", 500);
        return Results.Problem(
            title: problem.Title,
            detail: problem.Detail,
            statusCode: problem.StatusCode,
            type: problem.Type,
            instance: problem.Instance,
            extensions: problem.Extensions
        );
    }

    public static Microsoft.AspNetCore.Http.IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
            return Results.NoContent();
        
        var problem = result.Problem ?? Problem.Create("Operation failed", "Unknown error occurred", 500);
        return Results.Problem(
            title: problem.Title,
            detail: problem.Detail,
            statusCode: problem.StatusCode,
            type: problem.Type,
            instance: problem.Instance,
            extensions: problem.Extensions
        );
    }
}