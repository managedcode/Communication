using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication.Extensions;

public class CommunicationHubFilter : IHubFilter
{
    private readonly ILogger<CommunicationHubFilter> _logger;

    public CommunicationHubFilter(ILogger<CommunicationHubFilter> logger)
    {
        _logger = logger;
    }

    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        try
        {
            return await next(invocationContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, invocationContext.Hub.GetType().Name + "." + invocationContext.HubMethodName);
            var result = Result.Fail(HttpStatusCode.InternalServerError, ex.Message);
            return result;
        }
    }
}