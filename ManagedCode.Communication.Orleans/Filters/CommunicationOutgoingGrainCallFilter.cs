using System;
using System.Threading.Tasks;
using Orleans;

namespace ManagedCode.Communication.Filters;

public class CommunicationOutgoingGrainCallFilter : IOutgoingGrainCallFilter
{
    public async Task Invoke(IOutgoingGrainCallContext context)
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
