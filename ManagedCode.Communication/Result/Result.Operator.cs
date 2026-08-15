using System;

namespace ManagedCode.Communication;

public partial struct Result
{
    /// <summary>
    ///     Compares two results by success and problem.
    /// </summary>
    public bool Equals(Result other)
    {
        return IsSuccess == other.IsSuccess && Problem?.Title == other.Problem?.Title && Problem?.Detail == other.Problem?.Detail;
    }

    /// <summary>
    ///     Compares this result with another object.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is Result other && Equals(other);
    }

    /// <summary>
    ///     Returns a hash code consistent with <c>Equals</c>.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(IsSuccess, Problem?.GetHashCode() ?? 0);
    }

    /// <summary>
    ///     Compares the result's success against a boolean.
    /// </summary>
    public static bool operator ==(Result obj1, bool obj2)
    {
        return obj1.IsSuccess == obj2;
    }

    /// <summary>
    ///     Compares the result's success against a boolean.
    /// </summary>
    public static bool operator !=(Result obj1, bool obj2)
    {
        return obj1.IsSuccess != obj2;
    }

    /// <summary>
    ///     Converts to <c>true</c> when the result succeeded.
    /// </summary>
    public static implicit operator bool(Result result)
    {
        return result.IsSuccess;
    }

    /// <summary>
    ///     Converts the carried problem into an exception, or <c>null</c> on success.
    /// </summary>
    public static implicit operator Exception?(Result result)
    {
        return result.Problem != null ? new ProblemException(result.Problem) : null;
    }

    /// <summary>
    ///     Creates a failure from a problem.
    /// </summary>
    public static implicit operator Result(Problem problem)
    {
        return Fail(problem);
    }

    /// <summary>
    ///     Creates a failure from an exception.
    /// </summary>
    public static implicit operator Result(Exception? exception)
    {
        return exception != null ? Fail(exception) : Succeed();
    }

    /// <summary>
    ///     Creates a success or a generic failure from a boolean.
    /// </summary>
    public static implicit operator Result(bool success)
    {
        return success ? Succeed() : Fail();
    }

    // public static implicit operator string(Result result)
    // {
    //     return JsonSerializer.Serialize(result);
    // }
    //
    // public static implicit operator Result(string result)
    // {
    //     return JsonSerializer.Deserialize<Result>(result);
    // }
}