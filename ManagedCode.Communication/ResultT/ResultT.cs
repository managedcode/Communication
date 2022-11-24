using System;
using System.Diagnostics;
using System.Net;

namespace ManagedCode.Communication;

[DebuggerDisplay("IsSuccess: {IsSuccess}; {GetError().HasValue ? \" Error code: \" + GetError()!.Value.ErrorCode : string.Empty}")]
public partial struct Result<T> : IResult<T>
{
    private Result(bool isSuccess, T? value, Error[]? errors)
    {
        IsSuccess = isSuccess;
        IsFailed = !isSuccess;
        Value = value;
        Errors = errors;
    }
    
    public bool IsSuccess { get; set; }
    public bool IsFailed { get; set; }

    public T? Value { get; set; }
    
    public Error? GetError()
    {
        if (Errors == null || Errors.Length == 0)
            return null;

        return Errors[0];
    }
    
    public Error[]? Errors { get; set; }
    
}