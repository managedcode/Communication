using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Logging;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication.CollectionResults.Extensions;

public static partial class CollectionResultExecutionExtensions
{
    public static async Task<CollectionResult<T>> ToCollectionResultAsync<T>(this Task<T[]> task)
    {
        return await ExecuteAsync(task, CollectionResult<T>.Succeed).ConfigureAwait(false);
    }

    public static async Task<CollectionResult<T>> ToCollectionResultAsync<T>(this Task<IEnumerable<T>> task)
    {
        return await ExecuteAsync(task, CollectionResult<T>.Succeed).ConfigureAwait(false);
    }

    public static async Task<CollectionResult<T>> ToCollectionResultAsync<T>(this Task<CollectionResult<T>> task)
    {
        try
        {
            return await task.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return CollectionResult<T>.Fail(exception);
        }
    }

    public static async Task<CollectionResult<T>> ToCollectionResultAsync<T>(this Func<Task<T[]>> taskFactory, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(Task.Run(taskFactory, cancellationToken), CollectionResult<T>.Succeed).ConfigureAwait(false);
    }

    public static async Task<CollectionResult<T>> ToCollectionResultAsync<T>(this Func<Task<IEnumerable<T>>> taskFactory, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(Task.Run(taskFactory, cancellationToken), CollectionResult<T>.Succeed).ConfigureAwait(false);
    }

    public static async Task<CollectionResult<T>> ToCollectionResultAsync<T>(this Func<Task<CollectionResult<T>>> taskFactory, CancellationToken cancellationToken = default)
    {
        try
        {
            return await Task.Run(taskFactory, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return CollectionResult<T>.Fail(exception);
        }
    }

    public static async ValueTask<CollectionResult<T>> ToCollectionResultAsync<T>(this ValueTask<T[]> valueTask)
    {
        return await ExecuteAsync(valueTask.AsTask(), CollectionResult<T>.Succeed).ConfigureAwait(false);
    }

    public static async ValueTask<CollectionResult<T>> ToCollectionResultAsync<T>(this ValueTask<IEnumerable<T>> valueTask)
    {
        return await ExecuteAsync(valueTask.AsTask(), CollectionResult<T>.Succeed).ConfigureAwait(false);
    }

    public static async ValueTask<CollectionResult<T>> ToCollectionResultAsync<T>(this ValueTask<CollectionResult<T>> valueTask)
    {
        try
        {
            return await valueTask.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return CollectionResult<T>.Fail(exception);
        }
    }

    public static async Task<CollectionResult<T>> ToCollectionResultAsync<T>(this Func<ValueTask<T[]>> valueTaskFactory)
    {
        try
        {
            return await ExecuteAsync(valueTaskFactory().AsTask(), CollectionResult<T>.Succeed).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return CollectionResult<T>.Fail(exception);
        }
    }

    public static async Task<CollectionResult<T>> ToCollectionResultAsync<T>(this Func<ValueTask<IEnumerable<T>>> valueTaskFactory, [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string caller = null!, [CallerFilePath] string path = null!)
    {
        try
        {
            var values = await valueTaskFactory().ConfigureAwait(false);
            return CollectionResult<T>.Succeed(values);
        }
        catch (Exception exception)
        {
            ILogger? logger = CommunicationLogger.GetLogger();
            LoggerCenter.LogCollectionResultError(logger, exception, exception.Message, Path.GetFileName(path), lineNumber, caller);
            return CollectionResult<T>.Fail(exception);
        }
    }

    public static async Task<CollectionResult<T>> ToCollectionResultAsync<T>(this Func<ValueTask<CollectionResult<T>>> valueTaskFactory)
    {
        try
        {
            return await valueTaskFactory().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return CollectionResult<T>.Fail(exception);
        }
    }

    private static async Task<CollectionResult<T>> ExecuteAsync<T, TValue>(Task<TValue> task, Func<TValue, CollectionResult<T>> projector)
    {
        try
        {
            var value = await task.ConfigureAwait(false);
            return projector(value);
        }
        catch (Exception exception)
        {
            return CollectionResult<T>.Fail(exception);
        }
    }
}
