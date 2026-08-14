using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ManagedCode.Communication.AspNetCore.Filters;

public class ResultToActionResultFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        // A plain pattern match instead of Type.IsAssignableFrom: this filter runs on every response of
        // every action, so the reflection call was pure overhead on a hot path.
        if (context.Result is ObjectResult { Value: IResult result } objectResult)
        {
            if (result.IsFailed && result.Problem is { StatusCode: not 0 } problem)
            {
                objectResult.StatusCode = problem.StatusCode;
            }
            else if (result.IsSuccess)
            {
                // Only fill in a status the action did not choose. Overwriting would turn a deliberate
                // 201/202/204 into a 200.
                objectResult.StatusCode ??= StatusCodes.Status200OK;
            }
        }

        await next().ConfigureAwait(false);
    }
}