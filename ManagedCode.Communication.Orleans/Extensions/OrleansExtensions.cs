using ManagedCode.Communication.Orleans.Filters;
using Orleans.Hosting;

namespace ManagedCode.Communication.Orleans.Extensions;

/// <summary>
///     Registers the Communication grain call filters and serialization surrogates with an Orleans silo or client.
/// </summary>
public static class OrleansExtensions
{
    /// <summary>
    ///     Adds the Communication grain call filters to a silo.
    /// </summary>
    public static ISiloBuilder UseOrleansCommunication(this ISiloBuilder siloBuilder)
    {
        return siloBuilder.AddIncomingGrainCallFilter<CommunicationIncomingGrainCallFilter>();
    }

    /// <summary>
    ///     Adds the Communication grain call filters to a client.
    /// </summary>
    public static IClientBuilder UseOrleansCommunication(this IClientBuilder clientBuilder)
    {
        return clientBuilder.AddOutgoingGrainCallFilter<CommunicationOutgoingGrainCallFilter>();
    }
}