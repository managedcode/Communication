using System;
using System.Net;

namespace ManagedCode.Communication;

public partial struct Result<T> : IResult<T>
{
    private Result(bool isSuccess, T value, string resultType, Error[] errors)
    {
        IsSuccess = isSuccess;
        IsFail = !isSuccess;
        ResultType = resultType;
        Value = value;
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

    
    public T? Value { get; set; }
    
    public Error? GetError()
    {
        if (Errors == null || Errors.Length == 0)
            return null;

        return Errors[0];
    }
    
    public Error[]? Errors { get; set; }
    
}