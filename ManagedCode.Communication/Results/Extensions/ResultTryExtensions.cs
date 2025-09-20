using System;
using System.Net;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Communication.Results;

namespace ManagedCode.Communication.Results.Extensions;

/// <summary>
///     Extensions that wrap delegate execution with failure handling semantics.
/// </summary>
public static class ResultTryExtensions
{
    public static Result TryAsResult(this Action action, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
    {
        try
        {
            action();
            return Result.Succeed();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception, errorStatus);
        }
    }

    public static Result<T> TryAsResult<T>(this Func<T> func, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
    {
        try
        {
            return Result<T>.Succeed(func());
        }
        catch (Exception exception)
        {
            return Result<T>.Fail(exception, errorStatus);
        }
    }

    public static async Task<Result> TryAsResultAsync(this Func<Task> func, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
    {
        try
        {
            await func().ConfigureAwait(false);
            return Result.Succeed();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception, errorStatus);
        }
    }

    public static async Task<Result<T>> TryAsResultAsync<T>(this Func<Task<T>> func, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
    {
        try
        {
            var value = await func().ConfigureAwait(false);
            return Result<T>.Succeed(value);
        }
        catch (Exception exception)
        {
            return Result<T>.Fail(exception, errorStatus);
        }
    }
}
