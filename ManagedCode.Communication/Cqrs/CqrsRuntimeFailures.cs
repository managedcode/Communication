using System;

namespace ManagedCode.Communication.CQRS;

/// <summary>Classifies fatal runtime failures without converting them into recoverable CQRS results.</summary>
public static class CqrsRuntimeFailures
{
    /// <summary>Finds a fatal runtime exception directly or inside a native aggregate failure.</summary>
    /// <param name="exception">The observed exception, or null when there is no failure.</param>
    /// <returns>The direct fatal exception or first flattened fatal inner exception; otherwise null.</returns>
    public static Exception? FindFatal(Exception? exception)
    {
        if (exception is null)
        {
            return null;
        }

        if (IsFatal(exception))
        {
            return exception;
        }

        if (exception is not AggregateException aggregate)
        {
            return null;
        }

        foreach (var inner in aggregate.Flatten().InnerExceptions)
        {
            if (IsFatal(inner))
            {
                return inner;
            }
        }

        return null;
    }

    private static bool IsFatal(Exception exception)
        => exception is OutOfMemoryException or StackOverflowException or AccessViolationException;
}
