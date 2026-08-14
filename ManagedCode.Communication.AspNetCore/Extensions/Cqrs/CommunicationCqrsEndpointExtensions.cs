using ManagedCode.Communication.CQRS.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using CqrsEndpointExtensions =
    ManagedCode.Communication.CQRS.AspNetCore.Extensions.CommunicationCqrsEndpointExtensions;

namespace ManagedCode.Communication.AspNetCore.Extensions;

/// <summary>
///     Facade over the CQRS Minimal API endpoint helpers for applications that depend only on the monolithic
///     <c>ManagedCode.Communication.AspNetCore</c> package.
/// </summary>
public static class CommunicationCqrsEndpointExtensions
{
    /// <inheritdoc cref="CqrsEndpointExtensions.WithCommunicationCqrsResults(RouteHandlerBuilder)" />
    public static RouteHandlerBuilder WithCommunicationCqrsResults(this RouteHandlerBuilder builder)
    {
        return CqrsEndpointExtensions.WithCommunicationCqrsResults(builder);
    }

    /// <inheritdoc cref="CqrsEndpointExtensions.WithCommunicationCqrsResults(RouteGroupBuilder)" />
    public static RouteGroupBuilder WithCommunicationCqrsResults(this RouteGroupBuilder builder)
    {
        return CqrsEndpointExtensions.WithCommunicationCqrsResults(builder);
    }

    /// <inheritdoc cref="CqrsEndpointExtensions.WithCommunicationCqrsResults(RouteHandlerBuilder,CqrsStreamServerOptions)" />
    public static RouteHandlerBuilder WithCommunicationCqrsResults(
        this RouteHandlerBuilder builder,
        CqrsStreamServerOptions options)
    {
        return CqrsEndpointExtensions.WithCommunicationCqrsResults(builder, options);
    }

    /// <inheritdoc cref="CqrsEndpointExtensions.WithCommunicationCqrsResults(RouteGroupBuilder,CqrsStreamServerOptions)" />
    public static RouteGroupBuilder WithCommunicationCqrsResults(
        this RouteGroupBuilder builder,
        CqrsStreamServerOptions options)
    {
        return CqrsEndpointExtensions.WithCommunicationCqrsResults(builder, options);
    }
}
