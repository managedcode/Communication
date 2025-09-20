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
    public static CollectionResult<T> From(Func<T[]> func)
    {
        return func.ToCollectionResult();
    }

    public static CollectionResult<T> From(Func<IEnumerable<T>> func)
    {
        return func.ToCollectionResult();
    }

    public static CollectionResult<T> From(Func<CollectionResult<T>> func)
    {
        return func.ToCollectionResult();
    }

    public static async Task<CollectionResult<T>> From(Task<T[]> task)
    {
        return await task.ToCollectionResultAsync().ConfigureAwait(false);
    }

    public static async Task<CollectionResult<T>> From(Task<IEnumerable<T>> task)
    {
        return await task.ToCollectionResultAsync().ConfigureAwait(false);
    }


    public static async Task<CollectionResult<T>> From(Task<CollectionResult<T>> task)
    {
        return await task.ToCollectionResultAsync().ConfigureAwait(false);
    }

    public static async Task<CollectionResult<T>> From(Func<Task<T[]>> task, CancellationToken cancellationToken = default)
    {
        return await task.ToCollectionResultAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async Task<CollectionResult<T>> From(Func<Task<IEnumerable<T>>> task, CancellationToken cancellationToken = default)
    {
        return await task.ToCollectionResultAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async Task<CollectionResult<T>> From(Func<Task<CollectionResult<T>>> task, CancellationToken cancellationToken = default)
    {
        return await task.ToCollectionResultAsync(cancellationToken).ConfigureAwait(false);
    }

    public static CollectionResult<T> From(CollectionResult<T> result)
    {
        return result.IsSuccess ? result : result.Problem != null ? Fail(result.Problem) : Fail();
    }

    public static Result From<U>(CollectionResult<U> result)
    {
        return result.ToResult();
    }


    public static async ValueTask<CollectionResult<T>> From(ValueTask<T[]> valueTask)
    {
        return await valueTask.ToCollectionResultAsync().ConfigureAwait(false);
    }

    public static async ValueTask<CollectionResult<T>> From(ValueTask<IEnumerable<T>> valueTask)
    {
        return await valueTask.ToCollectionResultAsync().ConfigureAwait(false);
    }

    public static async ValueTask<CollectionResult<T>> From(ValueTask<CollectionResult<T>> valueTask)
    {
        return await valueTask.ToCollectionResultAsync().ConfigureAwait(false);
    }

    public static async Task<CollectionResult<T>> From(Func<ValueTask<T[]>> valueTask)
    {
        return await valueTask.ToCollectionResultAsync().ConfigureAwait(false);
    }

    public static async Task<CollectionResult<T>> From(Func<ValueTask<IEnumerable<T>>> valueTask, [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string caller = null!, [CallerFilePath] string path = null!)
    {
        return await valueTask.ToCollectionResultAsync(lineNumber, caller, path).ConfigureAwait(false);
    }

    public static async Task<CollectionResult<T>> From(Func<ValueTask<CollectionResult<T>>> valueTask)
    {
        return await valueTask.ToCollectionResultAsync().ConfigureAwait(false);
}
}
