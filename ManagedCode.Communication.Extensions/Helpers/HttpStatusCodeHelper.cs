using System;
using System.Net;
using ManagedCode.Communication.Helpers;

namespace ManagedCode.Communication.Extensions.Helpers;

public static class HttpStatusCodeHelper
{
    public static HttpStatusCode GetStatusCodeForException(Exception exception)
    {
        // First check ASP.NET/SignalR-specific exceptions
        var aspNetStatusCode = exception switch
        {
            // ASP.NET Core specific exceptions could go here
            // For now, we don't have any specific ones, but this is where they would be added
            
            _ => (HttpStatusCode?)null
        };

        // If we found an ASP.NET-specific status code, return it
        if (aspNetStatusCode.HasValue)
        {
            return aspNetStatusCode.Value;
        }

        // Otherwise, use the base helper for standard .NET exceptions
        return ManagedCode.Communication.Helpers.HttpStatusCodeHelper.GetStatusCodeForException(exception);
    }
}