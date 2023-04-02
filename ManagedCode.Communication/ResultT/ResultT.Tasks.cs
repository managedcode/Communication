using System.Threading.Tasks;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    public Task<Result<T>> AsTask()
    {
        return Task.FromResult(this);
    }

    public ValueTask<Result<T>> AsValueTask()
    {
        return ValueTask.FromResult(this);
    }
    
}