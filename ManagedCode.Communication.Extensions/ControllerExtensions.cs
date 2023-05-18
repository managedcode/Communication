using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Communication.Extensions;

public static class ControllerExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        return result.IsSuccess ? new OkObjectResult(result.Value) : new BadRequestObjectResult(result.GetError()?.Message);
    }
}