using System;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Communication.Results.Extensions;

/// <summary>
///     Converts delegates and tasks into <c>Result</c>, catching exceptions.
/// </summary>
public static partial class ResultExecutionExtensions
{
    /// <summary>
    ///     Awaits the operation and wraps its outcome, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<Result> ToResultAsync(this Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
            return Result.Succeed();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception);
        }
    }

    /// <summary>
    ///     Invokes and awaits the factory, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<Result> ToResultAsync(this Func<Task> taskFactory, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await taskFactory().ConfigureAwait(false);
            return Result.Succeed();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception);
        }
    }

    /// <summary>
    ///     Awaits the operation and wraps its outcome, turning a thrown exception into a failure.
    /// </summary>
    public static async ValueTask<Result> ToResultAsync(this ValueTask valueTask)
    {
        try
        {
            await valueTask.ConfigureAwait(false);
            return Result.Succeed();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception);
        }
    }

    /// <summary>
    ///     Invokes and awaits the factory, turning a thrown exception into a failure.
    /// </summary>
    public static async ValueTask<Result> ToResultAsync(this Func<ValueTask> taskFactory)
    {
        try
        {
            await taskFactory().ConfigureAwait(false);
            return Result.Succeed();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception);
        }
    }
}
