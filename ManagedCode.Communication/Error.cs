using System;

namespace ManagedCode.Communication;

public sealed class Error<TErrorCode> where TErrorCode : Enum
{
    public Error(string message, TErrorCode? errorCode = default)
    {
        Message = message;
        ErrorCode = errorCode;
    }

    public Error(Exception exception, TErrorCode? errorCode = default)
    {
        Exception = exception;
        ErrorCode = errorCode;
        Message = exception.Message ?? string.Empty;
    }

    public Error(Exception exception, string message, TErrorCode? errorCode = default)
    {
        Exception = exception;
        ErrorCode = errorCode;
        Message = message;
    }

    public string Message { get; set; }
    public Exception? Exception { get; set; }
    public TErrorCode? ErrorCode { get; set; }

    public static Error<TErrorCode> FromException(Exception exception, TErrorCode? errorCode = default)
    {
        return new Error<TErrorCode>(exception, errorCode);
    }
}