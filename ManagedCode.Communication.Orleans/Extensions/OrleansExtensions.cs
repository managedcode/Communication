using ManagedCode.Communication.Filters;
using Orleans.Hosting;

namespace ManagedCode.Communication.Extensions;

public static class OrleansExtensions
{
    public static ISiloBuilder UseOrleansCommunication(this ISiloBuilder builder)
    {
        return builder.AddIncomingGrainCallFilter<CommunicationIncomingGrainCallFilter>();
    }

    public static IClientBuilder UseOrleansCommunication(this IClientBuilder builder)
    {
        return builder.AddOutgoingGrainCallFilter<CommunicationOutgoingGrainCallFilter>();
    }
}