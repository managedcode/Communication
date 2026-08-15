using System;
using ManagedCode.Communication.Results;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    /// <summary>
    ///     Creates a success carrying the default value.
    /// </summary>
    public static Result<T> Succeed() => CreateSuccess(default!);

    /// <summary>
    ///     Creates a success carrying the value.
    /// </summary>
    public static Result<T> Succeed(T value) => CreateSuccess(value);

    /// <summary>
    ///     Creates a new <typeparamref name="T" />, lets the action configure it, and returns it as a success.
    /// </summary>
    public static Result<T> Succeed(Action<T> action)
    {
        T instance = Activator.CreateInstance<T>();
        action(instance);
        return CreateSuccess(instance!);
    }
}
