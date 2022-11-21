using System;
using System.Net;

namespace ManagedCode.Communication;

public partial struct Result : IResult
{
    
    public Result(bool isSuccess)
    {
        ResultType = isSuccess ? Enum.GetName(typeof(HttpStatusCode),HttpStatusCode.OK) : Enum.GetName(typeof(HttpStatusCode),HttpStatusCode.InternalServerError);
        IsSuccess = isSuccess;
        IsFail = !isSuccess;
    }
    
    public Result(bool isSuccess, Enum resultCode)
    {
        ResultType = Enum.GetName(resultCode.GetType(), resultCode);
        IsSuccess = isSuccess;
        IsFail = !isSuccess;
    }

    public Result(Error error)
    {
        IsSuccess = false;
        IsFail = true;
        Errors = new [] { error };
    }

    public Result(Error[] errors)
    {
        IsSuccess = false;
        IsFail = true;
        Errors = errors;
    }

    public Result(bool isSuccess, Error[] errors)
    {
        IsSuccess = isSuccess;
        IsFail = !isSuccess;
        Errors = errors;
    }
    
    public TEnum ResultCode<TEnum>() where TEnum : Enum
    {
        return (TEnum)Enum.Parse(typeof(TEnum), ResultType);
    }
    
    public bool IsResultCode(Enum value)
    {
        return Enum.GetName(value.GetType(), value) == ResultType;
    }
    
    public bool IsNotResultCode(Enum value)
    {
        return Enum.GetName(value.GetType(), value) != ResultType;
    }

    public bool IsSuccess { get; set; }
    public bool IsFail { get; set; }
    public string ResultType { get; set; }
   

    public Error? Error
    {
        get
        {
            if (Errors == null || Errors.Length == 0)
                return null;

            return Errors[0];
        }
    }
    public Error[]? Errors { get; set; }
    
}