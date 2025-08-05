using System;
using System.Collections.Generic;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    public bool Equals(Result<T> other)
    {
        return IsSuccess == other.IsSuccess && EqualityComparer<T?>.Default.Equals(Value, other.Value) && Problem?.Title == other.Problem?.Title &&
               Problem?.Detail == other.Problem?.Detail;
    }

    public override bool Equals(object? obj)
    {
        return obj is Result<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IsSuccess, Value?.GetHashCode() ?? 0, Problem?.GetHashCode() ?? 0);
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

    public static implicit operator Exception?(Result<T> result)
    {
        return result.Problem != null ? new ProblemException(result.Problem) : null;
    }

    public static implicit operator Result<T>(Problem problem)
    {
        return Fail(problem);
    }

    public static implicit operator Result<T>(Exception? exception)
    {
        return exception != null ? Fail(exception) : Fail();
    }

    public static implicit operator Result<T>(Result result)
    {
        return result.Problem != null ? Fail(result.Problem) : Fail();
    }

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