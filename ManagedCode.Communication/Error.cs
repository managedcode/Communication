using System;

namespace ManagedCode.Communication;

public struct Error
{
    public Error()
    {
        Message = string.Empty;
    }

    public Error(string message, string? errorCode = default)
    {
        Message = message;
        ErrorCode = errorCode;
    }

    public Error(Exception? exception, string? errorCode = default)
    {
        Exception = exception;
        ErrorCode = errorCode;

        Message = exception?.Message ?? string.Empty;
    }

    public Error(Exception exception, string message, string? errorCode = default)
    {
        Exception = exception;
        ErrorCode = errorCode;
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

    public static Error FromException(Exception? exception, string? errorCode = default)
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
    
    public static Error Create(string errorCode)
    {
        return new Error(string.Empty, errorCode);
    }
    
    public static Error Create<TEnum>(TEnum errorCode) where TEnum : Enum
    {
        return new Error(string.Empty, Enum.GetName(typeof(TEnum), errorCode));
    }
    
    public static Error Create<TEnum>(string message, TEnum errorCode) where TEnum : Enum
    {
        return new Error(message, Enum.GetName(typeof(TEnum), errorCode));
    }

    public static Error Create<TEnum>(Exception? exception) where TEnum : Enum
    {
        return new Error(exception, string.Empty);
    }
    
    public static Error Create<TEnum>(Exception? exception, TEnum errorCode) where TEnum : Enum
    {
        return new Error(exception, Enum.GetName(typeof(TEnum), errorCode));
    }
    
    
    public static Error Create<TEnum>(string message, Exception exception, TEnum errorCode) where TEnum : Enum
    {
        return new Error(exception, message, Enum.GetName(typeof(TEnum), errorCode));
    }
}