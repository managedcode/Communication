using System;
using System.Collections.Generic;

namespace ManagedCode.Communication.CollectionResultT;

public partial struct CollectionResult<T>
{
    /// <summary>
    ///     Compares two results by success, value and problem.
    /// </summary>
    public bool Equals(CollectionResult<T> other)
    {
        return IsSuccess == other.IsSuccess && EqualityComparer<T[]?>.Default.Equals(Collection, other.Collection) &&
               Problem?.Title == other.Problem?.Title && Problem?.Detail == other.Problem?.Detail;
    }

    /// <summary>
    ///     Compares this result with another object.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is CollectionResult<T> other && Equals(other);
    }

    /// <summary>
    ///     Returns a hash code consistent with <c>Equals</c>.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(IsSuccess, Collection?.GetHashCode() ?? 0, Problem?.GetHashCode() ?? 0);
    }

    /// <summary>
    ///     Compares the result's success against a boolean.
    /// </summary>
    public static bool operator ==(CollectionResult<T> obj1, bool obj2)
    {
        return obj1.IsSuccess == obj2;
    }

    /// <summary>
    ///     Compares the result's success against a boolean.
    /// </summary>
    public static bool operator !=(CollectionResult<T> obj1, bool obj2)
    {
        return obj1.IsSuccess != obj2;
    }

    /// <summary>
    ///     Converts to <c>true</c> when the result succeeded.
    /// </summary>
    public static implicit operator bool(CollectionResult<T> result)
    {
        return result.IsSuccess;
    }

    /// <summary>
    ///     Discards the value and keeps only success or failure.
    /// </summary>
    public static implicit operator Result(CollectionResult<T> result)
    {
        return result.IsSuccess ? Result.Succeed() : result.Problem != null ? Result.Fail(result.Problem) : Result.Fail();
    }

    /// <summary>
    ///     Converts the carried problem into an exception, or <c>null</c> on success.
    /// </summary>
    public static implicit operator Exception?(CollectionResult<T> result)
    {
        return result.Problem != null ? new ProblemException(result.Problem) : null;
    }

    /// <summary>
    ///     Creates a failure from a problem.
    /// </summary>
    public static implicit operator CollectionResult<T>(Problem problem)
    {
        return Fail(problem);
    }

    /// <summary>
    ///     Creates a failure from an exception.
    /// </summary>
    public static implicit operator CollectionResult<T>(Exception? exception)
    {
        return exception != null ? Fail(exception) : Fail();
    }

    /// <summary>
    ///     Adopts the success or failure of a non-generic result.
    /// </summary>
    public static implicit operator CollectionResult<T>(Result result)
    {
        return result.Problem != null ? Fail(result.Problem) : Fail();
    }
}