using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using static ManagedCode.Communication.Extensions.Helpers.HttpStatusCodeHelper;
using static ManagedCode.Communication.Extensions.Constants.ProblemConstants;

namespace ManagedCode.Communication.Extensions;

public abstract class ExceptionFilterBase(ILogger logger) : IExceptionFilter
{
    protected readonly ILogger Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    
    public virtual void OnException(ExceptionContext context)
    {
        try
        {
            var exception = context.Exception;
            var actionName = context.ActionDescriptor.DisplayName;
            var controllerName = context.ActionDescriptor.RouteValues["controller"] ?? "Unknown";

            Logger.LogError(exception, "Unhandled exception in {ControllerName}.{ActionName}", 
                controllerName, actionName);

            var statusCode = GetStatusCodeForException(exception);
            
            var problem = new Problem()
            {
                Title = exception.GetType().Name,
                Detail = exception.Message,
                Status = statusCode,
                Instance = context.HttpContext.Request.Path,
                Extensions =
                {
                    [ExtensionKeys.TraceId] = context.HttpContext.TraceIdentifier
                }
            };

            var result = Result<Problem>.Fail(exception.Message, problem);

            context.Result = new ObjectResult(result)
            {
                StatusCode = (int)statusCode
            };

            context.ExceptionHandled = true;

            Logger.LogInformation("Exception handled by {FilterType} for {ControllerName}.{ActionName}", 
                GetType().Name, controllerName, actionName);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred while handling exception in {FilterType}", GetType().Name);
            
            var fallbackProblem = new Problem
            {
                Title = Titles.UnexpectedError,
                Status = HttpStatusCode.InternalServerError,
                Instance = context.HttpContext.Request.Path
            };
            
            context.Result = new ObjectResult(Result<Problem>.Fail(Titles.UnexpectedError, fallbackProblem))
            {
                StatusCode = (int)HttpStatusCode.InternalServerError
            };
            context.ExceptionHandled = true;
        }
    }
} 