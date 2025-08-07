using System.Collections.Generic;
using ManagedCode.Communication.AspNetCore.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using static ManagedCode.Communication.AspNetCore.Helpers.HttpStatusCodeHelper;
using static ManagedCode.Communication.AspNetCore.Constants.ProblemConstants;

namespace ManagedCode.Communication.AspNetCore.Filters;

/// <summary>
///     Example showing how Problem and ProblemDetails are interchangeable.
/// </summary>
public abstract class ExceptionFilterWithProblemDetails(ILogger logger) : IExceptionFilter
{
    protected readonly ILogger Logger = logger;

    public virtual void OnException(ExceptionContext context)
    {
        var exception = context.Exception;
        var statusCode = GetStatusCodeForException(exception);

        // Option 1: Create ProblemDetails and convert to Problem
        var problemDetails = new ProblemDetails
        {
            Title = exception.GetType()
                .Name,
            Detail = exception.Message,
            Status = (int)statusCode,
            Instance = context.HttpContext.Request.Path
        };
        problemDetails.Extensions[ProblemConstants.ExtensionKeys.TraceId] = context.HttpContext.TraceIdentifier;

        // Convert from ProblemDetails to Problem
        var problem = problemDetails.AsProblem();
        var result1 = Result.Fail(problem);

        // Option 2: Create Problem and convert to ProblemDetails if needed
        var baseProblem = Problem.Create(exception, (int)statusCode);
        var ourProblem = baseProblem.WithExtensions(new Dictionary<string, object?> { ["instance"] = context.HttpContext.Request.Path });

        // Convert from Problem to ProblemDetails
        var convertedDetails = ourProblem.ToProblemDetails();

        // Both ways work - Problem and ProblemDetails are interchangeable
        var result2 = Result.Fail(ourProblem);

        // You can even do this - pass ProblemDetails directly
        var result3 = Result.Fail(problemDetails.AsProblem());

        context.Result = new ObjectResult(result1)
        {
            StatusCode = (int)statusCode
        };

        context.ExceptionHandled = true;
    }
}