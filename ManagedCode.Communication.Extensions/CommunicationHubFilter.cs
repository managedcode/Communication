using System;
using System.Net;
using System.Threading.Tasks;
using ManagedCode.Communication.Extensions.Extensions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ManagedCode.Communication.Extensions;

public class CommunicationHubFilter : IHubFilter
{
    private readonly ILogger<CommunicationHubFilter> _logger;
    private readonly IOptions<CommunicationOptions> _options;

    public CommunicationHubFilter(ILogger<CommunicationHubFilter> logger, IOptions<CommunicationOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        try
        {
            return await next(invocationContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, invocationContext.Hub.GetType().Name + "." + invocationContext.HubMethodName);

            if (_options.Value.ShowErrorDetails)
                return Result.Fail(HttpStatusCode.InternalServerError, ex.Message);

            return Result.Fail(HttpStatusCode.InternalServerError, nameof(HttpStatusCode.InternalServerError));
        }
    }
}