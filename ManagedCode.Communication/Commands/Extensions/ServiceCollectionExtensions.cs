using System;
using ManagedCode.Communication.Commands.Execution;
using ManagedCode.Communication.Commands.Stores;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

        services.AddOptions<CommandExecutionOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<CommandExecutionOptions>>().Value);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ICommandCircuitBreaker>(serviceProvider =>
            new PartitionedCommandCircuitBreaker(
                serviceProvider.GetRequiredService<CommandExecutionOptions>().CircuitBreaker,
                serviceProvider.GetRequiredService<TimeProvider>()));
        services.TryAddSingleton<ICommandCircuitBreakerStateProvider>(serviceProvider =>
            (ICommandCircuitBreakerStateProvider)serviceProvider.GetRequiredService<ICommandCircuitBreaker>());
        services.TryAddSingleton<CommandExecutionRuntime>(serviceProvider =>
            new CommandExecutionRuntime(
                serviceProvider.GetRequiredService<CommandExecutionOptions>(),
                serviceProvider.GetService<ICommandIdempotencyStore>(),
                serviceProvider.GetService<ICommandRateLimiter>(),
                serviceProvider.GetRequiredService<TimeProvider>(),
                serviceProvider.GetService<ILogger<DefaultCommandExecutor>>(),
                serviceProvider.GetService<ICommandCircuitBreaker>()));
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
        services.TryAddSingleton<MemoryCacheCommandIdempotencyStore>();
        services.TryAddSingleton<ICommandIdempotencyStore>(serviceProvider =>
            serviceProvider.GetRequiredService<MemoryCacheCommandIdempotencyStore>());
        services.TryAddSingleton<ICommandIdempotencyMaintenance>(serviceProvider =>
            serviceProvider.GetRequiredService<MemoryCacheCommandIdempotencyStore>());

        return services;
    }

    /// <summary>
    /// Adds command idempotency with custom store type
    /// </summary>
    public static IServiceCollection AddCommandIdempotency<TStore>(
        this IServiceCollection services)
        where TStore : class, ICommandIdempotencyStore
    {
        services.TryAddSingleton<TStore>();
        services.TryAddSingleton<ICommandIdempotencyStore>(serviceProvider =>
            serviceProvider.GetRequiredService<TStore>());
        if (typeof(ICommandIdempotencyMaintenance).IsAssignableFrom(typeof(TStore)))
        {
            services.TryAddSingleton<ICommandIdempotencyMaintenance>(serviceProvider =>
                (ICommandIdempotencyMaintenance)serviceProvider.GetRequiredService<TStore>());
        }

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
        if (store is ICommandIdempotencyMaintenance maintenance)
        {
            services.AddSingleton(maintenance);
        }

        return services;
    }
}
