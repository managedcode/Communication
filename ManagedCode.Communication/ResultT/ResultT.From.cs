using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Results.Extensions;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    /// <summary>
    ///     Runs the function and wraps its value, turning a thrown exception into a failure.
    /// </summary>
    public static Result<T> From(Func<T> func)
    {
        return func.ToResult();
    }

    /// <summary>
    ///     Runs the function and returns its result, turning a thrown exception into a failure.
    /// </summary>
    public static Result<T> From(Func<Result<T>> func)
    {
        return func.ToResult();
    }

    /// <summary>
    ///     Awaits the task and wraps its value, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<Result<T>> From(Task<T> task)
    {
        return await task.ToResultAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Awaits the task and returns its result, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<Result<T>> From(Task<Result<T>> task)
    {
        return await task.ToResultAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Invokes and awaits the factory, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<Result<T>> From(Func<Task<T>> task, CancellationToken cancellationToken = default)
    {
        return await task.ToResultAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Invokes and awaits the factory, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<Result<T>> From(Func<Task<Result<T>>> task, CancellationToken cancellationToken = default)
    {
        return await task.ToResultAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Returns the result unchanged; present so conversions compose uniformly.
    /// </summary>
    public static Result<T> From(Result<T> result)
    {
        return result.IsSuccess ? result : result.Problem != null ? Fail(result.Problem) : Fail();
    }

    /// <summary>
    ///     Discards the value and keeps only success or failure.
    /// </summary>
    public static Result From<U>(Result<U> result)
    {
        return result.ToResult();
    }

    /// <summary>
    ///     Awaits the value task and wraps its value, turning a thrown exception into a failure.
    /// </summary>
    public static async ValueTask<Result<T>> From(ValueTask<T> valueTask)
    {
        return await valueTask.ToResultAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Awaits the value task and returns its result, turning a thrown exception into a failure.
    /// </summary>
    public static async ValueTask<Result<T>> From(ValueTask<Result<T>> valueTask)
    {
        return await valueTask.ToResultAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Invokes and awaits the factory, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<Result<T>> From(Func<ValueTask<T>> valueTask)
    {
        return await valueTask.ToResultAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Invokes and awaits the factory, turning a thrown exception into a failure.
    /// </summary>
    public static async Task<Result<T>> From(Func<ValueTask<Result<T>>> valueTask)
    {
        return await valueTask.ToResultAsync().ConfigureAwait(false);
    }
}
