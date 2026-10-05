using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Communication.CQRS;

internal static class CqrsStreamProducerSettlement
{
    internal static async ValueTask SettleAsync(CancellationTokenSource cancellation, Task producer,
        Exception? iterationFailure)
    {
        Exception? cancellationFailure = null;
        Exception? producerFailure = null;

        try
        {
            await cancellation.CancelAsync().ConfigureAwait(true);
        }
        catch (Exception error)
        {
            cancellationFailure = error;
        }

        try
        {
            await producer.ConfigureAwait(true);
        }
        catch (Exception error)
        {
            producerFailure = error;
        }

        ThrowCombined(iterationFailure, cancellationFailure, producerFailure);
    }

    private static void ThrowCombined(Exception? iteration, Exception? cancellation, Exception? producer)
    {
        var fatal = PreserveFatalOwner(iteration)
            ?? PreserveFatalOwner(cancellation)
            ?? PreserveFatalOwner(producer);
        if (fatal is not null)
        {
            ExceptionDispatchInfo.Capture(fatal).Throw();
        }

        var combined = CombineOrdinary(iteration, cancellation, producer);
        if (combined is not null)
        {
            ExceptionDispatchInfo.Capture(combined).Throw();
        }
    }

    private static Exception? PreserveFatalOwner(Exception? error)
        => CqrsRuntimeFailures.FindFatal(error) is null ? null : error;

    private static Exception? CombineOrdinary(Exception? iteration, Exception? cancellation, Exception? producer)
    {
        if (iteration is null)
        {
            return CombinePair(cancellation, producer);
        }

        if (cancellation is null || ReferenceEquals(iteration, cancellation))
        {
            return CombinePair(iteration, producer);
        }

        if (producer is null || ReferenceEquals(iteration, producer) || ReferenceEquals(cancellation, producer))
        {
            return new AggregateException(iteration, cancellation);
        }

        return new AggregateException(iteration, cancellation, producer);
    }

    private static Exception? CombinePair(Exception? first, Exception? second)
    {
        if (first is null || ReferenceEquals(first, second))
        {
            return second;
        }

        return second is null ? first : new AggregateException(first, second);
    }
}
