using System;
using System.Net;
using ManagedCode.Communication.Helpers;
using Orleans.Runtime;

namespace ManagedCode.Communication.Orleans.Helpers;

public static class OrleansHttpStatusCodeHelper
{
    public static HttpStatusCode GetStatusCodeForException(Exception exception)
    {
        // First check Orleans-specific exceptions
        var orleansStatusCode = exception switch
        {
            // Orleans-specific exceptions
            SiloUnavailableException => HttpStatusCode.ServiceUnavailable,
            OrleansMessageRejectionException => HttpStatusCode.ServiceUnavailable,
            TimeoutException => HttpStatusCode.GatewayTimeout,
            GrainExtensionNotInstalledException => HttpStatusCode.NotImplemented,
            OrleansConfigurationException => HttpStatusCode.InternalServerError,
            
            OrleansException => HttpStatusCode.InternalServerError,

            
            _ => (HttpStatusCode?)null
        };

        // If we found an Orleans-specific status code, return it
        if (orleansStatusCode.HasValue)
        {
            return orleansStatusCode.Value;
        }

        // Otherwise, try the base helper for standard .NET exceptions
        return HttpStatusCodeHelper.GetStatusCodeForException(exception);
    }
}