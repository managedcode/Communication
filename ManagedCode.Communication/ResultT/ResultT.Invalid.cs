using System;
using System.Collections.Generic;
using ManagedCode.Communication.Results;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    public static Result<T> Invalid() => ResultFactoryBridge<Result<T>>.Invalid();

    public static Result<T> Invalid<TEnum>(TEnum code) where TEnum : Enum
    {
        return ResultFactoryBridge<Result<T>>.Invalid(code);
    }

    public static Result<T> Invalid(string message)
    {
        return ResultFactoryBridge<Result<T>>.Invalid(message);
    }

    public static Result<T> Invalid<TEnum>(TEnum code, string message) where TEnum : Enum
    {
        return ResultFactoryBridge<Result<T>>.Invalid(code, message);
    }

    public static Result<T> Invalid(string key, string value)
    {
        return ResultFactoryBridge<Result<T>>.Invalid(key, value);
    }

    public static Result<T> Invalid<TEnum>(TEnum code, string key, string value) where TEnum : Enum
    {
        return ResultFactoryBridge<Result<T>>.Invalid(code, key, value);
    }

    public static Result<T> Invalid(Dictionary<string, string> values)
    {
        return ResultFactoryBridge<Result<T>>.Invalid(values);
    }

    public static Result<T> Invalid<TEnum>(TEnum code, Dictionary<string, string> values) where TEnum : Enum
    {
        return ResultFactoryBridge<Result<T>>.Invalid(code, values);
    }
}
