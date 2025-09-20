using System;

namespace ManagedCode.Communication;

public partial struct Result
{
    public static Result Succeed() => CreateSuccess();

    public static Result<T> Succeed<T>(T value) => Result<T>.CreateSuccess(value);

    public static Result<T> Succeed<T>(Action<T> action) where T : new()
    {
        var instance = new T();
        action?.Invoke(instance);
        return Result<T>.CreateSuccess(instance);
    }
}
