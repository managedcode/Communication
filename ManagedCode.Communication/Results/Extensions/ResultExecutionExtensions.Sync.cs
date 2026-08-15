using System;

namespace ManagedCode.Communication.Results.Extensions;

public static partial class ResultExecutionExtensions
{
    /// <summary>
    ///     Runs the delegate and wraps its outcome, turning a thrown exception into a failure.
    /// </summary>
    public static Result ToResult(this Action action)
    {
        try
        {
            action();
            return Result.Succeed();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception);
        }
    }

    /// <summary>
    ///     Runs the delegate and wraps its outcome, turning a thrown exception into a failure.
    /// </summary>
    public static Result ToResult(this Func<Result> func)
    {
        try
        {
            return func();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception);
        }
    }
}
