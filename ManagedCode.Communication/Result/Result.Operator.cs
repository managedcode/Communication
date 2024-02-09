using System;
using System.Linq;
using System.Text.Json;

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
        var errorsHashCode = Errors?.Aggregate(0, (current, error) => HashCode.Combine(current, error.GetHashCode())) ?? 0;
        return HashCode.Combine(IsSuccess, errorsHashCode);
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