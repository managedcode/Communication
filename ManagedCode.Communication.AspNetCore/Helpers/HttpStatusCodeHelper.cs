using System;
using System.Net;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.SignalR;

namespace ManagedCode.Communication.AspNetCore.Helpers;

public static class HttpStatusCodeHelper
{
    public static HttpStatusCode GetStatusCodeForException(Exception exception)
    {
        // First check ASP.NET/SignalR-specific exceptions
        var aspNetStatusCode = exception switch
        {
            // ASP.NET Core specific exceptions
            BadHttpRequestException => HttpStatusCode.BadRequest,
            ConnectionAbortedException => HttpStatusCode.BadRequest,
            ConnectionResetException => HttpStatusCode.BadRequest,
            AmbiguousActionException => HttpStatusCode.InternalServerError,
            AuthenticationFailureException => HttpStatusCode.Unauthorized,

            // SignalR specific exceptions
            HubException => HttpStatusCode.BadRequest,

            // Antiforgery
            AntiforgeryValidationException => HttpStatusCode.BadRequest,


            _ => (HttpStatusCode?)null
        };

        // If we found an ASP.NET-specific status code, return it
        if (aspNetStatusCode.HasValue)
        {
            return aspNetStatusCode.Value;
        }

        // Otherwise, use the base helper for standard .NET exceptions
        return Communication.Helpers.HttpStatusCodeHelper.GetStatusCodeForException(exception);
    }
}