using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication.Logging;

public static class CommunicationLogger
{
    private static IServiceProvider? _serviceProvider;
    private static ILoggerFactory? _fallbackLoggerFactory;
    private static ILoggerFactory? _lastResortLoggerFactory;
    private static ILogger<Result>? _logger;

    public static void Configure(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = null;
    }

    public static void Configure(ILoggerFactory loggerFactory)
    {
        _fallbackLoggerFactory = loggerFactory;
        _logger = null;
    }

    public static ILogger<Result> GetLogger()
    {
        if (_logger != null)
            return _logger;
            
        _logger = CreateLogger();
        return _logger;
    }

    private static ILogger<Result> CreateLogger()
    {
        var logger = _serviceProvider?.GetService<ILogger<Result>>();
        if (logger != null)
            return logger;

        if (_fallbackLoggerFactory != null)
        {
            return new Logger<Result>(_fallbackLoggerFactory);
        }

        _lastResortLoggerFactory ??= LoggerFactory.Create(builder => 
            builder.SetMinimumLevel(LogLevel.Warning));
            
        return new Logger<Result>(_lastResortLoggerFactory);
    }
}
