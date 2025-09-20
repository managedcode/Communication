using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Communication.Results.Factories;

namespace ManagedCode.Communication.Results.Extensions;

/// <summary>
///     Execution helpers that convert delegates into <see cref="Result"/> instances.
/// </summary>
public static class ResultExecutionExtensions
{
    public static Result ToResult(this Action action)
    {
        try
        {
            action();
            return ResultFactory.Success();
        }
        catch (Exception exception)
        {
            return ResultFactory.Failure(exception);
        }
    }

    public static Result ToResult(this Func<Result> func)
    {
        try
        {
            return func();
        }
        catch (Exception exception)
        {
            return ResultFactory.Failure(exception);
        }
    }

    public static async Task<Result> ToResultAsync(this Task task)
    {
        try
        {
            if (task.IsCompletedSuccessfully)
            {
                return ResultFactory.Success();
            }

            if (task.IsCanceled)
            {
                return ResultFactory.Failure(new TaskCanceledException());
            }

            if (task.IsFaulted && task.Exception is not null)
            {
                return ResultFactory.Failure(task.Exception);
            }

            await task.ConfigureAwait(false);
            return ResultFactory.Success();
        }
        catch (Exception exception)
        {
            return ResultFactory.Failure(exception);
        }
    }

    public static async Task<Result> ToResultAsync(this Func<Task> taskFactory, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Run(taskFactory, cancellationToken).ConfigureAwait(false);
            return ResultFactory.Success();
        }
        catch (Exception exception)
        {
            return ResultFactory.Failure(exception);
        }
    }

    public static async ValueTask<Result> ToResultAsync(this ValueTask valueTask)
    {
        try
        {
            if (valueTask.IsCompletedSuccessfully)
            {
                return ResultFactory.Success();
            }

            if (valueTask.IsCanceled || valueTask.IsFaulted)
            {
                return ResultFactory.Failure();
            }

            await valueTask.ConfigureAwait(false);
            return ResultFactory.Success();
        }
        catch (Exception exception)
        {
            return ResultFactory.Failure(exception);
        }
    }

    public static async Task<Result> ToResultAsync(this Func<ValueTask> taskFactory)
    {
        try
        {
            await taskFactory().ConfigureAwait(false);
            return ResultFactory.Success();
        }
        catch (Exception exception)
        {
            return ResultFactory.Failure(exception);
        }
    }

    public static Result ToResult(this bool condition)
    {
        return condition ? ResultFactory.Success() : ResultFactory.Failure();
    }

    public static Result ToResult(this bool condition, Problem problem)
    {
        return condition ? ResultFactory.Success() : ResultFactory.Failure(problem);
    }

    public static Result ToResult(this Func<bool> predicate)
    {
        return predicate() ? ResultFactory.Success() : ResultFactory.Failure();
    }

    public static Result ToResult(this Func<bool> predicate, Problem problem)
    {
        return predicate() ? ResultFactory.Success() : ResultFactory.Failure(problem);
    }
}
