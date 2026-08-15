using System;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Communication;

public partial struct Result
{
    /// <summary>
    ///     Runs the delegate and wraps its outcome, turning a thrown exception into a failure.
    /// </summary>
    public static Result<T> From<T>(Func<T> func)
    {
        try
        {
            return Succeed(func());
        }
        catch (Exception e)
        {
            return Fail<T>(e);
        }
    }

    /// <summary>
    ///     Runs the delegate and wraps its outcome, turning a thrown exception into a failure.
    /// </summary>
    public static Result<T> From<T>(Func<Result<T>> func)
    {
        try
        {
            return func();
        }
        catch (Exception e)
        {
            return Fail<T>(e);
        }
    }

    /// <summary>
    ///     Awaits the operation and wraps its outcome, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<Result<T>> From<T>(Task<T> task)
    {
        try
        {
            return Succeed(await task);
        }
        catch (Exception e)
        {
            return Fail<T>(e);
        }
    }

    /// <summary>
    ///     Awaits the operation and wraps its outcome, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<Result<T>> From<T>(Task<Result<T>> task)
    {
        try
        {
            return await task;
        }
        catch (Exception e)
        {
            return Fail<T>(e);
        }
    }

    /// <summary>
    ///     Invokes and awaits the factory, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<Result<T>> From<T>(Func<Task<T>> task, CancellationToken cancellationToken = default)
    {
        try
        {
            return Succeed(await Task.Run(task, cancellationToken));
        }
        catch (Exception e)
        {
            return Fail<T>(e);
        }
    }

    /// <summary>
    ///     Invokes and awaits the factory, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<Result<T>> From<T>(Func<Task<Result<T>>> task, CancellationToken cancellationToken = default)
    {
        try
        {
            return await Task.Run(task, cancellationToken);
        }
        catch (Exception e)
        {
            return Fail<T>(e);
        }
    }

    /// <summary>
    ///     Awaits the operation and wraps its outcome, turning a thrown exception into a failure.
    /// </summary>
    public static async ValueTask<Result<T>> From<T>(ValueTask<T> valueTask)
    {
        try
        {
            return Succeed(await valueTask);
        }
        catch (Exception e)
        {
            return Fail<T>(e);
        }
    }

    /// <summary>
    ///     Awaits the operation and wraps its outcome, turning a thrown exception into a failure.
    /// </summary>
    public static async ValueTask<Result<T>> From<T>(ValueTask<Result<T>> valueTask)
    {
        try
        {
            return await valueTask;
        }
        catch (Exception e)
        {
            return Fail<T>(e);
        }
    }

    /// <summary>
    ///     Invokes and awaits the factory, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<Result<T>> From<T>(Func<ValueTask<T>> valueTask)
    {
        try
        {
            return Succeed(await valueTask());
        }
        catch (Exception e)
        {
            return Fail<T>(e);
        }
    }

    /// <summary>
    ///     Invokes and awaits the factory, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<Result<T>> From<T>(Func<ValueTask<Result<T>>> valueTask)
    {
        try
        {
            return await valueTask();
        }
        catch (Exception e)
        {
            return Fail<T>(e);
        }
    }
}