using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ManagedCode.Communication.Logging;

namespace ManagedCode.Communication.Extensions;

/// <summary>
/// Extension methods for configuring Communication library services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures Communication library to use the service provider for logging
    /// </summary>
    public static IServiceCollection ConfigureCommunication(this IServiceCollection services)
    {
        // Configure the static logger to use DI
        var serviceProvider = services.BuildServiceProvider();
        CommunicationLogger.Configure(serviceProvider);
        
        return services;
    }

    /// <summary>
    /// Configures Communication library with a specific logger factory
    /// </summary>
    public static IServiceCollection ConfigureCommunication(this IServiceCollection services, ILoggerFactory loggerFactory)
    {
        CommunicationLogger.Configure(loggerFactory);
        return services;
    }
}