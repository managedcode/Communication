using System.Threading.Tasks;
using ManagedCode.Communication.Results.Extensions;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    /// <summary>
    ///     Wraps this result in a completed task.
    /// </summary>
    public Task<Result<T>> AsTask()
    {
        return ResultTaskExtensions.AsTask(this);
    }

    /// <summary>
    ///     Wraps this result in a completed value task.
    /// </summary>
    public ValueTask<Result<T>> AsValueTask()
    {
        return ResultTaskExtensions.AsValueTask(this);
    }
}
