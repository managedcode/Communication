using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;
using Shouldly;

namespace ManagedCode.Communication.Tests.CQRS;

internal static class CqrsStreamCancellationJoinScenario
{
    private const string StartedState = "started";
    private const string FinalState = "complete";
    private const int OperationTimeoutSeconds = 10;
    private const int PrematureCompletionObservationMilliseconds = 250;

    internal static async Task AssertFaultingCallbackJoinAsync()
    {
        var probe = new CqrsStreamCancellationJoinProbe();
        var enumerator = CqrsStream.Create<ProgressUpdate, FinalResult>(probe.HandleAsync).GetAsyncEnumerator();
        await RunFaultingCallbackJoinAsync(probe, enumerator);
    }

    internal static async Task AssertOrdinaryCancellationJoinAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var handlerSettled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var enumerator = CqrsStream.Create<ProgressUpdate, FinalResult>(async writer =>
        {
            try
            {
                await writer.StartedAsync(new ProgressUpdate(StartedState));
                await Task.Delay(Timeout.InfiniteTimeSpan, writer.CancellationToken).ConfigureAwait(true);
                return Result<FinalResult>.Succeed(new FinalResult(FinalState));
            }
            finally
            {
                handlerSettled.TrySetResult();
            }
        }).GetAsyncEnumerator(cancellation.Token);
        await RunOrdinaryCancellationJoinAsync(enumerator, handlerSettled.Task, cancellation);
    }

    private static async Task RunFaultingCallbackJoinAsync(
        CqrsStreamCancellationJoinProbe probe, IAsyncEnumerator<Chunk> enumerator)
    {
        Task<bool>? firstMove = null;
        Task? disposal = null;
        Exception? primaryFailure = null;
        var cleanupFailures = new List<Exception>();
        var callbackFailureObserved = false;
        try
        {
            firstMove = enumerator.MoveNextAsync().AsTask();
            await probe.HandlerEntered.WaitAsync(TimeSpan.FromSeconds(OperationTimeoutSeconds));
            (await firstMove.WaitAsync(TimeSpan.FromSeconds(OperationTimeoutSeconds))).ShouldBeTrue();
            enumerator.Current.Kind.ShouldBe(CqrsStreamChunkKind.Started);
            await probe.CallbackRegistered.WaitAsync(TimeSpan.FromSeconds(OperationTimeoutSeconds));
            disposal = DisposeEnumeratorAsync(enumerator);
            await probe.CallbackEntered.WaitAsync(TimeSpan.FromSeconds(OperationTimeoutSeconds));
            await probe.FinallyEntered.WaitAsync(TimeSpan.FromSeconds(OperationTimeoutSeconds));
            await Task.WhenAny(disposal, Task.Delay(TimeSpan.FromMilliseconds(
                PrematureCompletionObservationMilliseconds)));
            disposal.IsCompleted.ShouldBeFalse();
            probe.ReleaseHandler();
            await probe.HandlerSettled.WaitAsync(TimeSpan.FromSeconds(OperationTimeoutSeconds));
            await AssertOnlyCallbackFailureAsync(disposal, probe.CallbackFailure);
            callbackFailureObserved = true;
            await AssertHealthyFollowUpAsync();
        }
        catch (Exception exception)
        {
            primaryFailure = exception;
        }
        finally
        {
            probe.ReleaseHandler();
            disposal ??= DisposeEnumeratorAsync(enumerator);
            if (!callbackFailureObserved)
            {
                await CqrsStreamCancellationJoinCleanup.ObserveTaskAsync(disposal, cleanupFailures);
            }
            if (firstMove is not null)
            {
                await CqrsStreamCancellationJoinCleanup.ObserveTaskAsync(firstMove, cleanupFailures);
            }
            await probe.HandlerSettled;
        }
        CqrsStreamCancellationJoinCleanup.ThrowFailures(primaryFailure, cleanupFailures);
    }

    private static async Task RunOrdinaryCancellationJoinAsync(
        IAsyncEnumerator<Chunk> enumerator, Task handlerSettled, CancellationTokenSource cancellation)
    {
        Task<bool>? firstMove = null;
        Task? disposal = null;
        Exception? primaryFailure = null;
        var cleanupFailures = new List<Exception>();
        try
        {
            firstMove = enumerator.MoveNextAsync().AsTask();
            (await firstMove.WaitAsync(TimeSpan.FromSeconds(OperationTimeoutSeconds))).ShouldBeTrue();
            enumerator.Current.Kind.ShouldBe(CqrsStreamChunkKind.Started);
            disposal = DisposeEnumeratorAsync(enumerator);
            await handlerSettled.WaitAsync(TimeSpan.FromSeconds(OperationTimeoutSeconds));
            await disposal.WaitAsync(TimeSpan.FromSeconds(OperationTimeoutSeconds));
        }
        catch (Exception exception)
        {
            primaryFailure = exception;
        }
        finally
        {
            await CqrsStreamCancellationJoinCleanup.ObserveTaskAsync(
                cancellation.CancelAsync(), cleanupFailures);
            disposal ??= DisposeEnumeratorAsync(enumerator);
            await CqrsStreamCancellationJoinCleanup.ObserveTaskAsync(disposal, cleanupFailures);
            if (firstMove is not null)
            {
                await CqrsStreamCancellationJoinCleanup.ObserveTaskAsync(firstMove, cleanupFailures);
            }
            await handlerSettled;
        }
        CqrsStreamCancellationJoinCleanup.ThrowFailures(primaryFailure, cleanupFailures);
    }

    private static async Task AssertOnlyCallbackFailureAsync(Task disposal, Exception expected)
    {
        var failure = await Should.ThrowAsync<Exception>(
            () => disposal.WaitAsync(TimeSpan.FromSeconds(OperationTimeoutSeconds)));
        var leaves = CqrsStreamCancellationJoinCleanup.Leaves(failure!);
        leaves.Count.ShouldBe(1);
        leaves[0].ShouldBeSameAs(expected);
    }

    private static async Task AssertHealthyFollowUpAsync()
    {
        var chunks = new List<Chunk>();
        await foreach (var chunk in CqrsStream.Create<ProgressUpdate, FinalResult>(async writer =>
        {
            await writer.StartedAsync(new ProgressUpdate(StartedState));
            return Result<FinalResult>.Succeed(new FinalResult(FinalState));
        }))
        {
            chunks.Add(chunk);
        }
        chunks.ConvertAll(chunk => chunk.Kind).ShouldBe(
            [CqrsStreamChunkKind.Started, CqrsStreamChunkKind.Completed]);
    }

    private static async Task DisposeEnumeratorAsync(IAsyncEnumerator<Chunk> enumerator)
    {
        await enumerator.DisposeAsync().ConfigureAwait(false);
    }

}
