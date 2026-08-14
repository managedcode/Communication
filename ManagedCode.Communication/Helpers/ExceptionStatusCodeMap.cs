using System;
using System.Collections.Concurrent;
using System.Net;

namespace ManagedCode.Communication.Helpers;

/// <summary>
///     Application-controlled overrides for the exception-to-status-code mapping.
/// </summary>
/// <remarks>
///     <para>
///         The built-in mapping is a heuristic and cannot know your domain. Register overrides once at startup,
///         before serving traffic; the map is a process-wide singleton and is not meant to change per request.
///     </para>
///     <para>
///         Lookup walks the exception's type hierarchy, so mapping a base type covers everything derived from it.
///         The most derived registration wins.
///     </para>
///     <example>
///         <code>
///         ExceptionStatusCodeMap.Map&lt;OrderNotFoundException&gt;(HttpStatusCode.NotFound);
///         ExceptionStatusCodeMap.Map&lt;DomainRuleViolationException&gt;(HttpStatusCode.UnprocessableEntity);
///         </code>
///     </example>
/// </remarks>
public static class ExceptionStatusCodeMap
{
    private static readonly ConcurrentDictionary<Type, HttpStatusCode> Overrides = new();

    /// <summary>
    ///     Maps an exception type — and anything derived from it — to a status code.
    /// </summary>
    public static void Map<TException>(HttpStatusCode statusCode)
        where TException : Exception
    {
        Overrides[typeof(TException)] = statusCode;
    }

    /// <summary>
    ///     Maps an exception type — and anything derived from it — to a status code.
    /// </summary>
    public static void Map(Type exceptionType, HttpStatusCode statusCode)
    {
        ArgumentNullException.ThrowIfNull(exceptionType);

        if (!typeof(Exception).IsAssignableFrom(exceptionType))
        {
            throw new ArgumentException($"'{exceptionType}' is not an exception type.", nameof(exceptionType));
        }

        Overrides[exceptionType] = statusCode;
    }

    /// <summary>
    ///     Removes a previously registered override. Returns <c>true</c> when one was present.
    /// </summary>
    public static bool Remove<TException>()
        where TException : Exception
    {
        return Overrides.TryRemove(typeof(TException), out _);
    }

    /// <summary>
    ///     Clears every override, restoring the built-in mapping. Intended for tests.
    /// </summary>
    public static void Reset()
    {
        Overrides.Clear();
    }

    /// <summary>
    ///     Finds the override that applies to <paramref name="exception" />, preferring the most derived match.
    /// </summary>
    internal static bool TryResolve(Exception exception, out HttpStatusCode statusCode)
    {
        if (Overrides.IsEmpty)
        {
            statusCode = default;
            return false;
        }

        for (var type = exception.GetType(); type is not null; type = type.BaseType)
        {
            if (Overrides.TryGetValue(type, out statusCode))
            {
                return true;
            }
        }

        statusCode = default;
        return false;
    }
}
