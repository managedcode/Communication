using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using static ManagedCode.Communication.Extensions.Helpers.HttpStatusCodeHelper;
using static ManagedCode.Communication.Extensions.Constants.ProblemConstants;

namespace ManagedCode.Communication.Extensions.Filters;

public abstract class HubExceptionFilterBase(ILogger logger) : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        try
        {
            return await next(invocationContext);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, invocationContext.Hub.GetType().Name + "." + invocationContext.HubMethodName);

            var problem = new ManagedCode.Communication.Problem
            {
                Title = ex.GetType().Name,
                Detail = ex.Message,
                StatusCode = (int)GetStatusCodeForException(ex),
                Instance = invocationContext.Hub.Context.ConnectionId,
                Extensions =
                {
                    [ExtensionKeys.HubMethod] = invocationContext.HubMethodName,
                    [ExtensionKeys.HubType] = invocationContext.Hub.GetType().Name
                }
            };

            return ManagedCode.Communication.Result.Fail(problem);
        }
    }
} 