using System;
using System.Collections.Generic;
using ManagedCode.Communication.Results;

namespace ManagedCode.Communication.CollectionResultT;

public partial struct CollectionResult<T>
{
    public static CollectionResult<T> Invalid() => ResultFactoryBridge<CollectionResult<T>>.Invalid();

    public static CollectionResult<T> Invalid<TEnum>(TEnum code) where TEnum : Enum
    {
        return ResultFactoryBridge<CollectionResult<T>>.Invalid(code);
    }

    public static CollectionResult<T> Invalid(string message)
    {
        return ResultFactoryBridge<CollectionResult<T>>.Invalid(message);
    }

    public static CollectionResult<T> Invalid<TEnum>(TEnum code, string message) where TEnum : Enum
    {
        return ResultFactoryBridge<CollectionResult<T>>.Invalid(code, message);
    }

    public static CollectionResult<T> Invalid(string key, string value)
    {
        return ResultFactoryBridge<CollectionResult<T>>.Invalid(key, value);
    }

    public static CollectionResult<T> Invalid<TEnum>(TEnum code, string key, string value) where TEnum : Enum
    {
        return ResultFactoryBridge<CollectionResult<T>>.Invalid(code, key, value);
    }

    public static CollectionResult<T> Invalid(Dictionary<string, string> values)
    {
        return ResultFactoryBridge<CollectionResult<T>>.Invalid(values);
    }

    public static CollectionResult<T> Invalid<TEnum>(TEnum code, Dictionary<string, string> values) where TEnum : Enum
    {
        return ResultFactoryBridge<CollectionResult<T>>.Invalid(code, values);
    }
}
