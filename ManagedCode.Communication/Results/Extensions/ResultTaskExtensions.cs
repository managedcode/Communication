using System.Threading.Tasks;
using ManagedCode.Communication;

namespace ManagedCode.Communication.Results.Extensions;

/// <summary>
///     Helpers for exposing results as tasks.
/// </summary>
public static class ResultTaskExtensions
{
    /// <summary>
    ///     Wraps the result in a completed task.
    /// </summary>
    public static Task<Result> AsTask(this Result result)
    {
        return Task.FromResult(result);
    }

    /// <summary>
    ///     Wraps the result in a completed value task.
    /// </summary>
    public static ValueTask<Result> AsValueTask(this Result result)
    {
        return ValueTask.FromResult(result);
    }

    /// <summary>
    ///     Wraps the result in a completed task.
    /// </summary>
    public static Task<Result<T>> AsTask<T>(this Result<T> result)
    {
        return Task.FromResult(result);
    }

    /// <summary>
    ///     Wraps the result in a completed value task.
    /// </summary>
    public static ValueTask<Result<T>> AsValueTask<T>(this Result<T> result)
    {
        return ValueTask.FromResult(result);
    }
}
