using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication;

[Serializable]
[DebuggerDisplay("IsSuccess: {IsSuccess}; {GetError().HasValue ? \" Error code: \" + GetError()!.Value.ErrorCode : string.Empty}")]
public partial struct Result<T> : IResult<T>
{
    internal Result(bool isSuccess, T? value, Error[]? errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
    }
    
    internal Result(Error error)
    {
        IsSuccess = false;
        Errors = new []{error};
    }

    public bool IsSuccess { get; set; }

    [JsonIgnore]
    public bool IsFailed => !IsSuccess;

    public T? Value { get; set; }

    public Error? GetError()
    {
        if (Errors == null || Errors.Length == 0)
        {
            return null;
        }

        return Errors[0];
    }

    public Error[]? Errors { get; set; }
}