using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication;

[Serializable]
[DebuggerDisplay("IsSuccess: {IsSuccess}; {GetError().HasValue ? \" Error code: \" + GetError()!.Value.ErrorCode : string.Empty}")]
public partial struct Result<T> : IResult<T>, IErrorAdder
{
    internal Result(bool isSuccess, T? value, Error[]? errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
    }
    
    public void AddError(Error error)
    {
        if (Errors == null)
        {
            Errors = new[] { error };
        }
        else
        {
            var list = Errors.ToList();
            list.Add(error);
            Errors = list.ToArray();
        }
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