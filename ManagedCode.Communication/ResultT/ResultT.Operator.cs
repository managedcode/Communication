using System;
using System.Collections.Generic;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    /// <summary>
    ///     Compares two results by success, value and problem.
    /// </summary>
    public bool Equals(Result<T> other)
    {
        return IsSuccess == other.IsSuccess && EqualityComparer<T?>.Default.Equals(Value, other.Value) && Problem?.Title == other.Problem?.Title &&
               Problem?.Detail == other.Problem?.Detail;
    }

    /// <summary>
    ///     Compares this result with another object.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is Result<T> other && Equals(other);
    }

    /// <summary>
    ///     Returns a hash code consistent with <c>Equals</c>.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(IsSuccess, Value?.GetHashCode() ?? 0, Problem?.GetHashCode() ?? 0);
    }

    /// <summary>
    ///     Compares the result's success against a boolean.
    /// </summary>
    public static bool operator ==(Result<T> obj1, bool obj2)
    {
        return obj1.IsSuccess == obj2;
    }

    /// <summary>
    ///     Compares the result's success against a boolean.
    /// </summary>
    public static bool operator !=(Result<T> obj1, bool obj2)
    {
        return obj1.IsSuccess != obj2;
    }

    /// <summary>
    ///     Converts to <c>true</c> when the result succeeded.
    /// </summary>
    public static implicit operator bool(Result<T> result)
    {
        return result.IsSuccess;
    }

    /// <summary>
    ///     Discards the value and keeps only success or failure.
    /// </summary>
    public static implicit operator Result(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Result.Succeed();
        }

        if (result.Problem != null)
        {
            return Result.Fail(result.Problem);
        }

        return Result.Fail();
    }

    /// <summary>
    ///     Converts the carried problem into an exception, or <c>null</c> on success.
    /// </summary>
    public static implicit operator Exception?(Result<T> result)
    {
        return result.Problem != null ? new ProblemException(result.Problem) : null;
    }

    /// <summary>
    ///     Creates a failure from a problem.
    /// </summary>
    public static implicit operator Result<T>(Problem problem)
    {
        return Fail(problem);
    }

    /// <summary>
    ///     Creates a failure from an exception.
    /// </summary>
    public static implicit operator Result<T>(Exception? exception)
    {
        return exception != null ? Fail(exception) : Fail();
    }

    /// <summary>
    ///     Adopts the success or failure of a non-generic result.
    /// </summary>
    public static implicit operator Result<T>(Result result)
    {
        return result.Problem != null ? Fail(result.Problem) : Fail();
    }

    /// <summary>
    ///     Creates a success carrying the value.
    /// </summary>
    public static implicit operator Result<T>(T value)
    {
        return Succeed(value);
    }

    // public static implicit operator string(Result<T> result)
    // {
    //     return JsonSerializer.Serialize(result);
    // }
    //
    // public static implicit operator Result<T>(string result)
    // {
    //     return JsonSerializer.Deserialize<Result<T>>(result);
    // }
}