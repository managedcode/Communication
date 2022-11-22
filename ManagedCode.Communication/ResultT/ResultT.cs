using System;
using System.Net;

namespace ManagedCode.Communication;

public partial struct Result<T> : IResult<T>
{
    private Result(bool isSuccess, T value, Error[]? errors)
    {
        IsSuccess = isSuccess;
        IsFail = !isSuccess;
        Value = value;
        Errors = errors;
    }
    
    public bool IsSuccess { get; set; }
    public bool IsFail { get; set; }

    public T Value { get; set; }
    
    public Error? GetError()
    {
        if (Errors == null || Errors.Length == 0)
            return null;

        return Errors[0];
    }
    
    public Error[]? Errors { get; set; }
    
}