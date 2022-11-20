using System;

namespace ManagedCode.Communication;

public partial class Result
{
    public Result()
    {
    }

    public Result(bool isSuccess)
    {
        ResultCode = isSuccess ? ResultCodes.Ok : ResultCodes.Unknown;
        IsSuccess = isSuccess;
    }
    
    public Result(bool isSuccess, Enum resultCode)
    {
        ResultCode = resultCode;
        IsSuccess = isSuccess;
    }

    public Result(Error error)
    {
        IsSuccess = false;
        Errors = new [] { error };
    }

    public Result(Error[] errors)
    {
        IsSuccess = false;
        Errors = errors;
    }

    public Result(bool isSuccess, Error[] errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; protected set; }
    public Enum ResultCode { get; protected set; } = ResultCodes.Unknown;
    
    public bool IsFail => !IsSuccess;

    public Error? Error
    {
        get
        {
            if (Errors == null || Errors.Length == 0)
                return null;

            return Errors[0];
        }
    }
    public Error[]? Errors { get; protected set; }
    
}