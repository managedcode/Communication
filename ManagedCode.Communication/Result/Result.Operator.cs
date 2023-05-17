using System;

namespace ManagedCode.Communication;

public partial struct Result
{
    public bool Equals(Result other)
    {
        return IsSuccess == other.IsSuccess && GetError()?.Message == other.GetError()?.Message && GetError()?.ErrorCode == other.GetError()?.ErrorCode;
    }

    public override bool Equals(object? obj)
    {
        return obj is Result other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IsSuccess, Errors);
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
        return result.GetError()?.Exception();
    }

    public static implicit operator Result(Error error)
    {
        return Fail(error);
    }

    public static implicit operator Result(Error[]? errors)
    {
        return Fail(errors);
    }

    public static implicit operator Result(Exception? exception)
    {
        return Fail(Error.FromException(exception));
    }
}