using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;

namespace ManagedCode.Communication.Tests.CQRS;

internal static class CqrsHttpBoundsEnumeratorDisposal
{
    private const string ActiveMoveNextTimeoutMessage = "The active MoveNextAsync operation did not settle before disposal.";

    public static Task DisposeAfterMoveSettledAsync(
        IAsyncEnumerator<Chunk> enumerator,
        Task moveNext,
        CancellationTokenSource lifetime,
        List<Exception> failures,
        Exception? primaryFailure)
    {
        if (!moveNext.IsCompleted)
        {
            failures.Add(new TimeoutException(ActiveMoveNextTimeoutMessage));
            return Task.CompletedTask;
        }

        return DisposeEnumeratorAsync(enumerator, lifetime, failures, primaryFailure);
    }

    public static async Task DisposeEnumeratorAsync(
        IAsyncEnumerator<Chunk> enumerator,
        CancellationTokenSource lifetime,
        List<Exception> failures,
        Exception? primaryFailure)
    {
        Task dispose;
        try
        {
            dispose = enumerator.DisposeAsync().AsTask();
        }
        catch (Exception exception)
        {
            CqrsHttpBoundsFailure.AddDistinct(failures, exception, primaryFailure);
            return;
        }

        try
        {
            await dispose.WaitAsync(TimeSpan.FromSeconds(CqrsHttpBoundsTestSupport.CleanupTimeoutSeconds))
                .ConfigureAwait(false);
        }
        catch (TimeoutException exception)
        {
            CqrsHttpBoundsFailure.AddDistinct(failures, exception, primaryFailure);
            await CancelAndJoinDisposeAsync(lifetime, dispose, failures, primaryFailure).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            CqrsHttpBoundsFailure.AddDistinct(failures, exception, primaryFailure);
        }
    }

    public static async Task ObserveAbortAsync(
        Task<bool> responseStarted,
        Task responseAborted,
        List<Exception> failures,
        Exception? primaryFailure)
    {
        if (!responseStarted.IsCompletedSuccessfully || !responseStarted.Result)
        {
            return;
        }

        try
        {
            await responseAborted.WaitAsync(TimeSpan.FromSeconds(CqrsHttpBoundsTestSupport.CleanupTimeoutSeconds))
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            CqrsHttpBoundsFailure.AddDistinct(failures, exception, primaryFailure);
        }
    }

    private static async Task CancelAndJoinDisposeAsync(
        CancellationTokenSource lifetime,
        Task dispose,
        List<Exception> failures,
        Exception? primaryFailure)
    {
        try
        {
            await lifetime.CancelAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            CqrsHttpBoundsFailure.AddDistinct(failures, exception, primaryFailure);
        }

        try
        {
            await dispose.WaitAsync(TimeSpan.FromSeconds(CqrsHttpBoundsTestSupport.CleanupTimeoutSeconds))
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            CqrsHttpBoundsFailure.AddDistinct(failures, exception, primaryFailure);
        }
    }
}
