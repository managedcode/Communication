using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ManagedCode.Communication.Commands.Stores;

namespace ManagedCode.Communication.Commands.Extensions;

/// <summary>
/// Extension methods for registering command idempotency services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds memory cache-based command idempotency store
    /// </summary>
    public static IServiceCollection AddCommandIdempotency(
        this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<ICommandIdempotencyStore, MemoryCacheCommandIdempotencyStore>();
        
        return services;
    }

    /// <summary>
    /// Adds command idempotency with custom store type
    /// </summary>
    public static IServiceCollection AddCommandIdempotency<TStore>(
        this IServiceCollection services)
        where TStore : class, ICommandIdempotencyStore
    {
        services.AddSingleton<ICommandIdempotencyStore, TStore>();
        
        return services;
    }

    /// <summary>
    /// Adds command idempotency with custom store instance
    /// </summary>
    public static IServiceCollection AddCommandIdempotency(
        this IServiceCollection services,
        ICommandIdempotencyStore store)
    {
        services.AddSingleton(store);
        
        return services;
    }
}