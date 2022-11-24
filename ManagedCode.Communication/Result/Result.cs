using System.Diagnostics;

namespace ManagedCode.Communication;

[DebuggerDisplay("IsSuccess: {IsSuccess}; {GetError().HasValue ? \" Error code: \" + GetError()!.Value.ErrorCode : string.Empty}")]
public partial struct Result : IResult
{
    private Result(bool isSuccess, Error[]? errors)
    {
        IsSuccess = isSuccess;
        IsFailed = !isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; set; }
    public bool IsFailed { get; set; }

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