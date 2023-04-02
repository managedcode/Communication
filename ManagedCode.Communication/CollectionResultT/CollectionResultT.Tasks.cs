using System.Threading.Tasks;

namespace ManagedCode.Communication;

public partial struct CollectionResult<T>
{
    public Task<CollectionResult<T>> AsTask()
    {
        return Task.FromResult(this);
    }

    public ValueTask<CollectionResult<T>> AsValueTask()
    {
        return ValueTask.FromResult(this);
    }
    
}