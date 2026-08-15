using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using ManagedCode.Communication.Logging;

namespace ManagedCode.Communication.AspNetCore.Filters;

/// <summary>
///     MVC filter that turns model-state errors into a validation <c>Problem</c>.
/// </summary>
public class CommunicationModelValidationFilter(ILogger<CommunicationModelValidationFilter> logger) : IActionFilter
{
    /// <summary>
    ///     Short-circuits the action when model validation failed.
    /// </summary>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            LoggerCenter.LogValidationFailed(logger, context.ActionDescriptor.DisplayName ?? "Unknown");

            var validationErrors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => (x.Key, e.ErrorMessage)))
                .ToArray();

            var result = Result.FailValidation(validationErrors);

            context.Result = new BadRequestObjectResult(result);
        }
    }

    /// <summary>
    ///     No-op; validation is handled before the action runs.
    /// </summary>
    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Not needed for this filter
    }
}