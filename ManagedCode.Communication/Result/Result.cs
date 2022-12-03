using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication;

[Serializable]
[DebuggerDisplay("IsSuccess: {IsSuccess}; {GetError().HasValue ? \" Error code: \" + GetError()!.Value.ErrorCode : string.Empty}")]
public partial struct Result : IResult
{
    internal Result(bool isSuccess, Error[]? errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; set; }

    [JsonIgnore]
    public bool IsFailed => !IsSuccess;

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