using System;
using ManagedCode.Communication.Results.Factories;

namespace ManagedCode.Communication;

public partial struct Result
{
    public static Result Succeed()
    {
        return ResultFactory.Success();
    }

    public static Result<T> Succeed<T>(T value)
    {
        return ResultFactory.Success(value);
    }

    public static Result<T> Succeed<T>(Action<T> action) where T : new()
    {
        return ResultFactory.Success(() =>
        {
            var instance = new T();
            action?.Invoke(instance);
            return instance;
        });
    }
}
