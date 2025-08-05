using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using static ManagedCode.Communication.Extensions.Helpers.HttpStatusCodeHelper;
using static ManagedCode.Communication.Extensions.Constants.ProblemConstants;

namespace ManagedCode.Communication.Extensions.Filters;

public class CommunicationExceptionFilter(ILogger<CommunicationExceptionFilter> logger) : IExceptionFilter
{
    public virtual void OnException(ExceptionContext context)
    {
        try
        {
            var exception = context.Exception;
            var actionName = context.ActionDescriptor.DisplayName;
            var controllerName = context.ActionDescriptor.RouteValues["controller"] ?? "Unknown";

            logger.LogError(exception, "Unhandled exception in {ControllerName}.{ActionName}", 
                controllerName, actionName);

            var statusCode = GetStatusCodeForException(exception);
            var result = Result.Fail(exception, statusCode);

            context.Result = new ObjectResult(result)
            {
                StatusCode = (int)statusCode
            };

            context.ExceptionHandled = true;

            logger.LogInformation("Exception handled by {FilterType} for {ControllerName}.{ActionName}", 
                GetType().Name, controllerName, actionName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while handling exception in {FilterType}", GetType().Name);
            
            context.Result = new ObjectResult(Result.Fail(Titles.UnexpectedError, ex.Message, HttpStatusCode.InternalServerError))
            {
                StatusCode = (int)HttpStatusCode.InternalServerError
            };
            context.ExceptionHandled = true;
        }
    }
} 