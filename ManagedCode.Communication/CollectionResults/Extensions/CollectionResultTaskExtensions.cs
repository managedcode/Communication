using System.Threading.Tasks;
using ManagedCode.Communication.CollectionResultT;

namespace ManagedCode.Communication.CollectionResults.Extensions;

/// <summary>
///     Conversion helpers for <see cref="CollectionResult{T}"/> asynchronous pipelines.
/// </summary>
public static class CollectionResultTaskExtensions
{
    public static Task<CollectionResult<T>> AsTask<T>(this CollectionResult<T> result)
    {
        return Task.FromResult(result);
    }

    public static ValueTask<CollectionResult<T>> AsValueTask<T>(this CollectionResult<T> result)
    {
        return ValueTask.FromResult(result);
    }
}
