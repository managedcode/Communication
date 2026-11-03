using System;
using System.Collections.Generic;

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

        // Visit aggregates in the same breadth-first order as Flatten.
        Queue<AggregateException>? pending = null;
        while (true)
        {
            var inners = aggregate.InnerExceptions;
            if (inners.Count == 1 && inners[0] is AggregateException onlyChild
                && (pending is null || pending.Count == 0))
            {
                aggregate = onlyChild;
                continue;
            }

            for (var i = 0; i < inners.Count; i++)
            {
                var inner = inners[i];
                if (IsFatal(inner))
                {
                    return inner;
                }

                if (inner is AggregateException nested)
                {
                    (pending ??= new Queue<AggregateException>()).Enqueue(nested);
                }
            }

            if (pending is null || !pending.TryDequeue(out var next))
            {
                return null;
            }

            aggregate = next;
        }
    }

    private static bool IsFatal(Exception exception)
        => exception is OutOfMemoryException or StackOverflowException or AccessViolationException;
}
