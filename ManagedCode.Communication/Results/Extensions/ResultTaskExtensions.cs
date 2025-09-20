using System.Threading.Tasks;
using ManagedCode.Communication;

namespace ManagedCode.Communication.Results.Extensions;

/// <summary>
///     Helpers for exposing results as tasks.
/// </summary>
public static class ResultTaskExtensions
{
    public static Task<Result> AsTask(this Result result)
    {
        return Task.FromResult(result);
    }

    public static ValueTask<Result> AsValueTask(this Result result)
    {
        return ValueTask.FromResult(result);
    }

    public static Task<Result<T>> AsTask<T>(this Result<T> result)
    {
        return Task.FromResult(result);
    }

    public static ValueTask<Result<T>> AsValueTask<T>(this Result<T> result)
    {
        return ValueTask.FromResult(result);
    }
}
