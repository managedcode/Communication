using System;
using ManagedCode.Communication.CQRS.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using CqrsServiceCollectionExtensions =
    ManagedCode.Communication.CQRS.AspNetCore.Extensions.CommunicationServiceCollectionExtensions;

namespace ManagedCode.Communication.AspNetCore.Extensions;

/// <summary>
///     Facade over the CQRS ASP.NET Core service registration for applications that depend only on the monolithic
///     <c>ManagedCode.Communication.AspNetCore</c> package.
/// </summary>
public static class CommunicationCqrsServiceCollectionExtensions
{
    /// <inheritdoc cref="CqrsServiceCollectionExtensions.AddCommunicationCqrs" />
    public static IServiceCollection AddCommunicationCqrs(
        this IServiceCollection services,
        Action<CqrsStreamServerOptions>? configure = null)
    {
        return CqrsServiceCollectionExtensions.AddCommunicationCqrs(services, configure);
    }

    /// <inheritdoc cref="CqrsServiceCollectionExtensions.AddCommunicationCqrsFilters" />
    public static IServiceCollection AddCommunicationCqrsFilters(
        this IServiceCollection services,
        Action<CqrsStreamServerOptions>? configure = null)
    {
        return CqrsServiceCollectionExtensions.AddCommunicationCqrsFilters(services, configure);
    }
}
