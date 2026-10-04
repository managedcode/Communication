using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;
using Shouldly;

namespace ManagedCode.Communication.Tests.CQRS;

internal static class CqrsHttpBoundsReadSettlement
{
    public static async Task RunEarlyDisposeAsync(
        IAsyncEnumerator<Chunk> enumerator,
        CancellationTokenSource lifetime,
        Task<bool> responseStarted,
        Task responseAborted)
    {
        var moveNext = StartMoveNext(enumerator, out var startFailure);
        var primaryFailure = startFailure ?? await ObserveFirstMoveAsync(enumerator, moveNext, responseStarted)
            .ConfigureAwait(false);
        var failures = new List<Exception>();
        if (primaryFailure is not null)
        {
            try
            {
                failures.AddRange(await CancelAndJoinReadAsync(lifetime, moveNext, primaryFailure).ConfigureAwait(false));
            }
            catch (Exception exception)
            {
                CqrsHttpBoundsFailure.AddDistinct(failures, exception, primaryFailure);
            }
        }

        try
        {
            await CqrsHttpBoundsEnumeratorDisposal.DisposeAfterMoveSettledAsync(
                    enumerator,
                    moveNext,
                    lifetime,
                    failures,
                    primaryFailure)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            CqrsHttpBoundsFailure.AddDistinct(failures, exception, primaryFailure);
        }

        try
        {
            await CqrsHttpBoundsEnumeratorDisposal.ObserveAbortAsync(
                    responseStarted,
                    responseAborted,
                    failures,
                    primaryFailure)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            CqrsHttpBoundsFailure.AddDistinct(failures, exception, primaryFailure);
        }

        CqrsHttpBoundsFailure.Throw(primaryFailure, failures);
    }

    private static async Task<Exception?> ObserveFirstMoveAsync(
        IAsyncEnumerator<Chunk> enumerator,
        Task<bool> moveNext,
        Task<bool> responseStarted)
    {
        try
        {
            (await moveNext.WaitAsync(TimeSpan.FromSeconds(CqrsHttpBoundsTestSupport.CleanupTimeoutSeconds))
                .ConfigureAwait(false)).ShouldBeTrue();
            enumerator.Current.Kind.ShouldBe(CqrsStreamChunkKind.Started);
            await responseStarted.WaitAsync(TimeSpan.FromSeconds(CqrsHttpBoundsTestSupport.CleanupTimeoutSeconds))
                .ConfigureAwait(false);
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    public static async Task<List<Exception>> CancelAndJoinReadAsync(
        CancellationTokenSource lifetime,
        Task read,
        Exception? primaryFailure)
    {
        var failures = new List<Exception>();
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
            await read.WaitAsync(TimeSpan.FromSeconds(CqrsHttpBoundsTestSupport.CleanupTimeoutSeconds))
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            CqrsHttpBoundsFailure.AddDistinct(failures, exception, primaryFailure);
        }

        return failures;
    }

    public static async Task RunCancelledMoveAndSettleAsync(
        IAsyncEnumerator<Chunk> enumerator,
        CancellationTokenSource lifetime,
        Task<bool> responseStarted,
        Task responseAborted)
    {
        var moveNext = StartMoveNext(enumerator, out var primaryFailure);
        var failures = new List<Exception>();
        try
        {
            if (primaryFailure is null)
            {
                await responseStarted.WaitAsync(TimeSpan.FromSeconds(CqrsHttpBoundsTestSupport.CleanupTimeoutSeconds))
                    .ConfigureAwait(false);
                await lifetime.CancelAsync().ConfigureAwait(false);
                var wasCancelled = false;
                try
                {
                    await moveNext.WaitAsync(TimeSpan.FromSeconds(CqrsHttpBoundsTestSupport.CleanupTimeoutSeconds))
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
                {
                    wasCancelled = true;
                }

                wasCancelled.ShouldBeTrue(moveNext.IsCompletedSuccessfully && moveNext.Result
                    ? enumerator.Current.Problem?.ErrorCode
                    : null);
            }
        }
        catch (Exception exception)
        {
            primaryFailure = exception;
        }
        finally
        {
            try
            {
                failures.AddRange(await CancelAndJoinReadAsync(lifetime, moveNext, primaryFailure).ConfigureAwait(false));
            }
            catch (Exception exception)
            {
                CqrsHttpBoundsFailure.AddDistinct(failures, exception, primaryFailure);
            }

            try
            {
                await CqrsHttpBoundsEnumeratorDisposal.DisposeAfterMoveSettledAsync(
                        enumerator,
                        moveNext,
                        lifetime,
                        failures,
                        primaryFailure)
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                CqrsHttpBoundsFailure.AddDistinct(failures, exception, primaryFailure);
            }

            try
            {
                await CqrsHttpBoundsEnumeratorDisposal.ObserveAbortAsync(
                        responseStarted,
                        responseAborted,
                        failures,
                        primaryFailure)
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                CqrsHttpBoundsFailure.AddDistinct(failures, exception, primaryFailure);
            }
        }

        CqrsHttpBoundsFailure.Throw(primaryFailure, failures);
    }

    private static Task<bool> StartMoveNext(IAsyncEnumerator<Chunk> enumerator, out Exception? failure)
    {
        try
        {
            failure = null;
            return enumerator.MoveNextAsync().AsTask();
        }
        catch (Exception exception)
        {
            failure = exception;
            return Task.FromException<bool>(exception);
        }
    }
}
