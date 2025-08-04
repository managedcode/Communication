using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

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
                Status = (int)statusCode,
                Instance = context.HttpContext.Request.Path,
                Extensions =
                {
                    ["traceId"] = context.HttpContext.TraceIdentifier
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
                Title = "An unexpected error occurred",
                Status = (int)HttpStatusCode.InternalServerError,
                Instance = context.HttpContext.Request.Path
            };
            
            context.Result = new ObjectResult(Result<Problem>.Fail("An unexpected error occurred", fallbackProblem))
            {
                StatusCode = (int)HttpStatusCode.InternalServerError
            };
            context.ExceptionHandled = true;
        }
    }
    
    protected virtual HttpStatusCode GetStatusCodeForException(Exception exception)
    {
        return exception switch
        {
            ArgumentException => HttpStatusCode.BadRequest,
            InvalidOperationException => HttpStatusCode.BadRequest,
            NotSupportedException => HttpStatusCode.BadRequest,
            FormatException => HttpStatusCode.BadRequest,
            JsonException => HttpStatusCode.BadRequest,
            XmlException => HttpStatusCode.BadRequest,
            
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            
            SecurityException => HttpStatusCode.Forbidden,
            
            FileNotFoundException => HttpStatusCode.NotFound,
            DirectoryNotFoundException => HttpStatusCode.NotFound,
            KeyNotFoundException => HttpStatusCode.NotFound,
            
            TimeoutException => HttpStatusCode.RequestTimeout,
            TaskCanceledException => HttpStatusCode.RequestTimeout,
            OperationCanceledException => HttpStatusCode.RequestTimeout,
            
            InvalidDataException => HttpStatusCode.Conflict,
            
            NotImplementedException => HttpStatusCode.NotImplemented,
            NotFiniteNumberException => HttpStatusCode.InternalServerError,
            OutOfMemoryException => HttpStatusCode.InternalServerError,
            StackOverflowException => HttpStatusCode.InternalServerError,
            ThreadAbortException => HttpStatusCode.InternalServerError,
            
            _ => HttpStatusCode.InternalServerError
        };
    }
} 