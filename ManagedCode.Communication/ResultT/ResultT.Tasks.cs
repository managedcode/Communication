using System.Threading.Tasks;
using ManagedCode.Communication.Results.Extensions;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    public Task<Result<T>> AsTask()
    {
        return ResultTaskExtensions.AsTask(this);
    }

    public ValueTask<Result<T>> AsValueTask()
    {
        return ResultTaskExtensions.AsValueTask(this);
    }
}
