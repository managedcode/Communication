using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ManagedCode.Communication.Extensions.MinimalApi;

/// <summary>
///     Extension helpers for wiring ManagedCode.Communication support into Minimal API route handlers.
/// </summary>
public static class CommunicationEndpointExtensions
{
    /// <summary>
    ///     Adds <see cref="ResultEndpointFilter"/> to a specific <see cref="RouteHandlerBuilder"/> so that
    ///     <c>Result</c>-returning handlers are converted into <see cref="Microsoft.AspNetCore.Http.IResult"/> automatically.
    /// </summary>
    /// <param name="builder">The endpoint builder.</param>
    /// <returns>The same builder instance to enable fluent configuration.</returns>
    public static RouteHandlerBuilder WithCommunicationResults(this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddEndpointFilterFactory(CreateFilter);
        return builder;
    }

    /// <summary>
    ///     Adds <see cref="ResultEndpointFilter"/> to an entire <see cref="RouteGroupBuilder"/> so that every child endpoint
    ///     inherits automatic <c>Result</c> to <see cref="Microsoft.AspNetCore.Http.IResult"/> conversion.
    /// </summary>
    /// <param name="builder">The group builder.</param>
    /// <returns>The same builder instance for chaining.</returns>
    public static RouteGroupBuilder WithCommunicationResults(this RouteGroupBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddEndpointFilterFactory(CreateFilter);
        return builder;
    }

    private static EndpointFilterDelegate CreateFilter(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        var filter = new ResultEndpointFilter();
        return invocationContext => filter.InvokeAsync(invocationContext, next);
    }
}
