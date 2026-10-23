using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ManagedCode.Communication.Results.Extensions;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication;

public partial struct Result
{
    /// <summary>
    ///     Executes the delegate and returns its outcome, recording the original exception immediately.
    /// </summary>
    public static Result Try(Action action, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
    {
        return action.TryAsResult(errorStatus);
    }

    /// <summary>
    ///     Executes the delegate and returns its outcome, recording the original exception immediately.
    /// </summary>
    public static Result Try(Action action, ILogger? logger, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError, Func<Exception, Problem>? problemFactory = null)
    {
        return action.TryAsResult(logger, errorStatus, problemFactory);
    }

    /// <summary>
    ///     Executes the delegate and returns its outcome, recording the original exception immediately.
    /// </summary>
    public static Result<T> Try<T>(Func<T> func, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
    {
        return func.TryAsResult(errorStatus);
    }

    /// <summary>
    ///     Executes the delegate and returns its outcome, recording the original exception immediately.
    /// </summary>
    public static Result<T> Try<T>(Func<T> func, ILogger? logger, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError, Func<Exception, Problem>? problemFactory = null)
    {
        return func.TryAsResult(logger, errorStatus, problemFactory);
    }

    /// <summary>
    ///     Executes the delegate and returns its outcome, recording the original exception immediately.
    /// </summary>
    [OverloadResolutionPriority(1)]
    public static Task<Result> TryAsync(Func<Task> func, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
    {
        return func.TryAsResultAsync(errorStatus);
    }

    /// <summary>
    ///     Executes the delegate and returns its outcome, recording the original exception immediately.
    /// </summary>
    [OverloadResolutionPriority(1)]
    public static Task<Result> TryAsync(Func<Task> func, ILogger? logger, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError, Func<Exception, Problem>? problemFactory = null)
    {
        return func.TryAsResultAsync(logger, errorStatus, problemFactory);
    }

    /// <summary>
    ///     Executes the delegate and returns its outcome, recording the original exception immediately.
    /// </summary>
    [OverloadResolutionPriority(1)]
    public static Task<Result<T>> TryAsync<T>(Func<Task<T>> func, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
    {
        return func.TryAsResultAsync(errorStatus);
    }

    /// <summary>
    ///     Executes the delegate and returns its outcome, recording the original exception immediately.
    /// </summary>
    [OverloadResolutionPriority(1)]
    public static Task<Result<T>> TryAsync<T>(Func<Task<T>> func, ILogger? logger, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError, Func<Exception, Problem>? problemFactory = null)
    {
        return func.TryAsResultAsync(logger, errorStatus, problemFactory);
    }

    /// <summary>
    ///     Executes the delegate and returns its outcome, recording the original exception immediately.
    /// </summary>
    public static ValueTask<Result> TryAsync(Func<ValueTask> func, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
    {
        return func.TryAsResultAsync(errorStatus);
    }

    /// <summary>
    ///     Executes the delegate and returns its outcome, recording the original exception immediately.
    /// </summary>
    public static ValueTask<Result> TryAsync(Func<ValueTask> func, ILogger? logger, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError, Func<Exception, Problem>? problemFactory = null)
    {
        return func.TryAsResultAsync(logger, errorStatus, problemFactory);
    }

    /// <summary>
    ///     Executes the delegate and returns its outcome, recording the original exception immediately.
    /// </summary>
    public static ValueTask<Result<T>> TryAsync<T>(Func<ValueTask<T>> func, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
    {
        return func.TryAsResultAsync(errorStatus);
    }

    /// <summary>
    ///     Executes the delegate and returns its outcome, recording the original exception immediately.
    /// </summary>
    public static ValueTask<Result<T>> TryAsync<T>(Func<ValueTask<T>> func, ILogger? logger, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError, Func<Exception, Problem>? problemFactory = null)
    {
        return func.TryAsResultAsync(logger, errorStatus, problemFactory);
    }
}
