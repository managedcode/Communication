using System;
using System.Net;
using System.Threading.Tasks;
using ManagedCode.Communication.Logging;
using Microsoft.Extensions.Logging;
using Orleans;

namespace ManagedCode.Communication.Filters;

public class CommunicationIncomingGrainCallFilter(ILogger<CommunicationIncomingGrainCallFilter> logger) : IIncomingGrainCallFilter
{
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        try
        {
            await context.Invoke();
        }
        catch (Exception exception)
        {
            if (CommunicationGrainCallResultFactory.TrySetFailure(context, exception, out var statusCode))
            {
                LogExceptionConverted(context, exception, (int)statusCode);
                return;
            }

            LogExceptionConverted(context, exception, (int)HttpStatusCode.InternalServerError);

            throw;
        }
    }

    private void LogExceptionConverted(IIncomingGrainCallContext context, Exception exception, int statusCode)
    {
        LoggerCenter.LogOrleansGrainCallExceptionConverted(
            logger,
            exception,
            context.InterfaceName,
            context.MethodName,
            context.TargetId.ToString(),
            statusCode);
    }
}
