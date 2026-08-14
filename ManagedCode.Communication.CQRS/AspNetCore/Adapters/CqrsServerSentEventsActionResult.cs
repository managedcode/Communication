using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Communication.CQRS.AspNetCore;

internal sealed class CqrsServerSentEventsActionResult(Microsoft.AspNetCore.Http.IResult result) : IActionResult
{

    public Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return result.ExecuteAsync(context.HttpContext);
    }
}
