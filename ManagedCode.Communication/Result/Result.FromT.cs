using System;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Communication;

public partial struct Result
{
    public static Result<T> From<T>(Func<T> func)
    {
        try
        {
            return Succeed(func());
        }
        catch (Exception e)
        {
            return Fail<T>(Error.FromException(e));
        }
    }

    public static async Task<Result<T>> From<T>(Task<T> task)
    {
        try
        {
            return Succeed(await task);
        }
        catch (Exception e)
        {
            return Fail<T>(Error.FromException(e));
        }
    }

    public static async Task<Result<T>> From<T>(Func<Task<T>> task, CancellationToken cancellationToken = default)
    {
        try
        {
            return Succeed(await Task.Run(task, cancellationToken));
        }
        catch (Exception e)
        {
            return Fail<T>(Error.FromException(e));
        }
    }

#if NET6_0_OR_GREATER

    public static async ValueTask<Result<T>> From<T>(ValueTask<T> valueTask)
    {
        try
        {
            return Succeed(await valueTask);
        }
        catch (Exception e)
        {
            return Fail<T>(Error.FromException(e));
        }
    }

    public static async Task<Result<T>> From<T>(Func<ValueTask<T>> valueTask)
    {
        try
        {
            return Succeed(await valueTask());
        }
        catch (Exception e)
        {
            return Fail<T>(Error.FromException(e));
        }
    }

#endif
}