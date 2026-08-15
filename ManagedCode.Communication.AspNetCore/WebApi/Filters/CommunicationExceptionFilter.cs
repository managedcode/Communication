using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using ManagedCode.Communication.Logging;
using ManagedCode.Communication.Telemetry;
using static ManagedCode.Communication.AspNetCore.Helpers.HttpStatusCodeHelper;

namespace ManagedCode.Communication.AspNetCore.Filters;

/// <summary>
///     MVC filter that converts an unhandled action exception into a failed result, and records the original exception in logs and traces.
/// </summary>
public class CommunicationExceptionFilter(ILogger<CommunicationExceptionFilter> logger) : IExceptionFilter
{
    /// <summary>
    ///     Converts the exception into a failed result and stops further exception handling.
    /// </summary>
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

            // The Problem keeps only the exception's type name and message. Hand the exception itself to
            // telemetry so the span carries the real stack trace and inner exceptions.
            CommunicationTelemetry.RecordFailure(result.Problem, exception);

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