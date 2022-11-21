using System;

namespace ManagedCode.Communication;

public partial struct Result
{
    public bool Equals(Result other)
    {
        return ResultType != null && IsSuccess == other.IsSuccess && ResultType.Equals(other.ResultType, StringComparison.InvariantCultureIgnoreCase) && Equals(Errors, other.Errors);
    }

    public override bool Equals(object? obj)
    {
        return obj is Result other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IsSuccess, ResultType, Errors);
    }

    public static bool operator ==(Result obj1, bool obj2)
    {
        return obj1.IsSuccess == obj2;
    }

    public static bool operator !=(Result obj1, bool obj2)
    {
        return obj1.IsSuccess != obj2;
    }
    
    public static implicit operator bool(Result result)
    {
        return result.IsSuccess;
    }
    
    public static implicit operator Exception?(Result result)
    {
        return result.Error?.Exception;
    }
    
    public static implicit operator Result(Error error)
    {
        return new Result(error);
    }

    public static implicit operator Result(Error[] errors)
    {
        return new Result(errors);
    }

    public static implicit operator Result(Exception? exception)
    {
        return new Result(Error.FromException(exception));
    }
}