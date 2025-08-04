using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ManagedCode.Communication;

namespace ManagedCode.Communication.Extensions;

public abstract class HubExceptionFilterBase(ILogger logger) : IHubFilter
{
    protected readonly ILogger Logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        try
        {
            return await next(invocationContext);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, invocationContext.Hub.GetType().Name + "." + invocationContext.HubMethodName);

            var problem = new Problem
            {
                Title = ex.GetType().Name,
                Detail = ex.Message,
                Status = GetStatusCodeForException(ex),
                Instance = invocationContext.Hub.Context.ConnectionId,
                Extensions =
                {
                    ["hubMethod"] = invocationContext.HubMethodName,
                    ["hubType"] = invocationContext.Hub.GetType().Name
                }
            };

            return Result<Problem>.Fail(ex.Message, problem);
        }
    }

    protected virtual int GetStatusCodeForException(Exception exception)
    {
        return exception switch
        {
            ArgumentException or ArgumentNullException => 400,
            UnauthorizedAccessException => 401,
            InvalidOperationException => 400,
            NotSupportedException => 400,
            TimeoutException => 408,
            TaskCanceledException => 408,
            _ => 500
        };
    }
} 