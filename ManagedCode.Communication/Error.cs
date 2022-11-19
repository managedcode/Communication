using System;

namespace ManagedCode.Communication.ZALIPA;

public sealed class Error
{
    public Error()
    {
        Message = string.Empty;
    }

    public Error(string message, Enum? errorCode = default)
    {
        Message = message;
        ErrorCode = errorCode;
    }

    public Error(Exception? exception, Enum? errorCode = default)
    {
        Exception = exception;
        ErrorCode = errorCode;
        Message = exception?.Message ?? string.Empty;
    }

    public Error(Exception exception, string message, Enum? errorCode = default)
    {
        Exception = exception;
        ErrorCode = errorCode;
        Message = message;
    }

    public string Message { get; set; }
    public Exception? Exception { get; set; }
    public Enum? ErrorCode { get; set; }

    public static Error FromException(Exception? exception, Enum? errorCode = default)
    {
        return new Error(exception, errorCode);
    }
}