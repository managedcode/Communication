using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.CollectionResults.Factories;
using ManagedCode.Communication.Constants;
using ManagedCode.Communication.Results.Factories;
using ManagedCode.Communication.Logging;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication.CollectionResults.Extensions;

/// <summary>
///     Execution helpers for creating <see cref="CollectionResult{T}"/> instances.
/// </summary>
public static class CollectionResultExecutionExtensions
{
    public static CollectionResult<T> ToCollectionResult<T>(this Func<T[]> func)
    {
        return Execute(func, CollectionResultFactory.Success);
    }

    public static CollectionResult<T> ToCollectionResult<T>(this Func<IEnumerable<T>> func)
    {
        return Execute(func, CollectionResultFactory.Success);
    }

    public static CollectionResult<T> ToCollectionResult<T>(this Func<CollectionResult<T>> func)
    {
        try
        {
            return func();
        }
        catch (Exception exception)
        {
            return CollectionResultFactory.Failure<T>(exception);
        }
    }

    public static async Task<CollectionResult<T>> ToCollectionResultAsync<T>(this Task<T[]> task)
    {
        return await ExecuteAsync(task, CollectionResultFactory.Success).ConfigureAwait(false);
    }

    public static async Task<CollectionResult<T>> ToCollectionResultAsync<T>(this Task<IEnumerable<T>> task)
    {
        return await ExecuteAsync(task, CollectionResultFactory.Success).ConfigureAwait(false);
    }

    public static async Task<CollectionResult<T>> ToCollectionResultAsync<T>(this Task<CollectionResult<T>> task)
    {
        try
        {
            return await task.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return CollectionResultFactory.Failure<T>(exception);
        }
    }

    public static async Task<CollectionResult<T>> ToCollectionResultAsync<T>(this Func<Task<T[]>> taskFactory, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(Task.Run(taskFactory, cancellationToken), CollectionResultFactory.Success).ConfigureAwait(false);
    }

    public static async Task<CollectionResult<T>> ToCollectionResultAsync<T>(this Func<Task<IEnumerable<T>>> taskFactory, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(Task.Run(taskFactory, cancellationToken), CollectionResultFactory.Success).ConfigureAwait(false);
    }

    public static async Task<CollectionResult<T>> ToCollectionResultAsync<T>(this Func<Task<CollectionResult<T>>> taskFactory, CancellationToken cancellationToken = default)
    {
        try
        {
            return await Task.Run(taskFactory, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return CollectionResultFactory.Failure<T>(exception);
        }
    }

    public static async ValueTask<CollectionResult<T>> ToCollectionResultAsync<T>(this ValueTask<T[]> valueTask)
    {
        return await ExecuteAsync(valueTask.AsTask(), CollectionResultFactory.Success).ConfigureAwait(false);
    }

    public static async ValueTask<CollectionResult<T>> ToCollectionResultAsync<T>(this ValueTask<IEnumerable<T>> valueTask)
    {
        return await ExecuteAsync(valueTask.AsTask(), CollectionResultFactory.Success).ConfigureAwait(false);
    }

    public static async ValueTask<CollectionResult<T>> ToCollectionResultAsync<T>(this ValueTask<CollectionResult<T>> valueTask)
    {
        try
        {
            return await valueTask.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return CollectionResultFactory.Failure<T>(exception);
        }
    }

    public static async Task<CollectionResult<T>> ToCollectionResultAsync<T>(this Func<ValueTask<T[]>> valueTaskFactory)
    {
        try
        {
            return await ExecuteAsync(valueTaskFactory().AsTask(), CollectionResultFactory.Success).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return CollectionResultFactory.Failure<T>(exception);
        }
    }

    public static async Task<CollectionResult<T>> ToCollectionResultAsync<T>(this Func<ValueTask<IEnumerable<T>>> valueTaskFactory, [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string caller = null!, [CallerFilePath] string path = null!)
    {
        try
        {
            var values = await valueTaskFactory().ConfigureAwait(false);
            return CollectionResultFactory.Success(values);
        }
        catch (Exception exception)
        {
            ILogger? logger = CommunicationLogger.GetLogger();
            LoggerCenter.LogCollectionResultError(logger, exception, exception.Message, Path.GetFileName(path), lineNumber, caller);
            return CollectionResultFactory.Failure<T>(exception);
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
            return CollectionResultFactory.Failure<T>(exception);
        }
    }

    public static Result ToResult<T>(this CollectionResult<T> result)
    {
        return result.IsSuccess ? ResultFactory.Success() : ResultFactory.Failure(result.Problem ?? Problem.GenericError());
    }

    private static CollectionResult<T> Execute<T, TValue>(Func<TValue> func, Func<TValue, CollectionResult<T>> projector)
    {
        try
        {
            var value = func();
            return projector(value);
        }
        catch (Exception exception)
        {
            return CollectionResultFactory.Failure<T>(exception);
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
            return CollectionResultFactory.Failure<T>(exception);
        }
    }
}
