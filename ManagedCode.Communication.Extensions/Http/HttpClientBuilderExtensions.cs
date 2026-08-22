using System;
using System.Net.Http;
using ManagedCode.Communication.Commands.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication.Extensions.Http;

/// <summary>Registers native command reliability in <see cref="IHttpClientFactory"/> pipelines.</summary>
public static class HttpClientBuilderExtensions
{
    /// <summary>Adds retry and circuit breaking backed by <see cref="CommandExecutor"/>.</summary>
    public static IHttpClientBuilder AddCommunicationResilienceHandler(
        this IHttpClientBuilder builder,
        Action<CommandHttpClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddHttpMessageHandler(serviceProvider =>
        {
            var options = new CommandHttpClientOptions();
            configure?.Invoke(options);
            var logger = serviceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger<CommunicationResilienceHandler>();
            return new CommunicationResilienceHandler(options, logger);
        });
    }

    /// <summary>
    ///     Removes inherited Communication resilience handlers from this named client. A handler added after this call
    ///     remains active, allowing a client to replace shared defaults with a specific policy.
    /// </summary>
    public static IHttpClientBuilder RemoveCommunicationResilienceHandler(this IHttpClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureAdditionalHttpMessageHandlers(static (handlers, _) =>
        {
            for (var index = handlers.Count - 1; index >= 0; index--)
            {
                if (handlers[index] is CommunicationResilienceHandler)
                {
                    handlers.RemoveAt(index);
                }
            }
        });
        return builder;
    }
}
