using System;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication.Extensions;

internal static class LoggerExtensions
{
    internal static void LogException(this ILogger logger, Exception exception)
    {
        logger.LogError(exception, exception.Message);
    }
}