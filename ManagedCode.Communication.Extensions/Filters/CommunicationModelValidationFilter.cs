using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using static ManagedCode.Communication.Extensions.Constants.ProblemConstants;

namespace ManagedCode.Communication.Extensions.Filters;

public class CommunicationModelValidationFilter(ILogger<CommunicationModelValidationFilter> logger) : IActionFilter
{

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            logger.LogWarning("Model validation failed for {ActionName}", 
                context.ActionDescriptor.DisplayName);

            var validationErrors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => (x.Key, e.ErrorMessage)))
                .ToArray();

            var result = ManagedCode.Communication.Result.FailValidation(validationErrors);

            context.Result = new BadRequestObjectResult(result);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Not needed for this filter
    }
} 