using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Communication;

public partial struct CollectionResult<T>
{
    public static CollectionResult<T> From(Func<T[]> func)
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
    
    public static CollectionResult<T> From(Func<IEnumerable<T>> func)
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

    public static CollectionResult<T> From(Func<CollectionResult<T>> func)
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

    public static async Task<CollectionResult<T>> From(Task<T[]> task)
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
    
    public static async Task<CollectionResult<T>> From(Task<IEnumerable<T>> task)
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


    public static async Task<CollectionResult<T>> From(Task<CollectionResult<T>> task)
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

    public static async Task<CollectionResult<T>> From(Func<Task<T[]>> task, CancellationToken cancellationToken = default)
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
    
    public static async Task<CollectionResult<T>> From(Func<Task<IEnumerable<T>>> task, CancellationToken cancellationToken = default)
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

    public static async Task<CollectionResult<T>> From(Func<Task<CollectionResult<T>>> task, CancellationToken cancellationToken = default)
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

    public static CollectionResult<T> From(CollectionResult<T> result)
    {
        return result ? result : Fail(result.Errors);
    }

    public static Result From<T>(CollectionResult<T> result)
    {
        if (result)
        {
            return Result.Succeed();
        }

        return Result.Fail(result.Errors);
    }

#if NET6_0_OR_GREATER

    public static async ValueTask<CollectionResult<T>> From(ValueTask<T[]> valueTask)
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
    
    public static async ValueTask<CollectionResult<T>> From(ValueTask<IEnumerable<T>> valueTask)
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

    public static async ValueTask<CollectionResult<T>> From(ValueTask<CollectionResult<T>> valueTask)
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

    public static async Task<CollectionResult<T>> From(Func<ValueTask<T[]>> valueTask)
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

    public static async Task<CollectionResult<T>> From(Func<ValueTask<IEnumerable<T>>> valueTask)
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
    public static async Task<CollectionResult<T>> From(Func<ValueTask<CollectionResult<T>>> valueTask)
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

#endif
}