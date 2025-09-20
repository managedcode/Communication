using System;

namespace ManagedCode.Communication.Results.Extensions;

public static partial class ResultValueExecutionExtensions
{
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
