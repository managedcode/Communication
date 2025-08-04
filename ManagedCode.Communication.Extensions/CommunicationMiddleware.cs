using System;
using System.Net;
using System.Threading.Tasks;
using ManagedCode.Communication.Extensions.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ManagedCode.Communication.Extensions;

public class CommunicationMiddleware(ILogger<CommunicationMiddleware> logger, RequestDelegate next, IOptions<CommunicationOptions> options)
{
    public async Task Invoke(HttpContext httpContext)
    {
        try
        {
            await next(httpContext);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, httpContext.Request.Method + "::" + httpContext.Request.Path);

            if (httpContext.Response.HasStarted)
                throw;

            httpContext.Response.Headers.CacheControl = "no-cache,no-store";
            httpContext.Response.Headers.Pragma = "no-cache";
            httpContext.Response.Headers.Expires = "-1";
            httpContext.Response.Headers.ETag = default;

            httpContext.Response.ContentType = "application/json; charset=utf-8";
            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            if (options.Value.ShowErrorDetails)
                await httpContext.Response.WriteAsJsonAsync(Result.Fail(HttpStatusCode.InternalServerError,
                    ex.Message));
            else
                await httpContext.Response.WriteAsJsonAsync(Result.Fail(HttpStatusCode.InternalServerError,
                    nameof(HttpStatusCode.InternalServerError)));
        }
    }
}