using System;
using System.Threading.Tasks;

namespace ManagedCode.Communication.Extensions;

public static class ResultExtensions
{
    public static Task<BaseResult<T, TErrorCode>> AsTask<T, TErrorCode>(this BaseResult<T, TErrorCode> result) where TErrorCode : Enum
    {
        return Task.FromResult(result);
    }

    public static Task<BaseResult<TErrorCode>> AsTask<TErrorCode>(this BaseResult<TErrorCode> result) where TErrorCode : Enum
    {
        return Task.FromResult(result);
    }

    public static Task<Result<T>> AsTask<T>(this Result<T> result)
    {
        return Task.FromResult(result);
    }

    public static Task<Result> AsTask(this Result result)
    {
        return Task.FromResult(result);
    }
}