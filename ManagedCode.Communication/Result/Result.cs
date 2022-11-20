using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ManagedCode.Communication;

public partial class Result
{
    public Result()
    {
    }

    public Result(bool isSuccess)
    {
        ResultCode = ResultCodes.Ok;
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
        Errors = new List<Error> { error };
    }

    public Result(List<Error> errors)
    {
        IsSuccess = false;
        Errors = errors;
    }

    public Result(bool isSuccess, List<Error> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public Enum ResultCode { get; set; } = ResultCodes.Unknown;
    public bool IsFail => !IsSuccess;
    public Error? Error => Errors?.FirstOrDefault();
    public List<Error>? Errors { get; }
    
}