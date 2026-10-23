using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication.Results.Extensions;

/// <summary>
///     Extensions that wrap delegate execution with immediate failure diagnostics.
/// </summary>
public static class ResultTryExtensions
{
    /// <summary>
    ///     Executes the delegate, converting exceptions into failures with their original diagnostics.
    /// </summary>
    public static Result TryAsResult(this Action action, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
    {
        return TryAsResult(action, null, errorStatus);
    }

    /// <summary>
    ///     Executes the delegate and immediately logs a caught exception through the supplied logger.
    /// </summary>
    public static Result TryAsResult(this Action action, ILogger? logger, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError, Func<Exception, Problem>? problemFactory = null)
    {
        try
        {
            action();
            return Result.Succeed();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception, logger, errorStatus, problemFactory);
        }
    }

    /// <summary>
    ///     Executes the delegate, converting exceptions into failures with their original diagnostics.
    /// </summary>
    public static Result<T> TryAsResult<T>(this Func<T> func, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
    {
        return TryAsResult(func, null, errorStatus);
    }

    /// <summary>
    ///     Executes the delegate and immediately logs a caught exception through the supplied logger.
    /// </summary>
    public static Result<T> TryAsResult<T>(this Func<T> func, ILogger? logger, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError, Func<Exception, Problem>? problemFactory = null)
    {
        try
        {
            return Result<T>.Succeed(func());
        }
        catch (Exception exception)
        {
            return Result<T>.Fail(exception, logger, errorStatus, problemFactory);
        }
    }

    /// <summary>
    ///     Executes the delegate, converting exceptions into failures with their original diagnostics.
    /// </summary>
    [OverloadResolutionPriority(1)]
    public static Task<Result> TryAsResultAsync(this Func<Task> func, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
    {
        return TryAsResultAsync(func, null, errorStatus);
    }

    /// <summary>
    ///     Executes the delegate and immediately logs a caught exception through the supplied logger.
    /// </summary>
    [OverloadResolutionPriority(1)]
    public static async Task<Result> TryAsResultAsync(this Func<Task> func, ILogger? logger, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError, Func<Exception, Problem>? problemFactory = null)
    {
        try
        {
            await func().ConfigureAwait(false);
            return Result.Succeed();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception, logger, errorStatus, problemFactory);
        }
    }

    /// <summary>
    ///     Executes the delegate, converting exceptions into failures with their original diagnostics.
    /// </summary>
    [OverloadResolutionPriority(1)]
    public static Task<Result<T>> TryAsResultAsync<T>(this Func<Task<T>> func, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
    {
        return TryAsResultAsync(func, null, errorStatus);
    }

    /// <summary>
    ///     Executes the delegate and immediately logs a caught exception through the supplied logger.
    /// </summary>
    [OverloadResolutionPriority(1)]
    public static async Task<Result<T>> TryAsResultAsync<T>(this Func<Task<T>> func, ILogger? logger, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError, Func<Exception, Problem>? problemFactory = null)
    {
        try
        {
            return Result<T>.Succeed(await func().ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return Result<T>.Fail(exception, logger, errorStatus, problemFactory);
        }
    }

    /// <summary>
    ///     Executes the delegate, converting exceptions into failures with their original diagnostics.
    /// </summary>
    public static ValueTask<Result> TryAsResultAsync(this Func<ValueTask> func, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
    {
        return TryAsResultAsync(func, null, errorStatus);
    }

    /// <summary>
    ///     Executes the delegate and immediately logs a caught exception through the supplied logger.
    /// </summary>
    public static async ValueTask<Result> TryAsResultAsync(this Func<ValueTask> func, ILogger? logger, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError, Func<Exception, Problem>? problemFactory = null)
    {
        try
        {
            await func().ConfigureAwait(false);
            return Result.Succeed();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception, logger, errorStatus, problemFactory);
        }
    }

    /// <summary>
    ///     Executes the delegate, converting exceptions into failures with their original diagnostics.
    /// </summary>
    public static ValueTask<Result<T>> TryAsResultAsync<T>(this Func<ValueTask<T>> func, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
    {
        return TryAsResultAsync(func, null, errorStatus);
    }

    /// <summary>
    ///     Executes the delegate and immediately logs a caught exception through the supplied logger.
    /// </summary>
    public static async ValueTask<Result<T>> TryAsResultAsync<T>(this Func<ValueTask<T>> func, ILogger? logger, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError, Func<Exception, Problem>? problemFactory = null)
    {
        try
        {
            return Result<T>.Succeed(await func().ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return Result<T>.Fail(exception, logger, errorStatus, problemFactory);
        }
    }
}
