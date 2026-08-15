using System;

namespace ManagedCode.Communication;

/// <summary>
///     Lifts plain values and exceptions into results.
/// </summary>
public static class ResultConversionExtensions
{
    /// <summary>
    ///     Wraps the value in a successful result.
    /// </summary>
    public static Result<T> AsResult<T>(this T result)
    {
        return Result<T>.Succeed(result);
    }

    /// <summary>
    ///     Wraps the exception in a typed failure.
    /// </summary>
    public static Result<T> AsResult<T>(this Exception exception)
    {
        return Result<T>.Fail(exception);
    }

    /// <summary>
    ///     Wraps the exception in a failure.
    /// </summary>
    public static Result AsResult(this Exception exception)
    {
        return Result.Fail(exception);
    }
}