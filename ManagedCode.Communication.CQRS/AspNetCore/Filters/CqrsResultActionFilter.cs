using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ManagedCode.Communication.CQRS.AspNetCore.Filters;

/// <summary>
///     Result filter that converts streamed CQRS chunks into Server-Sent Events in MVC actions.
/// </summary>
public sealed class CqrsResultActionFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (context.Result is ObjectResult objectResult &&
            objectResult.Value is not null &&
            CqrsStreamResultFactory.TryCreateServerSentEventsActionResult(objectResult.Value, out var convertedResult))
        {
            context.Result = convertedResult;
        }

        await next();
    }
}
