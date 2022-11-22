using System;
using System.Diagnostics;
using System.Net;

namespace ManagedCode.Communication;

[DebuggerDisplay("IsSuccess: {IsSuccess}; {GetError().HasValue ? \" Error code: \" + GetError()!.Value.ErrorCode : string.Empty}")]
public partial struct Result : IResult
{
    
    private Result(bool isSuccess, Error[]? errors)
    {
        IsSuccess = isSuccess;
        IsFail = !isSuccess;
        Errors = errors;
    }
    

    public bool IsSuccess { get; set; }
    public bool IsFail { get; set; }
    
    public Error? GetError()
    {
        if (Errors == null || Errors.Length == 0)
            return null;

        return Errors[0];
    }

    public Error[]? Errors { get; set; }
}