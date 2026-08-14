using System;
using ManagedCode.Communication.CQRS.AspNetCore.Filters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ManagedCode.Communication.CQRS.AspNetCore.Extensions;

/// <summary>
///     Wires CQRS stream support into Minimal API route handlers.
/// </summary>
public static class CommunicationCqrsEndpointExtensions
{
    /// <summary>
    ///     Converts an <c>IAsyncEnumerable&lt;CqrsStreamChunk&lt;,&gt;&gt;</c> returned by this endpoint into a
    ///     Server-Sent Events response. Options come from DI when
    ///     <see cref="CommunicationServiceCollectionExtensions.AddCommunicationCqrs" /> was called, otherwise defaults apply.
    /// </summary>
    public static RouteHandlerBuilder WithCommunicationCqrsResults(this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddEndpointFilterFactory(CreateFilter);
        return builder;
    }

    /// <inheritdoc cref="WithCommunicationCqrsResults(RouteHandlerBuilder)" />
    public static RouteGroupBuilder WithCommunicationCqrsResults(this RouteGroupBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddEndpointFilterFactory(CreateFilter);
        return builder;
    }

    /// <summary>
    ///     Converts chunk streams to Server-Sent Events using explicit options instead of the ones registered in DI.
    /// </summary>
    public static RouteHandlerBuilder WithCommunicationCqrsResults(
        this RouteHandlerBuilder builder,
        CqrsStreamServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        builder.AddEndpointFilterFactory((_, next) => CreateFilterDelegate(options, next));
        return builder;
    }

    /// <inheritdoc cref="WithCommunicationCqrsResults(RouteHandlerBuilder,CqrsStreamServerOptions)" />
    public static RouteGroupBuilder WithCommunicationCqrsResults(
        this RouteGroupBuilder builder,
        CqrsStreamServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        builder.AddEndpointFilterFactory((_, next) => CreateFilterDelegate(options, next));
        return builder;
    }

    private static EndpointFilterDelegate CreateFilter(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        var options = context.ApplicationServices.GetService<IOptions<CqrsStreamServerOptions>>()?.Value
                      ?? CqrsStreamServerOptions.Default;

        return CreateFilterDelegate(options, next);
    }

    private static EndpointFilterDelegate CreateFilterDelegate(CqrsStreamServerOptions options, EndpointFilterDelegate next)
    {
        var filter = new CqrsResultEndpointFilter(options);
        return invocationContext => filter.InvokeAsync(invocationContext, next);
    }
}
