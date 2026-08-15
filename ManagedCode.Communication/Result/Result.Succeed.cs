using System;

namespace ManagedCode.Communication;

public partial struct Result
{
    /// <summary>
    ///     Creates a success.
    /// </summary>
    public static Result Succeed() => CreateSuccess();

    /// <summary>
    ///     Creates a success carrying the value.
    /// </summary>
    public static Result<T> Succeed<T>(T value) => Result<T>.CreateSuccess(value);

    /// <summary>
    ///     Creates a new <typeparamref name="T" />, lets the action configure it, and returns it as a success.
    /// </summary>
    public static Result<T> Succeed<T>(Action<T> action) where T : new()
    {
        var instance = new T();
        action?.Invoke(instance);
        return Result<T>.CreateSuccess(instance);
    }
}
