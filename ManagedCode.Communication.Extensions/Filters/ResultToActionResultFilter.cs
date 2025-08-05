using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ManagedCode.Communication.Extensions.Filters;

public class ResultToActionResultFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult objectResult && objectResult.Value != null)
        {
            var valueType = objectResult.Value.GetType();

            // Check if it's a Result or Result<T>
            if (typeof(IResult).IsAssignableFrom(valueType))
            {
                var result = (IResult)objectResult.Value;

                // Set the HTTP status code based on the Result's Problem
                if (result.IsFailed && result.Problem != null)
                {
                    objectResult.StatusCode = result.Problem.StatusCode;
                }
                else if (result.IsSuccess)
                {
                    objectResult.StatusCode = 200; // OK for successful results
                }
            }
        }

        await next();
    }
}