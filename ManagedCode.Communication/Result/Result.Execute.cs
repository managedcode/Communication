using System;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Communication;

public sealed partial class Result
{
    public static Result Execute(Action action)
    {
        try
        {
            action();
            return Succeed();
        }
        catch (Exception e)
        {
            return new Result(Error<ErrorCode>.FromException(e));
        }
    }

    public static Result Execute(Func<Result> func)
    {
        try
        {
            return func();
        }
        catch (Exception e)
        {
            return new Result(Error<ErrorCode>.FromException(e));
        }
    }

    public static async Task<Result> Execute(Task task, CancellationToken cancellationToken = default)
    {
        if (task.IsCompleted)
        {
            return Succeed();
        }

        if (task.IsCanceled || task.IsFaulted)
        {
            return new Result(Error<ErrorCode>.FromException(task.Exception));
        }
        
        try
        {
            await task;
            return Succeed();
        }
        catch (Exception e)
        {
            return new Result(Error<ErrorCode>.FromException(e));
        }
    }

    public static async Task<Result> Execute(Func<Task<Result>> task, CancellationToken cancellationToken = default)
    {
        try
        {
            return await task();
        }
        catch (Exception e)
        {
            return new Result(Error<ErrorCode>.FromException(e));
        }
    }
}

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
            return new Result<T>(Error<ErrorCode>.FromException(e));
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
            return new Result<T>(Error<ErrorCode>.FromException(e));
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
            return new Result<T>(Error<ErrorCode>.FromException(e));
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
            return new Result<T>(Error<ErrorCode>.FromException(e));
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
            return new Result<T>(Error<ErrorCode>.FromException(task.Exception));
        }
        
        try
        {
            var result = await task;
            return Succeed(result);
        }
        catch (Exception e)
        {
            return new Result<T>(Error<ErrorCode>.FromException(e));
        }
    }
}