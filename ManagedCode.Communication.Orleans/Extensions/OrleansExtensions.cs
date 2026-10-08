using System;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Commands.Execution;
using ManagedCode.Communication.Commands.Extensions;
using ManagedCode.Communication.Orleans.Filters;
using ManagedCode.Communication.Orleans.RateLimiting;
using ManagedCode.Communication.Orleans.Stores;
using ManagedCode.Orleans.RateLimiting.Core.Extensions;
using ManagedCode.Orleans.RateLimiting.Server.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Hosting;

namespace ManagedCode.Communication.Orleans.Extensions;

/// <summary>
///     Registers the Communication grain call filters and serialization surrogates with an Orleans silo or client.
/// </summary>
public static class OrleansExtensions
{
    /// <summary>
    ///     Adds the Communication grain call filters to a silo.
    /// </summary>
    public static ISiloBuilder UseOrleansCommunication(
        this ISiloBuilder siloBuilder,
        Action<OrleansCommandRateLimiterOptions>? configureRateLimiting = null)
    {
        ArgumentNullException.ThrowIfNull(siloBuilder);
        ConfigureRateLimiterOptions(siloBuilder.Services, configureRateLimiting);
        return siloBuilder.AddIncomingGrainCallFilter<CommunicationIncomingGrainCallFilter>();
    }

    /// <summary>
    ///     Adds the Communication grain call filters to a client.
    /// </summary>
    public static IClientBuilder UseOrleansCommunication(
        this IClientBuilder clientBuilder,
        Action<OrleansCommandRateLimiterOptions>? configureRateLimiting = null)
    {
        ArgumentNullException.ThrowIfNull(clientBuilder);
        ConfigureRateLimiterOptions(clientBuilder.Services, configureRateLimiting);
        return clientBuilder.AddOutgoingGrainCallFilter<CommunicationOutgoingGrainCallFilter>();
    }

    /// <summary>
    ///     Explicitly enables Orleans-backed command idempotency and cluster-wide rate limiting for a silo. Configure
    ///     grain storage named <c>commandStore</c> before using idempotency.
    /// </summary>
    public static ISiloBuilder UseOrleansCommandExecution(
        this ISiloBuilder siloBuilder,
        Action<CommandExecutionOptions>? configureExecution = null,
        Action<OrleansCommandRateLimiterOptions>? configureRateLimiting = null)
    {
        ArgumentNullException.ThrowIfNull(siloBuilder);
        ConfigureCommandExecution(siloBuilder.Services, configureExecution, configureRateLimiting);
        siloBuilder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigurationValidator, OrleansCommandExecutionStorageValidator>());
        siloBuilder.AddOrleansRateLimiting();
        return siloBuilder;
    }

    /// <summary>Explicitly enables Orleans-backed command execution for a client.</summary>
    public static IClientBuilder UseOrleansCommandExecution(
        this IClientBuilder clientBuilder,
        Action<CommandExecutionOptions>? configureExecution = null,
        Action<OrleansCommandRateLimiterOptions>? configureRateLimiting = null)
    {
        ArgumentNullException.ThrowIfNull(clientBuilder);
        clientBuilder.Services.AddOrleansRateLimitingCore();
        ConfigureCommandExecution(clientBuilder.Services, configureExecution, configureRateLimiting);
        return clientBuilder;
    }

    private static void ConfigureCommandExecution(
        IServiceCollection services,
        Action<CommandExecutionOptions>? configureExecution,
        Action<OrleansCommandRateLimiterOptions>? configureRateLimiting)
    {
        ConfigureRateLimiterOptions(services, configureRateLimiting);
        services.TryAddSingleton<ICommandIdempotencyStore, OrleansCommandIdempotencyStore>();
        services.TryAddSingleton<ICommandRateLimiter, OrleansCommandRateLimiter>();
        services.AddCommandExecution(configureExecution);
    }

    private static void ConfigureRateLimiterOptions(
        IServiceCollection services,
        Action<OrleansCommandRateLimiterOptions>? configureRateLimiting)
    {
        services.AddOptions<OrleansCommandRateLimiterOptions>();
        if (configureRateLimiting is not null)
        {
            services.Configure(configureRateLimiting);
        }

        services.TryAddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<OrleansCommandRateLimiterOptions>>().Value);
    }
}
