using System;
using System.Net;

namespace ManagedCode.Communication;

public partial struct Result
{
    public Result()
    {
    }

    public Result(bool isSuccess)
    {
        ResultType = isSuccess ? Enum.GetName(typeof(HttpStatusCode),HttpStatusCode.OK) : Enum.GetName(typeof(HttpStatusCode),HttpStatusCode.InternalServerError);
        IsSuccess = isSuccess;
    }
    
    public Result(bool isSuccess, Enum resultCode)
    {
        ResultType = Enum.GetName(resultCode.GetType(), resultCode);
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

    public readonly bool IsSuccess;

    public readonly string? ResultType;

    public TEnum ResultCode<TEnum>() where TEnum : Enum
    {
        return (TEnum)Enum.Parse(typeof(TEnum), ResultType);
    }

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
    public Error[]? Errors { get; private set; }
    
}