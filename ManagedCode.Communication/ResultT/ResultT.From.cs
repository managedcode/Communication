using System;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    public static Result<T> From(Func<T> func)
    {
        try
        {
            return Succeed(func());
        }
        catch (Exception e)
        {
            return Fail(Error.FromException(e));
        }
    }

    public static Result<T> From(Func<Result<T>> func)
    {
        try
        {
            return func();
        }
        catch (Exception e)
        {
            return Fail(Error.FromException(e));
        }
    }

    public static async Task<Result<T>> From(Task<T> task)
    {
        try
        {
            return Succeed(await task);
        }
        catch (Exception e)
        {
            return Fail(Error.FromException(e));
        }
    }

    public static async Task<Result<T>> From(Task<Result<T>> task)
    {
        try
        {
            return await task;
        }
        catch (Exception e)
        {
            return Fail(Error.FromException(e));
        }
    }

    public static async Task<Result<T>> From(Func<Task<T>> task, CancellationToken cancellationToken = default)
    {
        try
        {
            return Succeed(await Task.Run(task, cancellationToken));
        }
        catch (Exception e)
        {
            return Fail(Error.FromException(e));
        }
    }

    public static async Task<Result<T>> From(Func<Task<Result<T>>> task, CancellationToken cancellationToken = default)
    {
        try
        {
            return await Task.Run(task, cancellationToken);
        }
        catch (Exception e)
        {
            return Fail(Error.FromException(e));
        }
    }

    public static Result<T> From(Result<T> result)
    {
        return result ? result : Fail(result.Errors);
    }

    public static Result From<T>(Result<T> result)
    {
        if (result)
            return Result.Succeed();

        return Result.Fail(result.Errors);
    }

    public static async ValueTask<Result<T>> From(ValueTask<T> valueTask)
    {
        try
        {
            return Succeed(await valueTask);
        }
        catch (Exception e)
        {
            return Fail(Error.FromException(e));
        }
    }

    public static async ValueTask<Result<T>> From(ValueTask<Result<T>> valueTask)
    {
        try
        {
            return await valueTask;
        }
        catch (Exception e)
        {
            return Fail(Error.FromException(e));
        }
    }

    public static async Task<Result<T>> From(Func<ValueTask<T>> valueTask)
    {
        try
        {
            return Succeed(await valueTask());
        }
        catch (Exception e)
        {
            return Fail(Error.FromException(e));
        }
    }

    public static async Task<Result<T>> From(Func<ValueTask<Result<T>>> valueTask)
    {
        try
        {
            return await valueTask();
        }
        catch (Exception e)
        {
            return Fail(Error.FromException(e));
        }
    }
}