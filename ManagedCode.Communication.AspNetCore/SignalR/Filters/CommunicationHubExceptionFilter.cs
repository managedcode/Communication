using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ManagedCode.Communication.Logging;
using ManagedCode.Communication.Telemetry;
using static ManagedCode.Communication.AspNetCore.Helpers.HttpStatusCodeHelper;

namespace ManagedCode.Communication.AspNetCore.Filters;

/// <summary>
///     Hub filter that turns an exception thrown by a hub method into a failed result instead of faulting the connection.
/// </summary>
public class CommunicationHubExceptionFilter(ILogger<CommunicationHubExceptionFilter> logger) : IHubFilter
{
    /// <summary>
    ///     Invokes the hub method, converting any exception into a failed result.
    /// </summary>
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        try
        {
            return await next(invocationContext);
        }
        catch (Exception ex)
        {
            LoggerCenter.LogHubException(logger, ex, invocationContext.Hub.GetType().Name, invocationContext.HubMethodName);

            var statusCode = GetStatusCodeForException(ex);
            CommunicationTelemetry.RecordFailure(Problem.Create(ex, (int)statusCode), ex);
            return Result.Fail(ex, statusCode);
        }
    }
}