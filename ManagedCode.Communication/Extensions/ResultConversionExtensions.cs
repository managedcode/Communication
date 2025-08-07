using System;

namespace ManagedCode.Communication.Extensions;

public static class ResultConversionExtensions
{
    public static Result<T> AsResult<T>(this T result)
    {
        return Result<T>.Succeed(result);
    }

    public static Result<T> AsResult<T>(this Exception exception)
    {
        return Result<T>.Fail(exception);
    }

    public static Result AsResult(this Exception exception)
    {
        return Result.Fail(exception);
    }
}