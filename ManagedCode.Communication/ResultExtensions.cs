using System;
using System.Threading.Tasks;

namespace ManagedCode.Communication;

public static class ResultExtensions
{
    public static Task<BaseResult<T, TErrorCode>> AsTask<T, TErrorCode>(this BaseResult<T, TErrorCode> value) where TErrorCode : Enum
    {
        return Task.FromResult(value);
    }

    public static Task<BaseResult<TErrorCode>> AsTask<TErrorCode>(this BaseResult<TErrorCode> value) where TErrorCode : Enum
    {
        return Task.FromResult(value);
    }
}