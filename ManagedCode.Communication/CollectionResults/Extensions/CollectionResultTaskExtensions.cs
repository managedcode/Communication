using System.Threading.Tasks;
using ManagedCode.Communication.CollectionResultT;

namespace ManagedCode.Communication.CollectionResults.Extensions;

/// <summary>
///     Conversion helpers for <see cref="CollectionResult{T}"/> asynchronous pipelines.
/// </summary>
public static class CollectionResultTaskExtensions
{
    /// <summary>
    ///     Wraps the result in a completed task.
    /// </summary>
    public static Task<CollectionResult<T>> AsTask<T>(this CollectionResult<T> result)
    {
        return Task.FromResult(result);
    }

    /// <summary>
    ///     Wraps the result in a completed value task.
    /// </summary>
    public static ValueTask<CollectionResult<T>> AsValueTask<T>(this CollectionResult<T> result)
    {
        return ValueTask.FromResult(result);
    }
}
