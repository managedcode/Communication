using System;
using System.Collections.Generic;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    public bool Equals(Result<T> other)
    {
        return IsSuccess == other.IsSuccess && ResultType == other.ResultType
               && EqualityComparer<T?>.Default.Equals(Value, other.Value) 
               && Error?.Message == other.Error?.Message
               && Error?.ErrorCode == other.Error?.ErrorCode;
    }

    public override bool Equals(object? obj)
    {
        return obj is Result<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IsSuccess, ResultType, Value, Errors);
    }

    public static bool operator ==(Result<T> obj1, bool obj2)
    {
        return obj1.IsSuccess == obj2;
    }

    public static bool operator !=(Result<T> obj1, bool obj2)
    {
        return obj1.IsSuccess != obj2;
    }
    
    public static implicit operator bool(Result<T> result)
    {
        return result.IsSuccess;
    }
    
    public static implicit operator Exception?(Result<T> result)
    {
        return result.Error?.Exception;
    }
    
    public static implicit operator Result<T>(Error error)
    {
        return new Result<T>(error);
    }

    public static implicit operator Result<T>(Error[] errors)
    {
        return new Result<T>(errors);
    }

    public static implicit operator Result<T>(Exception? exception)
    {
        return new Result<T>(ManagedCode.Communication.Error.FromException(exception));
    }
}