using ManagedCode.Communication.CQRS.AspNetCore.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Communication.CQRS.AspNetCore.Extensions;

public static class CommunicationServiceCollectionExtensions
{
    /// <summary>
    ///     Adds CQRS streaming result filters to MVC pipelines.
    /// </summary>
    public static IServiceCollection AddCommunicationCqrsFilters(this IServiceCollection services)
    {
        services.Configure<MvcOptions>(options =>
        {
            options.AddCommunicationCqrsFilters();
        });

        return services;
    }

    /// <summary>
    ///     Adds CQRS streaming result filters to MVC pipelines.
    /// </summary>
    public static IServiceCollection AddCommunicationCqrs(this IServiceCollection services)
    {
        return AddCommunicationCqrsFilters(services);
    }
}
