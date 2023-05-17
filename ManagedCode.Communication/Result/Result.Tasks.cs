using System.Threading.Tasks;

namespace ManagedCode.Communication;

public partial struct Result
{
    public Task<Result> AsTask()
    {
        return Task.FromResult(this);
    }

    public ValueTask<Result> AsValueTask()
    {
        return ValueTask.FromResult(this);
    }
}