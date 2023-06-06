using ManagedCode.Communication.Filters;
using Orleans.Hosting;

namespace ManagedCode.Communication.Extensions;

public static class OrleansExtensions
{
    public static ISiloBuilder UseOrleansCommunication(this ISiloBuilder siloBuilder)
    {
        return siloBuilder.AddIncomingGrainCallFilter<CommunicationIncomingGrainCallFilter>();
    }

    public static IClientBuilder UseOrleansCommunication(this IClientBuilder clientBuilder)
    {
        return clientBuilder.AddOutgoingGrainCallFilter<CommunicationOutgoingGrainCallFilter>();
    }
}

