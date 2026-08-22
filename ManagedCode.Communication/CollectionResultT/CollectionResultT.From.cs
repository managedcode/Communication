using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.CollectionResults.Extensions;
using Microsoft.Extensions.Logging;
using ManagedCode.Communication.Logging;

namespace ManagedCode.Communication.CollectionResultT;

public partial struct CollectionResult<T>
{
    /// <summary>
    ///     Runs the function and wraps its items, turning a thrown exception into a failure.
    /// </summary>
    public static CollectionResult<T> From(Func<T[]> func)
    {
        return func.ToCollectionResult();
    }

    /// <summary>
    ///     Runs the function and wraps its items, turning a thrown exception into a failure.
    /// </summary>
    public static CollectionResult<T> From(Func<IEnumerable<T>> func)
    {
        return func.ToCollectionResult();
    }

    /// <summary>
    ///     Runs the function and returns its result, turning a thrown exception into a failure.
    /// </summary>
    public static CollectionResult<T> From(Func<CollectionResult<T>> func)
    {
        return func.ToCollectionResult();
    }

    /// <summary>
    ///     Awaits the task and wraps its items, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<CollectionResult<T>> From(Task<T[]> task)
    {
        return await task.ToCollectionResultAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Awaits the task and wraps its items, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<CollectionResult<T>> From(Task<IEnumerable<T>> task)
    {
        return await task.ToCollectionResultAsync().ConfigureAwait(false);
    }


    /// <summary>
    ///     Awaits the task and returns its result, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<CollectionResult<T>> From(Task<CollectionResult<T>> task)
    {
        return await task.ToCollectionResultAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Invokes and awaits the factory, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<CollectionResult<T>> From(Func<Task<T[]>> task, CancellationToken cancellationToken = default)
    {
        return await task.ToCollectionResultAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Invokes and awaits the factory, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<CollectionResult<T>> From(Func<Task<IEnumerable<T>>> task, CancellationToken cancellationToken = default)
    {
        return await task.ToCollectionResultAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Invokes and awaits the factory, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<CollectionResult<T>> From(Func<Task<CollectionResult<T>>> task, CancellationToken cancellationToken = default)
    {
        return await task.ToCollectionResultAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Returns the result unchanged; present so conversions compose uniformly.
    /// </summary>
    public static CollectionResult<T> From(CollectionResult<T> result)
    {
        return result.IsSuccess ? result : result.Problem != null ? Fail(result.Problem) : Fail();
    }

    /// <summary>
    ///     Discards the items and keeps only success or failure.
    /// </summary>
    public static Result From<U>(CollectionResult<U> result)
    {
        return result.ToResult();
    }


    /// <summary>
    ///     Awaits the value task and wraps its items, turning a thrown exception into a failure.
    /// </summary>
    public static async ValueTask<CollectionResult<T>> From(ValueTask<T[]> valueTask)
    {
        return await valueTask.ToCollectionResultAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Awaits the value task and wraps its items, turning a thrown exception into a failure.
    /// </summary>
    public static async ValueTask<CollectionResult<T>> From(ValueTask<IEnumerable<T>> valueTask)
    {
        return await valueTask.ToCollectionResultAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Awaits the value task and returns its result, turning a thrown exception into a failure.
    /// </summary>
    public static async ValueTask<CollectionResult<T>> From(ValueTask<CollectionResult<T>> valueTask)
    {
        return await valueTask.ToCollectionResultAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Invokes and awaits the factory, turning a thrown exception into a failure.
    /// </summary>
    public static async ValueTask<CollectionResult<T>> From(Func<ValueTask<T[]>> valueTask)
    {
        return await valueTask.ToCollectionResultAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Invokes and awaits the factory, turning a thrown exception into a failure.
    /// </summary>
    public static async ValueTask<CollectionResult<T>> From(Func<ValueTask<IEnumerable<T>>> valueTask, [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string caller = null!, [CallerFilePath] string path = null!)
    {
        return await valueTask.ToCollectionResultAsync(lineNumber, caller, path).ConfigureAwait(false);
    }

    /// <summary>
    ///     Invokes and awaits the factory, turning a thrown exception into a failure.
    /// </summary>
    public static async ValueTask<CollectionResult<T>> From(Func<ValueTask<CollectionResult<T>>> valueTask)
    {
        return await valueTask.ToCollectionResultAsync().ConfigureAwait(false);
    }
}
