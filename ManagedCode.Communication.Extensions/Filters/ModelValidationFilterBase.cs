using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using static ManagedCode.Communication.Extensions.Constants.ProblemConstants;

namespace ManagedCode.Communication.Extensions.Filters;

public abstract class ModelValidationFilterBase(ILogger logger) : IActionFilter
{

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            logger.LogWarning("Model validation failed for {ActionName}", 
                context.ActionDescriptor.DisplayName);

            var problem = new ManagedCode.Communication.Problem
            {
                Title = Titles.ValidationFailed,
                StatusCode = (int)HttpStatusCode.BadRequest,
                Instance = context.HttpContext.Request.Path,
                Extensions =
                {
                    [ExtensionKeys.ValidationErrors] = context.ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? []
                        )
                }
            };

            var result = ManagedCode.Communication.Result.Fail(problem);

            context.Result = new BadRequestObjectResult(result);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Not needed for this filter
    }
} 