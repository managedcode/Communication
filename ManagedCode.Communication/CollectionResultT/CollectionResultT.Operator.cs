using System;
using System.Collections.Generic;

namespace ManagedCode.Communication;

public partial struct CollectionResult<T>
{
    public bool Equals(CollectionResult<T> other)
    {
        return IsSuccess == other.IsSuccess && EqualityComparer<T[]?>.Default.Equals(Collection, other.Collection) && GetError()?.Message == other.GetError()?.Message &&
               GetError()?.ErrorCode == other.GetError()?.ErrorCode;
    }

    public override bool Equals(object? obj)
    {
        return obj is CollectionResult<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IsSuccess, Collection, Errors);
    }

    public static bool operator ==(CollectionResult<T> obj1, bool obj2)
    {
        return obj1.IsSuccess == obj2;
    }

    public static bool operator !=(CollectionResult<T> obj1, bool obj2)
    {
        return obj1.IsSuccess != obj2;
    }

    public static implicit operator bool(CollectionResult<T> result)
    {
        return result.IsSuccess;
    }

    public static implicit operator Result(CollectionResult<T> result)
    {
        return result.IsSuccess ? Result.Succeed() : Result.Fail(result);
    }

    public static implicit operator Exception?(CollectionResult<T> result)
    {
        return result.GetError()?.Exception();
    }

    public static implicit operator CollectionResult<T>(Error error)
    {
        return Fail(error);
    }

    public static implicit operator CollectionResult<T>(Error[]? errors)
    {
        return Fail(errors);
    }

    public static implicit operator CollectionResult<T>(Exception? exception)
    {
        return Fail(Error.FromException(exception));
    }

    public static implicit operator CollectionResult<T>(Result result)
    {
        var error = result.GetError();
        return error is null ? Fail() : Fail(error);
    }
}