using System;
using ManagedCode.Communication.CQRS.AspNetCore.Filters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ManagedCode.Communication.CQRS.AspNetCore.Extensions;

/// <summary>
///     Extension helpers for wiring CQRS stream support into Minimal API route handlers.
/// </summary>
public static class CommunicationCqrsEndpointExtensions
{
    public static RouteHandlerBuilder WithCommunicationCqrsResults(this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddEndpointFilterFactory(CreateFilter);
        return builder;
    }

    public static RouteGroupBuilder WithCommunicationCqrsResults(this RouteGroupBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddEndpointFilterFactory(CreateFilter);
        return builder;
    }

    private static EndpointFilterDelegate CreateFilter(EndpointFilterFactoryContext _, EndpointFilterDelegate next)
    {
        var filter = new CqrsResultEndpointFilter();
        return invocationContext => filter.InvokeAsync(invocationContext, next);
    }
}
