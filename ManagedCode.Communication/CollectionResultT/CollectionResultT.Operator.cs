using System;
using System.Collections.Generic;

namespace ManagedCode.Communication.CollectionResultT;

public partial struct CollectionResult<T>
{
    public bool Equals(CollectionResult<T> other)
    {
        return IsSuccess == other.IsSuccess && EqualityComparer<T[]?>.Default.Equals(Collection, other.Collection) &&
               Problem?.Title == other.Problem?.Title && Problem?.Detail == other.Problem?.Detail;
    }

    public override bool Equals(object? obj)
    {
        return obj is CollectionResult<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IsSuccess, Collection?.GetHashCode() ?? 0, Problem?.GetHashCode() ?? 0);
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
        return result.IsSuccess ? Result.Succeed() : result.Problem != null ? Result.Fail(result.Problem) : Result.Fail();
    }

    public static implicit operator Exception?(CollectionResult<T> result)
    {
        return result.Problem != null ? new ProblemException(result.Problem) : null;
    }

    public static implicit operator CollectionResult<T>(Problem problem)
    {
        return Fail(problem);
    }

    public static implicit operator CollectionResult<T>(Exception? exception)
    {
        return exception != null ? Fail(exception) : Fail();
    }

    public static implicit operator CollectionResult<T>(Result result)
    {
        return result.Problem != null ? Fail(result.Problem) : Fail();
    }
}