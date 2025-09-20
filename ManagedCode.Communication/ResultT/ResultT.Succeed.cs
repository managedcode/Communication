using System;
using ManagedCode.Communication.Results;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    public static Result<T> Succeed() => CreateSuccess(default!);

    public static Result<T> Succeed(T value) => CreateSuccess(value);
 
    public static Result<T> Succeed(Action<T> action)
    {
        if (action is null)
        {
            return CreateSuccess(default!);
        }

        var instance = Activator.CreateInstance<T>();
        action(instance);
        return CreateSuccess(instance!);
    }
}
