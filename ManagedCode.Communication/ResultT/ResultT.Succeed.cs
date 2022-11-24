using System;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    public static Result<T> Succeed(T value)
    {
        return new Result<T>(true, value, null);
    }

    public static Result<T> Succeed(Action<T> action)
    {
        var result = Activator.CreateInstance<T>();
        action?.Invoke(result);
        return Succeed(result);
    }
}