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
    /// Configures Communication library with a specific logger factory.
    /// For ASP.NET Core applications, use the AspNetCore extension instead.
    /// </summary>
    public static IServiceCollection ConfigureCommunication(this IServiceCollection services, ILoggerFactory loggerFactory)
    {
        CommunicationLogger.Configure(loggerFactory);
        return services;
    }
}