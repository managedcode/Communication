using System;

namespace ManagedCode.Communication;

public partial struct Result
{
    public static Result Succeed()
    {
        return new Result(true, null, default);
    }

    public static Result<T> Succeed<T>(T value)
    {
        return Result<T>.Succeed(value);
    }

    public static Result<T> Succeed<T>(Action<T> action) where T : new()
    {
        var result = new T();
        action?.Invoke(result);
        return Result<T>.Succeed(result);
    }
}