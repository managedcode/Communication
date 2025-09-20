using System;
using System.Net;
using System.Threading.Tasks;
using ManagedCode.Communication.Results.Extensions;

namespace ManagedCode.Communication;

public partial struct Result
{
    /// <summary>
    ///     Executes a function and returns a Result, catching any exceptions.
    /// </summary>
    public static Result Try(Action action, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
    {
        return action.TryAsResult(errorStatus);
    }

    /// <summary>
    ///     Executes a function and returns a Result<T>, catching any exceptions.
    /// </summary>
    public static Result<T> Try<T>(Func<T> func, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
    {
        return func.TryAsResult(errorStatus);
    }

    /// <summary>
    ///     Executes an async function and returns a Result, catching any exceptions.
    /// </summary>
    public static async Task<Result> TryAsync(Func<Task> func, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
    {
        return await func.TryAsResultAsync(errorStatus).ConfigureAwait(false);
    }

    /// <summary>
    ///     Executes an async function and returns a Result<T>, catching any exceptions.
    /// </summary>
    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> func, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
    {
        return await func.TryAsResultAsync(errorStatus).ConfigureAwait(false);
    }
}
