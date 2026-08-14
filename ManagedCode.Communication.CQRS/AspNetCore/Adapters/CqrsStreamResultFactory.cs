using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Threading;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Net.ServerSentEvents;
using ManagedCode.Communication;
using ManagedCode.Communication.CQRS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using AspNetResult = Microsoft.AspNetCore.Http.IResult;
using AspNetActionResult = Microsoft.AspNetCore.Mvc.IActionResult;

namespace ManagedCode.Communication.CQRS.AspNetCore;

internal static class CqrsStreamResultFactory
{
    private static readonly Type StreamChunkOpenType = typeof(CqrsStreamChunk<,>);

    public static bool TryCreateServerSentEventsResult(object? result, out AspNetResult convertedResult)
    {
        convertedResult = null!;
        if (result is null)
        {
            return false;
        }
        if (result is AspNetResult alreadyConverted)
        {
            convertedResult = alreadyConverted;
            return false;
        }

        var itemType = TryGetChunkItemType(result.GetType());
        if (itemType is null)
        {
            return false;
        }

        var arguments = itemType.GetGenericArguments();
        var progressType = arguments[0];
        var finalType = arguments[1];
        var genericMethod = typeof(CqrsStreamResultFactory).GetMethod(
            nameof(ToServerSentEventsResult),
            BindingFlags.NonPublic | BindingFlags.Static)!;
        if (genericMethod is null)
        {
            return false;
        }

        var method = genericMethod.MakeGenericMethod(progressType, finalType);
        convertedResult = (AspNetResult)method.Invoke(null, [result])!;
        return true;
    }

    public static bool TryCreateServerSentEventsActionResult(object? result, out AspNetActionResult convertedResult)
    {
        convertedResult = null!;
        if (!TryCreateServerSentEventsResult(result, out var serverResult))
        {
            return false;
        }

        convertedResult = new CqrsServerSentEventsActionResult(serverResult);
        return true;
    }

    private static Type? TryGetChunkItemType(Type candidateType)
    {
        var asyncTypes = candidateType.GetInterfaces()
            .Append(candidateType)
            .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
            .ToArray();

        foreach (var asyncType in asyncTypes)
        {
            var itemType = asyncType.GetGenericArguments().SingleOrDefault();
            if (itemType is null)
            {
                continue;
            }

            if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == StreamChunkOpenType)
            {
                return itemType;
            }
        }

        return null;
    }

    private static AspNetResult ToServerSentEventsResult<TProgress, TResult>(
        IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> updates)
    {
        return TypedResults.ServerSentEvents(ToSseItems(updates));
    }

    private static async IAsyncEnumerable<SseItem<CqrsStreamChunk<TProgress, TResult>>>
        ToSseItems<TProgress, TResult>(
            IAsyncEnumerable<CqrsStreamChunk<TProgress, TResult>> updates,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var enumerator = updates.GetAsyncEnumerator(cancellationToken);
        var sequence = 0L;
        CqrsStreamChunk<TProgress, TResult>? failedChunk = null;

        try
        {
            while (true)
            {
                bool movedNext;

                try
                {
                    movedNext = await enumerator.MoveNextAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    failedChunk = CqrsStreamChunk<TProgress, TResult>.Failed(
                        Problem.Create(exception),
                        message: "An unexpected error occurred while streaming command updates.",
                        sequence: sequence + 1);
                    break;
                }

                if (!movedNext)
                {
                    break;
                }

                var update = enumerator.Current;
                if (update is null)
                {
                    continue;
                }

                sequence = update.Sequence.HasValue && update.Sequence.Value > sequence
                    ? update.Sequence.Value
                    : sequence + 1;

                var eventId = update.EventId
                              ?? update.Sequence?.ToString(CultureInfo.InvariantCulture);

                yield return new SseItem<CqrsStreamChunk<TProgress, TResult>>(update, update.EventType)
                {
                    EventId = eventId
                };
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }

        if (failedChunk is not null)
        {
            yield return new SseItem<CqrsStreamChunk<TProgress, TResult>>(failedChunk, failedChunk.EventType)
            {
                EventId = failedChunk.EventId ?? failedChunk.Sequence?.ToString(CultureInfo.InvariantCulture)
            };
        }
    }
}
