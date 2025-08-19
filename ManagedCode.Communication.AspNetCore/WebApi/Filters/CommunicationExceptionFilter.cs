using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using ManagedCode.Communication.Logging;
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

            LoggerCenter.LogControllerException(logger, exception, controllerName, actionName ?? "Unknown");

            var statusCode = GetStatusCodeForException(exception);
            var result = Result.Fail(exception, statusCode);

            context.Result = new ObjectResult(result)
            {
                StatusCode = (int)statusCode
            };

            context.ExceptionHandled = true;

            LoggerCenter.LogExceptionHandled(logger, GetType().Name, controllerName, actionName ?? "Unknown");
        }
        catch (Exception ex)
        {
            LoggerCenter.LogFilterError(logger, ex, GetType().Name);

            var statusCode = GetStatusCodeForException(ex);
            context.Result = new ObjectResult(Result.Fail(ex, statusCode))
            {
                StatusCode = (int)statusCode
            };
            context.ExceptionHandled = true;
        }
    }
}