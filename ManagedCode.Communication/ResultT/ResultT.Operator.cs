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
    ///     Widens a failed <see cref="Result" /> so it can be returned from a method that produces
    ///     <see cref="Result{T}" />.
    /// </summary>
    /// <remarks>
    ///     This is what lets a guard clause read <c>return Result.FailValidation(("cart", "is empty"));</c> inside
    ///     a <c>Result&lt;Order&gt;</c> method, and lets a failed <see cref="Result" /> be passed straight along.
    ///     <para>
    ///         <b>A success does not survive the conversion.</b> A <see cref="Result" /> carries no value, so
    ///         there is nothing to put in <see cref="Result{T}.Value" />; converting one yields a failure rather
    ///         than a success whose value is <c>null</c> in defiance of the nullable annotations. Convert only
    ///         failures — on the success path, build the <see cref="Result{T}" /> from its value.
    ///     </para>
    /// </remarks>
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