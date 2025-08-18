using System;
using Microsoft.Extensions.DependencyInjection;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.AspNetCore.Extensions;

namespace ManagedCode.Communication.AspNetCore.Extensions;

/// <summary>
/// Extension methods for configuring command idempotency services
/// </summary>
public static class CommandIdempotencyServiceCollectionExtensions
{
    /// <summary>
    /// Adds command idempotency with automatic cleanup
    /// </summary>
    public static IServiceCollection AddCommandIdempotency<TStore>(
        this IServiceCollection services,
        Action<CommandCleanupOptions>? configureCleanup = null)
        where TStore : class, ICommandIdempotencyStore
    {
        services.AddSingleton<ICommandIdempotencyStore, TStore>();
        
        // Configure cleanup options
        var cleanupOptions = new CommandCleanupOptions();
        configureCleanup?.Invoke(cleanupOptions);
        services.AddSingleton(cleanupOptions);
        
        // Add background cleanup service
        services.AddHostedService<CommandCleanupBackgroundService>();
        
        return services;
    }

    /// <summary>
    /// Adds command idempotency with custom store instance
    /// </summary>
    public static IServiceCollection AddCommandIdempotency(
        this IServiceCollection services,
        ICommandIdempotencyStore store,
        Action<CommandCleanupOptions>? configureCleanup = null)
    {
        services.AddSingleton(store);
        
        // Configure cleanup options
        var cleanupOptions = new CommandCleanupOptions();
        configureCleanup?.Invoke(cleanupOptions);
        services.AddSingleton(cleanupOptions);
        
        // Add background cleanup service
        services.AddHostedService<CommandCleanupBackgroundService>();
        
        return services;
    }

    /// <summary>
    /// Adds only command idempotency store without cleanup (for custom cleanup scenarios)
    /// </summary>
    public static IServiceCollection AddCommandIdempotencyStore<TStore>(
        this IServiceCollection services)
        where TStore : class, ICommandIdempotencyStore
    {
        services.AddSingleton<ICommandIdempotencyStore, TStore>();
        return services;
    }

    /// <summary>
    /// Adds command idempotency with manual cleanup control
    /// </summary>
    public static IServiceCollection AddCommandIdempotencyWithManualCleanup<TStore>(
        this IServiceCollection services,
        CommandCleanupOptions cleanupOptions)
        where TStore : class, ICommandIdempotencyStore
    {
        services.AddSingleton<ICommandIdempotencyStore, TStore>();
        services.AddSingleton(cleanupOptions);
        
        // Manual cleanup - no background service
        return services;
    }
}