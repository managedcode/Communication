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
        ConfigureServices(siloBuilder.Services, configureRateLimiting);
        siloBuilder.AddOrleansRateLimiting();
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
        clientBuilder.Services.AddOrleansRateLimitingCore();
        ConfigureServices(clientBuilder.Services, configureRateLimiting);
        return clientBuilder.AddOutgoingGrainCallFilter<CommunicationOutgoingGrainCallFilter>();
    }

    private static void ConfigureServices(
        IServiceCollection services,
        Action<OrleansCommandRateLimiterOptions>? configureRateLimiting)
    {
        var options = new OrleansCommandRateLimiterOptions();
        configureRateLimiting?.Invoke(options);
        services.TryAddSingleton(options);
        services.TryAddSingleton<ICommandIdempotencyStore, OrleansCommandIdempotencyStore>();
        services.TryAddSingleton<ICommandRateLimiter, OrleansCommandRateLimiter>();
        services.AddCommandExecution();
    }
}
