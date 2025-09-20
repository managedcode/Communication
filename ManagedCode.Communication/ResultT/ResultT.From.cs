using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication.Results.Extensions;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    public static Result<T> From(Func<T> func)
    {
        return func.ToResult();
    }

    public static Result<T> From(Func<Result<T>> func)
    {
        return func.ToResult();
    }

    public static async Task<Result<T>> From(Task<T> task)
    {
        return await task.ToResultAsync().ConfigureAwait(false);
    }

    public static async Task<Result<T>> From(Task<Result<T>> task)
    {
        return await task.ToResultAsync().ConfigureAwait(false);
    }

    public static async Task<Result<T>> From(Func<Task<T>> task, CancellationToken cancellationToken = default)
    {
        return await task.ToResultAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async Task<Result<T>> From(Func<Task<Result<T>>> task, CancellationToken cancellationToken = default)
    {
        return await task.ToResultAsync(cancellationToken).ConfigureAwait(false);
    }

    public static Result<T> From(Result<T> result)
    {
        return result.IsSuccess ? result : result.Problem != null ? Fail(result.Problem) : Fail();
    }

    public static Result From<U>(Result<U> result)
    {
        return result.ToResult();
    }

    public static async ValueTask<Result<T>> From(ValueTask<T> valueTask)
    {
        return await valueTask.ToResultAsync().ConfigureAwait(false);
    }

    public static async ValueTask<Result<T>> From(ValueTask<Result<T>> valueTask)
    {
        return await valueTask.ToResultAsync().ConfigureAwait(false);
    }

    public static async Task<Result<T>> From(Func<ValueTask<T>> valueTask)
    {
        return await valueTask.ToResultAsync().ConfigureAwait(false);
    }

    public static async Task<Result<T>> From(Func<ValueTask<Result<T>>> valueTask)
    {
        return await valueTask.ToResultAsync().ConfigureAwait(false);
    }
}
