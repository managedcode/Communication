using System;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Communication.Extensions.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCommunication(this IServiceCollection services,
        Action<CommunicationOptions> options)
    {
        services.AddOptions<CommunicationOptions>().Configure(options);
        return services;
    }
}