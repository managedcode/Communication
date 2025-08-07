using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using static ManagedCode.Communication.AspNetCore.Helpers.HttpStatusCodeHelper;

namespace ManagedCode.Communication.AspNetCore.Filters;

public class CommunicationHubExceptionFilter(ILogger<CommunicationHubExceptionFilter> logger) : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        try
        {
            return await next(invocationContext);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in hub method {HubType}.{HubMethod}", invocationContext.Hub.GetType()
                .Name, invocationContext.HubMethodName);

            var statusCode = GetStatusCodeForException(ex);
            return Result.Fail(ex, statusCode);
        }
    }
}