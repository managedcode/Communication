using System.Threading.Tasks;
using ManagedCode.Communication.CollectionResults.Extensions;

namespace ManagedCode.Communication.CollectionResultT;

public partial struct CollectionResult<T>
{
    public Task<CollectionResult<T>> AsTask()
    {
        return CollectionResultTaskExtensions.AsTask(this);
    }

    public ValueTask<CollectionResult<T>> AsValueTask()
    {
        return CollectionResultTaskExtensions.AsValueTask(this);
    }
}
