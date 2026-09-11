using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Logging;
using ManagedCode.Communication.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace ManagedCode.Communication.Extensions.Telemetry;

/// <summary>Registers Communication signals with the application's OpenTelemetry providers.</summary>
public static class CommunicationOpenTelemetryExtensions
{
    /// <summary>Subscribes to Communication traces and metrics and connects failure logs at host startup.</summary>
    /// <remarks>Call in each service's ServiceDefaults. Configure exporters and OpenTelemetry logging in the application.</remarks>
    public static TBuilder AddCommunicationTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddOpenTelemetry().WithCommunication();
        return builder;
    }

    /// <summary>Adds Communication traces, metrics, and host logger registration without replacing existing providers.</summary>
    public static OpenTelemetryBuilder WithCommunication(this OpenTelemetryBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddHostedService<CommunicationTelemetryLoggerService>();
        return builder.WithTracing(tracing => tracing.AddCommunicationInstrumentation())
            .WithMetrics(metrics => metrics.AddCommunicationInstrumentation());
    }

    /// <summary>Subscribes to Communication activities.</summary>
    public static TracerProviderBuilder AddCommunicationInstrumentation(this TracerProviderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddSource(CommunicationTelemetry.SourceName);
    }

    /// <summary>Subscribes to Communication meters.</summary>
    public static MeterProviderBuilder AddCommunicationInstrumentation(this MeterProviderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddMeter(CommunicationTelemetry.SourceName);
    }
}

internal sealed class CommunicationTelemetryLoggerService(IServiceProvider serviceProvider) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        CommunicationLogger.Configure(serviceProvider);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
