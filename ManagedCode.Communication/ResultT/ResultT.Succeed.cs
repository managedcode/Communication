using System;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    /// <summary>
    ///     Creates a success carrying the default value when that value is non-null.
    /// </summary>
    /// <exception cref="ArgumentNullException">The default value of T is null. Use non-generic Result for a success without a payload.</exception>
    public static Result<T> Succeed() => CreateSuccess(default!);

    /// <summary>
    ///     Creates a success carrying the value.
    /// </summary>
    /// <exception cref="ArgumentNullException">The supplied value is null.</exception>
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
