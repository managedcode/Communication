using System;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Communication.ZALIPA.Result;

public sealed partial class Result<T>
{
    public static Result<T> Execute(Func<Result<T>> func)
    {
        try
        {
            return func();
        }
        catch (Exception e)
        {
            return new Result<T>(Error.FromException(e));
        }
    }

    public static Result<T> Execute(Func<T> func)
    {
        try
        {
            var result = func();
            return Succeed(result);
        }
        catch (Exception e)
        {
            return new Result<T>(Error.FromException(e));
        }
    }

    public static async Task<Result<T>> Execute(Func<Task<T>> task, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await task();
            return Succeed(result);
        }
        catch (Exception e)
        {
            return new Result<T>(Error.FromException(e));
        }
    }

    public static async Task<Result<T>> Execute(Func<Task<Result<T>>> task, CancellationToken cancellationToken = default)
    {
        try
        {
            return await task();
        }
        catch (Exception e)
        {
            return new Result<T>(Error.FromException(e));
        }
    }

    public static async Task<Result<T>> Execute(Task<T> task, CancellationToken cancellationToken = default)
    {
        if (task.IsCompleted)
        {
            return Succeed(task.Result);
        }

        if (task.IsCanceled || task.IsFaulted)
        {
            return new Result<T>(Error.FromException(task.Exception));
        }

        try
        {
            var result = await task;
            return Succeed(result);
        }
        catch (Exception e)
        {
            return new Result<T>(Error.FromException(e));
        }
    }
}