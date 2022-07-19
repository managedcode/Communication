using System.Threading.Tasks;

namespace ManagedCode.Communication;

public static class ResultExtensions
{
    public static Result<T> ToSucceededResult<T>(this T value)
    {
        return new Result<T>(true, value);
    }

    public static Task<Result<T>> AsTask<T>(this Result<T> value)
    {
        return Task.FromResult(value);
    }

    public static Task<Result> AsTask(this Result value)
    {
        return Task.FromResult(value);
    }
}