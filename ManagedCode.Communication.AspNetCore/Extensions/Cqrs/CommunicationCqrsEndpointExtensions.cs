using CqrsEndpointExtensions = ManagedCode.Communication.CQRS.AspNetCore.Extensions.CommunicationCqrsEndpointExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace ManagedCode.Communication.AspNetCore.Extensions;

/// <summary>
///     Backward/monolithic package facade for CQRS streaming endpoint mapping helpers.
/// </summary>
public static class CommunicationCqrsEndpointExtensions
{
    /// <summary>
    ///     Adds CQRS stream result conversion to a single Minimal API endpoint.
    /// </summary>
    /// <param name="builder">Endpoint builder.</param>
    /// <returns>The same builder for fluent configuration.</returns>
    public static RouteHandlerBuilder WithCommunicationCqrsResults(this RouteHandlerBuilder builder)
    {
        return CqrsEndpointExtensions.WithCommunicationCqrsResults(builder);
    }

    /// <summary>
    ///     Adds CQRS stream result conversion to a route group.
    /// </summary>
    /// <param name="builder">Route group builder.</param>
    /// <returns>The same builder for fluent configuration.</returns>
    public static RouteGroupBuilder WithCommunicationCqrsResults(this RouteGroupBuilder builder)
    {
        return CqrsEndpointExtensions.WithCommunicationCqrsResults(builder);
    }
}
