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
    
    public TEnum ErrorCodeAs<TEnum>() where TEnum : Enum
    {
        return (TEnum)Enum.Parse(typeof(TEnum), ErrorCode);
    }
    
    public bool IsErrorCode(Enum value)
    {
        return Enum.GetName(value.GetType(), value) == ErrorCode;
    }
    
    public bool IsNotErrorCode(Enum value)
    {
        return Enum.GetName(value.GetType(), value) != ErrorCode;
    }
    
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

    public override int GetHashCode()
    {
        return HashCode.Combine(Message, Exception, ErrorCode);
    }

    public static Error FromException(Exception? exception, Enum? errorCode = default)
    {
        return new Error(exception, errorCode);
    }

    public static bool operator ==(Error? error, Enum errorCode)
    {
        if (error.HasValue)
        {
            return error.Value.ErrorCode == Enum.GetName(errorCode.GetType(), errorCode);
        }

        return false;
    }

    public static bool operator !=(Error? error, Enum errorCode)
    {
        if (error.HasValue)
        {
            return error.Value.ErrorCode == Enum.GetName(errorCode.GetType(), errorCode);
        }

        return true;
    }

    public static bool operator ==(Enum errorCode, Error? error)
    {
        if (error.HasValue)
        {
            return error.Value.ErrorCode == Enum.GetName(errorCode.GetType(), errorCode);
        }

        return false;
    }

    public static bool operator !=(Enum errorCode, Error? error)
    {
        if (error.HasValue)
        {
            return error.Value.ErrorCode == Enum.GetName(errorCode.GetType(), errorCode);
        }

        return true;
    }
}