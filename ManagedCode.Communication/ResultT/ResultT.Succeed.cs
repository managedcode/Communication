using System;
using ManagedCode.Communication.Results;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    public static Result<T> Succeed() => CreateSuccess(default!);

    public static Result<T> Succeed(T value) => CreateSuccess(value);

    public static Result<T> Succeed(Action<T> action)
    {
        T instance = Activator.CreateInstance<T>();
        action(instance);
        return CreateSuccess(instance!);
    }
}
