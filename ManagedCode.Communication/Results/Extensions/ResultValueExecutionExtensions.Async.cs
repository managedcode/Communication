using System;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Communication.Results.Extensions;

public static partial class ResultValueExecutionExtensions
{
    public static async Task<Result<T>> ToResultAsync<T>(this Task<T> task)
    {
        try
        {
            return Result<T>.Succeed(await task.ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return Result<T>.Fail(exception);
        }
    }

    public static async Task<Result<T>> ToResultAsync<T>(this Task<Result<T>> task)
    {
        try
        {
            return await task.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return Result<T>.Fail(exception);
        }
    }

    public static async Task<Result<T>> ToResultAsync<T>(this Func<Task<T>> taskFactory, CancellationToken cancellationToken = default)
    {
        try
        {
            return Result<T>.Succeed(await Task.Run(taskFactory, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return Result<T>.Fail(exception);
        }
    }

    public static async Task<Result<T>> ToResultAsync<T>(this Func<Task<Result<T>>> taskFactory, CancellationToken cancellationToken = default)
    {
        try
        {
            return await Task.Run(taskFactory, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return Result<T>.Fail(exception);
        }
    }

    public static async ValueTask<Result<T>> ToResultAsync<T>(this ValueTask<T> valueTask)
    {
        try
        {
            return Result<T>.Succeed(await valueTask.ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return Result<T>.Fail(exception);
        }
    }

    public static async ValueTask<Result<T>> ToResultAsync<T>(this ValueTask<Result<T>> valueTask)
    {
        try
        {
            return await valueTask.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return Result<T>.Fail(exception);
        }
    }

    public static async Task<Result<T>> ToResultAsync<T>(this Func<ValueTask<T>> valueTaskFactory)
    {
        try
        {
            return Result<T>.Succeed(await valueTaskFactory().ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return Result<T>.Fail(exception);
        }
    }

    public static async Task<Result<T>> ToResultAsync<T>(this Func<ValueTask<Result<T>>> valueTaskFactory)
    {
        try
        {
            return await valueTaskFactory().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return Result<T>.Fail(exception);
        }
    }
}
