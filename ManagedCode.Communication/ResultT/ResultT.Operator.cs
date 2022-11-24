using System;
using System.Collections.Generic;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    public bool Equals(Result<T> other)
    {
        return IsSuccess == other.IsSuccess 
               && EqualityComparer<T?>.Default.Equals(Value, other.Value) 
               && GetError()?.Message == other.GetError()?.Message 
               && GetError()?.ErrorCode == other.GetError()?.ErrorCode;
    }

    public override bool Equals(object? obj)
    {
        return obj is Result<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IsSuccess, Value, Errors);
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

    public static implicit operator Result(Result<T> result)
    {
        return result.IsSuccess ? Result.Succeed() : Result.Fail(result);
    }
    
    public static implicit operator Exception?(Result<T> result)
    {
        return result.GetError()?.Exception;
    }

    public static implicit operator Result<T>(Error error)
    {
        return Fail(error);
    }

    public static implicit operator Result<T>(Error[]? errors)
    {
        return Fail(errors);
    }

    public static implicit operator Result<T>(Exception? exception)
    {
        return Fail(Error.FromException(exception));
    }
}