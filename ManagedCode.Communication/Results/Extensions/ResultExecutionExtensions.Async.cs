using System;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Communication.Results.Extensions;

public static partial class ResultExecutionExtensions
{
    public static async Task<Result> ToResultAsync(this Task task)
    {
        try
        {
            if (task.IsCompletedSuccessfully)
            {
                return Result.Succeed();
            }

            if (task.IsCanceled)
            {
                return Result.Fail(new TaskCanceledException());
            }

            if (task.IsFaulted && task.Exception is not null)
            {
                return Result.Fail(task.Exception);
            }

            await task.ConfigureAwait(false);
            return Result.Succeed();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception);
        }
    }

    public static async Task<Result> ToResultAsync(this Func<Task> taskFactory, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Run(taskFactory, cancellationToken).ConfigureAwait(false);
            return Result.Succeed();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception);
        }
    }

    public static async ValueTask<Result> ToResultAsync(this ValueTask valueTask)
    {
        try
        {
            if (valueTask.IsCompletedSuccessfully)
            {
                return Result.Succeed();
            }

            if (valueTask.IsCanceled || valueTask.IsFaulted)
            {
                return Result.Fail();
            }

            await valueTask.ConfigureAwait(false);
            return Result.Succeed();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception);
        }
    }

    public static async Task<Result> ToResultAsync(this Func<ValueTask> taskFactory)
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
