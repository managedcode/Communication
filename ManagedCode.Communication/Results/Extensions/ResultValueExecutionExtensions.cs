using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Communication.Results.Factories;

namespace ManagedCode.Communication.Results.Extensions;

/// <summary>
///     Execution helpers that convert delegates into <see cref="Result{T}"/> values.
/// </summary>
public static class ResultValueExecutionExtensions
{
    public static Result<T> ToResult<T>(this Func<T> func)
    {
        try
        {
            return ResultFactory.Success(func());
        }
        catch (Exception exception)
        {
            return ResultFactory.Failure<T>(exception);
        }
    }

    public static Result<T> ToResult<T>(this Func<Result<T>> func)
    {
        try
        {
            return func();
        }
        catch (Exception exception)
        {
            return ResultFactory.Failure<T>(exception);
        }
    }

    public static async Task<Result<T>> ToResultAsync<T>(this Task<T> task)
    {
        try
        {
            return ResultFactory.Success(await task.ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return ResultFactory.Failure<T>(exception);
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
            return ResultFactory.Failure<T>(exception);
        }
    }

    public static async Task<Result<T>> ToResultAsync<T>(this Func<Task<T>> taskFactory, CancellationToken cancellationToken = default)
    {
        try
        {
            return ResultFactory.Success(await Task.Run(taskFactory, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return ResultFactory.Failure<T>(exception);
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
            return ResultFactory.Failure<T>(exception);
        }
    }

    public static async ValueTask<Result<T>> ToResultAsync<T>(this ValueTask<T> valueTask)
    {
        try
        {
            return ResultFactory.Success(await valueTask.ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return ResultFactory.Failure<T>(exception);
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
            return ResultFactory.Failure<T>(exception);
        }
    }

    public static async Task<Result<T>> ToResultAsync<T>(this Func<ValueTask<T>> valueTaskFactory)
    {
        try
        {
            return ResultFactory.Success(await valueTaskFactory().ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return ResultFactory.Failure<T>(exception);
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
            return ResultFactory.Failure<T>(exception);
        }
    }

    public static Result ToResult<T>(this IResult<T> result)
    {
        return result.IsSuccess ? ResultFactory.Success() : ResultFactory.Failure(result.Problem ?? Problem.GenericError());
    }
}
