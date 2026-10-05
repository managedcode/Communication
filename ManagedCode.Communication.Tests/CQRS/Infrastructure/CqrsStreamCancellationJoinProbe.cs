using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.CQRS;

namespace ManagedCode.Communication.Tests.CQRS;

internal sealed class CqrsStreamCancellationJoinProbe
{
    private const string StartedState = "started";
    private const string FinalState = "complete";
    private const string CallbackFailureMessage = "The registered cancellation callback failed.";
    private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource handlerEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource callbackRegistered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource callbackEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource finallyEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource handlerSettled = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal Task HandlerEntered => handlerEntered.Task;
    internal Task CallbackRegistered => callbackRegistered.Task;
    internal Task CallbackEntered => callbackEntered.Task;
    internal Task FinallyEntered => finallyEntered.Task;
    internal Task HandlerSettled => handlerSettled.Task;
    internal InvalidOperationException CallbackFailure { get; } = new(CallbackFailureMessage);

    internal async ValueTask<Result<FinalResult>> HandleAsync(ICqrsStreamWriter<ProgressUpdate, FinalResult> writer)
    {
        handlerEntered.TrySetResult();
        try
        {
            await writer.StartedAsync(new ProgressUpdate(StartedState));
            using var registration = writer.CancellationToken.Register(
                static state => ((CqrsStreamCancellationJoinProbe)state!).ThrowFromCallback(), this);
            callbackRegistered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, writer.CancellationToken).ConfigureAwait(true);
            return Result<FinalResult>.Succeed(new FinalResult(FinalState));
        }
        finally
        {
            finallyEntered.TrySetResult();
            await release.Task.ConfigureAwait(true);
            handlerSettled.TrySetResult();
        }
    }

    internal void ReleaseHandler() => release.TrySetResult();

    private void ThrowFromCallback()
    {
        callbackEntered.TrySetResult();
        throw CallbackFailure;
    }
}
