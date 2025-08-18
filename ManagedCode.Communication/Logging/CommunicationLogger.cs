using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication.Logging;

/// <summary>
/// Static logger for Communication library that uses DI when available
/// </summary>
public static class CommunicationLogger
{
    private static IServiceProvider? _serviceProvider;
    private static ILoggerFactory? _fallbackLoggerFactory;

    /// <summary>
    /// Configure the service provider for logger resolution
    /// </summary>
    public static void Configure(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Configure fallback logger factory when DI is not available
    /// </summary>
    public static void Configure(ILoggerFactory loggerFactory)
    {
        _fallbackLoggerFactory = loggerFactory;
    }

    /// <summary>
    /// Get logger for specified type
    /// </summary>
    public static ILogger<T> GetLogger<T>()
    {
        // Try to get from DI first
        var logger = _serviceProvider?.GetService<ILogger<T>>();
        if (logger != null)
            return logger;

        // Fallback to configured logger factory
        if (_fallbackLoggerFactory != null)
        {
            return new Logger<T>(_fallbackLoggerFactory);
        }

        // Last resort - create minimal logger factory
        var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));
        return new Logger<T>(loggerFactory);
    }

    /// <summary>
    /// Get logger by name
    /// </summary>
    public static ILogger GetLogger(string categoryName)
    {
        // Try to get from DI first
        if (_serviceProvider != null)
        {
            var loggerFactory = _serviceProvider.GetService<ILoggerFactory>();
            if (loggerFactory != null) return loggerFactory.CreateLogger(categoryName);
        }

        // Fallback to configured logger factory
        if (_fallbackLoggerFactory != null)
        {
            return _fallbackLoggerFactory.CreateLogger(categoryName);
        }

        // Last resort - create minimal logger factory
        var factory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));
        return factory.CreateLogger(categoryName);
    }
}
