using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication.Extensions;

public class CommunicationMiddleware
{
    private readonly ILogger<CommunicationMiddleware> _logger;
    private readonly RequestDelegate _next;

    public CommunicationMiddleware(ILogger<CommunicationMiddleware> logger, RequestDelegate next)
    {
        _logger = logger;
        _next = next;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _next(httpContext);

        }
        catch (Exception ex) when (ex is InvalidDataException ||
                                   ex is InvalidDataContractException)
        {
            _logger.LogError("Request throw an error", ex);
            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            var result = Result.Fail(HttpStatusCode.InternalServerError, ex);
            var json = JsonSerializer.Serialize(result);
            await httpContext.Response.WriteAsJsonAsync(json);
        }
        catch (Exception ex) when (ex is ValidationException)
        {
            _logger.LogError("Request throw an error", ex);
            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            var result = Result.Fail(HttpStatusCode.InternalServerError, ex.Message);
            var json = JsonSerializer.Serialize(result);
            await httpContext.Response.WriteAsJsonAsync(json);
        }
        catch (Exception ex)
        {
            _logger.LogError("Request throw an error", ex);
            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            var result = Result.Fail(HttpStatusCode.InternalServerError, ex.Message);
            var json = JsonSerializer.Serialize(result);
            await httpContext.Response.WriteAsJsonAsync(json);
        }
        sw.Stop();
        httpContext.Response.Headers.Add("executionTime", sw.Elapsed.ToString());
    }
}