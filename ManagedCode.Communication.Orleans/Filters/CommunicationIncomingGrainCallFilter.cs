using System;
using System.Threading.Tasks;
using Orleans;

namespace ManagedCode.Communication.Filters;

public class CommunicationIncomingGrainCallFilter : IIncomingGrainCallFilter
{
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        try
        {
            await context.Invoke();
        }
        catch (Exception exception)
        {
            if (CommunicationGrainCallResultFactory.TrySetFailure(context, exception))
            {
                return;
            }

            throw;
        }
    }
}
