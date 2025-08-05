using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Communication.Extensions.Extensions;

public static class ControllerExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        return result.IsSuccess ? new OkObjectResult(result.Value) : new BadRequestObjectResult(result.Problem?.Detail ?? "Operation failed");
    }

    public static Microsoft.AspNetCore.Http.IResult ToHttpResult<T>(this Result<T> result)
    {
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Problem?.Detail ?? "Operation failed");
    }
}