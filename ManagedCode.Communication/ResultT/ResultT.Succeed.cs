using System;
using ManagedCode.Communication.Results.Factories;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    public static Result<T> Succeed(T value)
    {
        return ResultFactory.Success(value);
    }

    public static Result<T> Succeed(Action<T> action)
    {
        return ResultFactory.Success(() =>
        {
            var instance = Activator.CreateInstance<T>();
            action?.Invoke(instance);
            return instance;
        });
    }
}
