using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using ManagedCode.Communication;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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
                Title = "Validation failed",
                Status = 400,
                Instance = context.HttpContext.Request.Path,
                Extensions =
                {
                    ["validationErrors"] = context.ModelState
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