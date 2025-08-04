using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using static ManagedCode.Communication.Extensions.Constants.ProblemConstants;

namespace ManagedCode.Communication.Extensions;

public abstract class ModelValidationFilterBase(ILogger<ModelValidationFilterBase> logger) : IActionFilter
{

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            logger.LogWarning("Model validation failed for {ActionName}", 
                context.ActionDescriptor.DisplayName);

            var problem = new Problem
            {
                Title = Titles.ValidationFailed,
                Status = HttpStatusCode.BadRequest,
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

            var result = Result<Problem>.Fail(problem);

            context.Result = new BadRequestObjectResult(result);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Not needed for this filter
    }
} 