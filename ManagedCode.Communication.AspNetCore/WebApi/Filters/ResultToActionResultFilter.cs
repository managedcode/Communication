using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ManagedCode.Communication.AspNetCore.Filters;

/// <summary>
///     MVC filter that aligns the HTTP status code with a returned <c>Result</c>, leaving a status the action chose intact.
/// </summary>
public class ResultToActionResultFilter : IAsyncResultFilter
{
    /// <summary>
    ///     Sets the status code from the result before the response is written.
    /// </summary>
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