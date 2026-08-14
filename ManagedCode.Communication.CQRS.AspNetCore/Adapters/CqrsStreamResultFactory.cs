using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net.ServerSentEvents;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using AspNetActionResult = Microsoft.AspNetCore.Mvc.IActionResult;
using AspNetResult = Microsoft.AspNetCore.Http.IResult;

namespace ManagedCode.Communication.CQRS.AspNetCore;

/// <summary>
///     Detects <c>IAsyncEnumerable&lt;CqrsStreamChunk&lt;,&gt;&gt;</c> values and adapts them to a Server-Sent Events
///     response.
/// </summary>
/// <remarks>
///     Detection results are cached per runtime type: the filters run on every response of every endpoint, so the
///     reflection cost must be paid once per type rather than once per request. Types that are not chunk streams cache
///     a <c>null</c> converter so the negative case is just as cheap.
/// </remarks>
internal static class CqrsStreamResultFactory
{
    private static readonly Type StreamChunkOpenType = typeof(CqrsStreamChunk<,>);

    private static readonly MethodInfo ToServerSentEventsResultMethod =
        typeof(CqrsStreamResultFactory).GetMethod(
            nameof(ToServerSentEventsResult),
            BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException(
            $"{nameof(ToServerSentEventsResult)} is missing; the CQRS SSE adapter cannot be initialised.");

    private static readonly ConcurrentDictionary<Type, Func<object, CqrsStreamServerOptions, AspNetResult>?> Converters = new();

    /// <summary>
    ///     Converts <paramref name="result" /> to an SSE <see cref="AspNetResult" /> when it is a chunk stream.
    /// </summary>
    /// <returns><c>true</c> only when a conversion happened; <paramref name="convertedResult" /> is unset otherwise.</returns>
    public static bool TryCreateServerSentEventsResult(
        object? result,
        CqrsStreamServerOptions options,
        [NotNullWhen(true)] out AspNetResult? convertedResult)
    {
        ArgumentNullException.ThrowIfNull(options);

        convertedResult = null;

        if (result is null)
        {
            return false;
        }

        var converter = Converters.GetOrAdd(result.GetType(), CreateConverter);
        if (converter is null)
        {
            return false;
        }

        convertedResult = converter(result, options);
        return true;
    }

    /// <summary>
    ///     MVC flavour of <see cref="TryCreateServerSentEventsResult" />.
    /// </summary>
    public static bool TryCreateServerSentEventsActionResult(
        object? result,
        CqrsStreamServerOptions options,
        [NotNullWhen(true)] out AspNetActionResult? convertedResult)
    {
        if (!TryCreateServerSentEventsResult(result, options, out var serverResult))
        {
            convertedResult = null;
            return false;
        }

        convertedResult = new CqrsServerSentEventsActionResult(serverResult);
        return true;
    }

    private static Func<object, CqrsStreamServerOptions, AspNetResult>? CreateConverter(Type candidateType)
    {
        // An already-materialised IResult is the endpoint's own choice of response; never re-wrap it.
        if (typeof(AspNetResult).IsAssignableFrom(candidateType) ||
            typeof(AspNetActionResult).IsAssignableFrom(candidateType))
        {
            return null;
        }

        var itemType = TryGetChunkItemType(candidateType);
        if (itemType is null)
        {
            return null;
        }

        var arguments = itemType.GetGenericArguments();
        var method = ToServerSentEventsResultMethod.MakeGenericMethod(arguments[0], arguments[1]);

        return (value, options) => (AspNetResult)method.Invoke(null, [value, options])!;
    }

    private static Type? TryGetChunkItemType(Type candidateType)
    {
        var asyncEnumerableTypes = candidateType.GetInterfaces()
            .Append(candidateType)
            .Where(static type => type.IsGenericType &&
                                  type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));

        Type? match = null;

        foreach (var asyncEnumerableType in asyncEnumerableTypes)
        {
            var itemType = asyncEnumerableType.GetGenericArguments()[0];

            if (!itemType.IsGenericType || itemType.GetGenericTypeDefinition() != StreamChunkOpenType)
            {
                continue;
            }

            if (match is not null && match != itemType)
            {
                // Two different chunk contracts on one type: picking either would be arbitrary.
                throw new InvalidOperationException(
                    $"Type '{candidateType}' implements IAsyncEnumerable for more than one CqrsStreamChunk contract " +
                    $"('{match}' and '{itemType}'). Return a single chunk stream so the transport can pick one.");
            }

            match = itemType;
        }

        return match;
    }

    private static AspNetResult ToServerSentEventsResult<TProgress, TResult>(
        IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> updates,
        CqrsStreamServerOptions options)
    {
        return TypedResults.ServerSentEvents(ToSseItems(updates, options));
    }

    private static async IAsyncEnumerable<SseItem<CqrsStreamChunk<TProgress, TResult>>> ToSseItems<TProgress, TResult>(
        IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> updates,
        CqrsStreamServerOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var normalized = CqrsStreamNormalizer.NormalizeAsync(
            updates,
            options.AssignSequenceNumbers,
            options.EnsureTerminalChunk,
            cancellationToken);

        await foreach (var chunk in normalized.ConfigureAwait(false))
        {
            yield return new SseItem<CqrsStreamChunk<TProgress, TResult>>(chunk, chunk.EventType)
            {
                EventId = chunk.EventId ?? chunk.Sequence?.ToString(CultureInfo.InvariantCulture)
            };
        }
    }
}
