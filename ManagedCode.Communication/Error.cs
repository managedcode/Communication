using System;

namespace ManagedCode.Communication;

public struct Error
{
    public Error()
    {
        Message = string.Empty;
    }

    public Error(string message, Enum? errorCode = default)
    {
        Message = message;
        if (errorCode != null)
        {
            ErrorCode = Enum.GetName(errorCode.GetType(), errorCode);
        }
    }

    public Error(Exception? exception, Enum? errorCode = default)
    {
        Exception = exception;
        if (errorCode != null)
        {
            ErrorCode = Enum.GetName(errorCode.GetType(), errorCode);
        }
        Message = exception?.Message ?? string.Empty;
    }

    public Error(Exception exception, string message, Enum? errorCode = default)
    {
        Exception = exception;
        if (errorCode != null)
        {
            ErrorCode = Enum.GetName(errorCode.GetType(), errorCode);
        }
        Message = message;
    }

    public string Message { get; set; }
    public Exception? Exception { get; set; }
    public string? ErrorCode { get; set; }
    
    public bool Equals(Error other)
    {
        return Message == other.Message &&
               ErrorCode == other.ErrorCode &&
               Exception?.GetType() == other.Exception?.GetType() &&
               Exception?.Message == other.Exception?.Message;
    }

    public override bool Equals(object? obj)
    {
        return obj is Error other && Equals(other);
    }

    public static Error FromException(Exception? exception, Enum? errorCode = default)
    {
        return new Error(exception, errorCode);
    }
}