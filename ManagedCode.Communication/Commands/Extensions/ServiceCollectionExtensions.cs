using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using ManagedCode.Communication.Commands.Execution;
using ManagedCode.Communication.Commands.Stores;

namespace ManagedCode.Communication.Commands.Extensions;

/// <summary>
/// Extension methods for registering command idempotency services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Adds Communication's native command executor. Optional idempotency and rate-limiter services are resolved
    ///     when they have been registered by the application.
    /// </summary>
    public static IServiceCollection AddCommandExecution(
        this IServiceCollection services,
        Action<CommandExecutionOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new CommandExecutionOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<CommandExecutionRuntime>(serviceProvider =>
            new CommandExecutionRuntime(
                serviceProvider.GetRequiredService<CommandExecutionOptions>(),
                serviceProvider.GetService<ICommandIdempotencyStore>(),
                serviceProvider.GetService<ICommandRateLimiter>(),
                serviceProvider.GetRequiredService<TimeProvider>(),
                serviceProvider.GetService<ILogger<DefaultCommandExecutor>>()));
        services.TryAddSingleton<ICommandExecutor, DefaultCommandExecutor>();

        return services;
    }

    /// <summary>Adds an application-owned local or distributed command rate limiter.</summary>
    public static IServiceCollection AddCommandRateLimiter(
        this IServiceCollection services,
        ICommandRateLimiter rateLimiter)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(rateLimiter);
        services.AddSingleton(rateLimiter);
        return services;
    }

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
