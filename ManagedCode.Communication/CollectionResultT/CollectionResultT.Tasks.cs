using System.Threading.Tasks;
using ManagedCode.Communication.CollectionResults.Extensions;

namespace ManagedCode.Communication.CollectionResultT;

public partial struct CollectionResult<T>
{
    /// <summary>
    ///     Wraps this result in a completed task.
    /// </summary>
    public Task<CollectionResult<T>> AsTask()
    {
        return CollectionResultTaskExtensions.AsTask(this);
    }

    /// <summary>
    ///     Wraps this result in a completed value task.
    /// </summary>
    public ValueTask<CollectionResult<T>> AsValueTask()
    {
        return CollectionResultTaskExtensions.AsValueTask(this);
    }
}
