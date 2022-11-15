using System;
using System.Collections.Generic;
using System.Linq;

namespace ManagedCode.Communication;

public abstract class BaseResult<TErrorCode> where TErrorCode : Enum
{
    protected BaseResult(bool isSuccess)
    {
        IsSuccess = isSuccess;
    }

    protected BaseResult(Error<TErrorCode> error)
    {
        IsSuccess = false;
        Errors = new List<Error<TErrorCode>> { error };
    }

    protected BaseResult(List<Error<TErrorCode>> errors)
    {
        IsSuccess = false;
        Errors = errors;
    }

    protected BaseResult(bool isSuccess, List<Error<TErrorCode>> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public bool IsFail => !IsSuccess;
    public Error<TErrorCode>? Error => Errors?.FirstOrDefault();
    public List<Error<TErrorCode>>? Errors { get; }
}