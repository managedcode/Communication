using CqrsServiceCollectionExtensions = ManagedCode.Communication.CQRS.AspNetCore.Extensions.CommunicationServiceCollectionExtensions;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Communication.AspNetCore.Extensions;

/// <summary>
///     Backward/monolithic package facade for CQRS ASP.NET Core service registration.
/// </summary>
public static class CommunicationCqrsServiceCollectionExtensions
{
    /// <summary>
    ///     Adds CQRS streaming result filters to MVC.
    /// </summary>
    public static IServiceCollection AddCommunicationCqrsFilters(this IServiceCollection services)
    {
        return CqrsServiceCollectionExtensions.AddCommunicationCqrsFilters(services);
    }

    /// <summary>
    ///     Configures CQRS streaming filters. Alias for <see cref="AddCommunicationCqrsFilters"/>.
    /// </summary>
    public static IServiceCollection AddCommunicationCqrs(this IServiceCollection services)
    {
        return AddCommunicationCqrsFilters(services);
    }
}
