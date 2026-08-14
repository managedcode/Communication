using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Communication.AspNetCore.Extensions;

/// <summary>
///     Service registration for CQRS streaming support.
/// </summary>
public static class CommunicationCqrsServiceCollectionExtensions
{
    /// <summary>
    ///     Registers CQRS streaming support: the MVC result filter, and <see cref="CqrsStreamServerOptions" /> for both
    ///     MVC actions and Minimal API endpoints that use <c>WithCommunicationCqrsResults()</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional server-side stream behaviour.</param>
    public static IServiceCollection AddCommunicationCqrs(
        this IServiceCollection services,
        Action<CqrsStreamServerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var optionsBuilder = services.AddOptions<CqrsStreamServerOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        services.Configure<MvcOptions>(static options => options.AddCommunicationCqrsFilters());

        return services;
    }

    /// <summary>
    ///     Alias for <see cref="AddCommunicationCqrs" />.
    /// </summary>
    public static IServiceCollection AddCommunicationCqrsFilters(
        this IServiceCollection services,
        Action<CqrsStreamServerOptions>? configure = null)
    {
        return AddCommunicationCqrs(services, configure);
    }
}
