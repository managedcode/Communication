using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ManagedCode.Communication.Logging;
using ManagedCode.Communication.AspNetCore.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Communication.AspNetCore.Extensions;

/// <summary>
/// Extension methods for configuring Communication library in ASP.NET Core applications
/// </summary>
public static class CommunicationServiceCollectionExtensions
{
    /// <summary>
    /// Configures Communication library to use the service provider for logging in ASP.NET Core applications.
    /// Uses a hosted service to configure logging after DI container is fully built.
    /// </summary>
    public static IServiceCollection AddCommunicationAspNetCore(this IServiceCollection services)
    {
        services.AddHostedService<CommunicationLoggerConfigurationService>();
        return services;
    }

    /// <summary>
    /// Configures Communication library with a specific logger factory
    /// </summary>
    public static IServiceCollection AddCommunicationAspNetCore(this IServiceCollection services, ILoggerFactory loggerFactory)
    {
        CommunicationLogger.Configure(loggerFactory);
        return services;
    }

    /// <summary>
    /// Adds Communication filters to MVC controllers.
    /// This is a legacy method for backward compatibility.
    /// </summary>
    public static IServiceCollection AddCommunicationFilters(this IServiceCollection services)
    {
        services.Configure<MvcOptions>(options =>
        {
            options.AddCommunicationFilters();
        });
        return services;
    }

    /// <summary>
    /// Configures Communication library for ASP.NET Core with options.
    /// This is a legacy method for backward compatibility.
    /// </summary>
    public static IServiceCollection AddCommunication(this IServiceCollection services, Action<CommunicationOptions>? configure = null)
    {
        var options = new CommunicationOptions();
        configure?.Invoke(options);
        
        services.AddCommunicationAspNetCore();
        services.AddCommunicationFilters();
        
        return services;
    }
}

/// <summary>
/// Hosted service that configures the static logger after DI container is built
/// </summary>
internal class CommunicationLoggerConfigurationService(IServiceProvider serviceProvider) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Configure the static logger with the fully built service provider
        CommunicationLogger.Configure(serviceProvider);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}