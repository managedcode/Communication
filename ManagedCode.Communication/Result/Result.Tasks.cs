using System.Threading.Tasks;
using ManagedCode.Communication.Results.Extensions;

namespace ManagedCode.Communication;

public partial struct Result
{
    public Task<Result> AsTask()
    {
        return ResultTaskExtensions.AsTask(this);
    }

    public ValueTask<Result> AsValueTask()
    {
        return ResultTaskExtensions.AsValueTask(this);
    }
}
