using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AspNetResult = Microsoft.AspNetCore.Http.IResult;

namespace ManagedCode.Communication.AspNetCore;

/// <summary>
///     Adapts a Minimal API <see cref="AspNetResult" /> so it can be returned from the MVC result pipeline.
/// </summary>
internal sealed class CqrsServerSentEventsActionResult : IActionResult
{
    private readonly AspNetResult _result;

    public CqrsServerSentEventsActionResult(AspNetResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        _result = result;
    }

    public Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _result.ExecuteAsync(context.HttpContext);
    }
}
