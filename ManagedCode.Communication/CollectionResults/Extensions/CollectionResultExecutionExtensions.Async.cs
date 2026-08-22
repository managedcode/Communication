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

/// <summary>
///     Converts tasks and factories that produce sequences into <c>CollectionResult</c>, catching exceptions.
/// </summary>
public static partial class CollectionResultExecutionExtensions
{
    /// <summary>
    ///     Awaits the task and wraps its items, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<CollectionResult<T>> ToCollectionResultAsync<T>(this Task<T[]> task)
    {
        return await ExecuteAsync(task, CollectionResult<T>.Succeed).ConfigureAwait(false);
    }

    /// <summary>
    ///     Awaits the task and wraps its items, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<CollectionResult<T>> ToCollectionResultAsync<T>(this Task<IEnumerable<T>> task)
    {
        return await ExecuteAsync(task, CollectionResult<T>.Succeed).ConfigureAwait(false);
    }

    /// <summary>
    ///     Awaits the task and returns its result, turning a thrown exception into a failure.
    /// </summary>
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

    /// <summary>
    ///     Invokes and awaits the factory, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<CollectionResult<T>> ToCollectionResultAsync<T>(this Func<Task<T[]>> taskFactory, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(Task.Run(taskFactory, cancellationToken), CollectionResult<T>.Succeed).ConfigureAwait(false);
    }

    /// <summary>
    ///     Invokes and awaits the factory, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<CollectionResult<T>> ToCollectionResultAsync<T>(this Func<Task<IEnumerable<T>>> taskFactory, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(Task.Run(taskFactory, cancellationToken), CollectionResult<T>.Succeed).ConfigureAwait(false);
    }

    /// <summary>
    ///     Invokes and awaits the factory, turning a thrown exception into a failure.
    /// </summary>
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

    /// <summary>
    ///     Awaits the value task and wraps its items, turning a thrown exception into a failure.
    /// </summary>
    public static async ValueTask<CollectionResult<T>> ToCollectionResultAsync<T>(this ValueTask<T[]> valueTask)
    {
        return await ExecuteAsync(valueTask, CollectionResult<T>.Succeed).ConfigureAwait(false);
    }

    /// <summary>
    ///     Awaits the value task and wraps its items, turning a thrown exception into a failure.
    /// </summary>
    public static async ValueTask<CollectionResult<T>> ToCollectionResultAsync<T>(this ValueTask<IEnumerable<T>> valueTask)
    {
        return await ExecuteAsync(valueTask, CollectionResult<T>.Succeed).ConfigureAwait(false);
    }

    /// <summary>
    ///     Awaits the value task and returns its result, turning a thrown exception into a failure.
    /// </summary>
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

    /// <summary>
    ///     Invokes and awaits the factory, turning a thrown exception into a failure.
    /// </summary>
    public static async ValueTask<CollectionResult<T>> ToCollectionResultAsync<T>(this Func<ValueTask<T[]>> valueTaskFactory)
    {
        try
        {
            return await ExecuteAsync(valueTaskFactory(), CollectionResult<T>.Succeed).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return CollectionResult<T>.Fail(exception);
        }
    }

    /// <summary>
    ///     Invokes and awaits the factory, turning a thrown exception into a failure.
    /// </summary>
    public static async ValueTask<CollectionResult<T>> ToCollectionResultAsync<T>(this Func<ValueTask<IEnumerable<T>>> valueTaskFactory, [CallerLineNumber] int lineNumber = 0,
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

    /// <summary>
    ///     Invokes and awaits the factory, turning a thrown exception into a failure.
    /// </summary>
    public static async ValueTask<CollectionResult<T>> ToCollectionResultAsync<T>(this Func<ValueTask<CollectionResult<T>>> valueTaskFactory)
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

    private static async ValueTask<CollectionResult<T>> ExecuteAsync<T, TValue>(
        ValueTask<TValue> valueTask,
        Func<TValue, CollectionResult<T>> projector)
    {
        try
        {
            var value = await valueTask.ConfigureAwait(false);
            return projector(value);
        }
        catch (Exception exception)
        {
            return CollectionResult<T>.Fail(exception);
        }
    }
}
