using System;

namespace ManagedCode.Communication;

public partial struct Result
{
    public bool Equals(Result other)
    {
        return IsSuccess == other.IsSuccess && Problem?.Title == other.Problem?.Title && Problem?.Detail == other.Problem?.Detail;
    }

    public override bool Equals(object? obj)
    {
        return obj is Result other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IsSuccess, Problem?.GetHashCode() ?? 0);
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
        return result.Problem != null ? new ProblemException(result.Problem) : null;
    }

    public static implicit operator Result(Problem problem)
    {
        return Fail(problem);
    }

    public static implicit operator Result(Exception? exception)
    {
        return exception != null ? Fail(exception) : Succeed();
    }

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