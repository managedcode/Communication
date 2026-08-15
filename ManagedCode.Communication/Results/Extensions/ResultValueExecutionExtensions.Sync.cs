using System;

namespace ManagedCode.Communication.Results.Extensions;

public static partial class ResultValueExecutionExtensions
{
    /// <summary>
    ///     Runs the delegate and wraps its outcome, turning a thrown exception into a failure.
    /// </summary>
    public static Result<T> ToResult<T>(this Func<T> func)
    {
        try
        {
            return Result<T>.Succeed(func());
        }
        catch (Exception exception)
        {
            return Result<T>.Fail(exception);
        }
    }

    /// <summary>
    ///     Runs the delegate and wraps its outcome, turning a thrown exception into a failure.
    /// </summary>
    public static Result<T> ToResult<T>(this Func<Result<T>> func)
    {
        try
        {
            return func();
        }
        catch (Exception exception)
        {
            return Result<T>.Fail(exception);
        }
    }
}
