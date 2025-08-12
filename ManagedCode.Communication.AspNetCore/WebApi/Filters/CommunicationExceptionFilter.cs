using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using static ManagedCode.Communication.AspNetCore.Helpers.HttpStatusCodeHelper;

namespace ManagedCode.Communication.AspNetCore.Filters;

public class CommunicationExceptionFilter(ILogger<CommunicationExceptionFilter> logger) : IExceptionFilter
{
    public virtual void OnException(ExceptionContext context)
    {
        try
        {
            var exception = context.Exception;
            var actionName = context.ActionDescriptor.DisplayName;
            var controllerName = context.ActionDescriptor.RouteValues["controller"] ?? "Unknown";

            logger.LogError(exception, "Unhandled exception in {ControllerName}.{ActionName}", controllerName, actionName);

            var statusCode = GetStatusCodeForException(exception);
            var result = Result.Fail(exception, statusCode);

            context.Result = new ObjectResult(result)
            {
                StatusCode = (int)statusCode
            };

            context.ExceptionHandled = true;

            logger.LogInformation("Exception handled by {FilterType} for {ControllerName}.{ActionName}", GetType()
                .Name, controllerName, actionName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while handling exception in {FilterType}", GetType()
                .Name);

            var statusCode = GetStatusCodeForException(ex);
            context.Result = new ObjectResult(Result.Fail(ex, statusCode))
            {
                StatusCode = (int)statusCode
            };
            context.ExceptionHandled = true;
        }
    }
}