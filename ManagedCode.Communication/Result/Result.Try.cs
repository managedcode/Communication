using System;
using System.Net;
using System.Threading.Tasks;

namespace ManagedCode.Communication;

public partial struct Result
{
    /// <summary>
    /// Executes a function and returns a Result, catching any exceptions.
    /// </summary>
    public static Result Try(Action action, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
    {
        try
        {
            action();
            return Succeed();
        }
        catch (Exception ex)
        {
            return Fail(ex, errorStatus);
        }
    }
    
    /// <summary>
    /// Executes a function and returns a Result<T>, catching any exceptions.
    /// </summary>
    public static Result<T> Try<T>(Func<T> func, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
    {
        try
        {
            return Result<T>.Succeed(func());
        }
        catch (Exception ex)
        {
            return Result<T>.Fail(ex, errorStatus);
        }
    }
    
    /// <summary>
    /// Executes an async function and returns a Result, catching any exceptions.
    /// </summary>
    public static async Task<Result> TryAsync(Func<Task> func, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
    {
        try
        {
            await func();
            return Succeed();
        }
        catch (Exception ex)
        {
            return Fail(ex, errorStatus);
        }
    }
    
    /// <summary>
    /// Executes an async function and returns a Result<T>, catching any exceptions.
    /// </summary>
    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> func, HttpStatusCode errorStatus = HttpStatusCode.InternalServerError)
    {
        try
        {
            var result = await func();
            return Result<T>.Succeed(result);
        }
        catch (Exception ex)
        {
            return Result<T>.Fail(ex, errorStatus);
        }
    }
}