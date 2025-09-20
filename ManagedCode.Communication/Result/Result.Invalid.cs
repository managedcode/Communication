using System;
using System.Collections.Generic;
using ManagedCode.Communication.Results;

namespace ManagedCode.Communication;

public partial struct Result
{
    public static Result Invalid() => ResultFactoryBridge<Result>.Invalid();

    public static Result Invalid<TEnum>(TEnum code) where TEnum : Enum => ResultFactoryBridge<Result>.Invalid(code);

    public static Result Invalid(string message) => ResultFactoryBridge<Result>.Invalid(message);

    public static Result Invalid<TEnum>(TEnum code, string message) where TEnum : Enum
    {
        return ResultFactoryBridge<Result>.Invalid(code, message);
    }

    public static Result Invalid(string key, string value)
    {
        return ResultFactoryBridge<Result>.Invalid(key, value);
    }

    public static Result Invalid<TEnum>(TEnum code, string key, string value) where TEnum : Enum
    {
        return ResultFactoryBridge<Result>.Invalid(code, key, value);
    }

    public static Result Invalid(Dictionary<string, string> values)
    {
        return ResultFactoryBridge<Result>.Invalid(values);
    }

    public static Result Invalid<TEnum>(TEnum code, Dictionary<string, string> values) where TEnum : Enum
    {
        return ResultFactoryBridge<Result>.Invalid(code, values);
    }


    public static Result<T> Invalid<T>() => ResultFactoryBridge<Result<T>>.Invalid();

    public static Result<T> Invalid<T, TEnum>(TEnum code) where TEnum : Enum
    {
        return ResultFactoryBridge<Result<T>>.Invalid(code);
    }

    public static Result<T> Invalid<T>(string message)
    {
        return ResultFactoryBridge<Result<T>>.Invalid(message);
    }

    public static Result<T> Invalid<T, TEnum>(TEnum code, string message) where TEnum : Enum
    {
        return ResultFactoryBridge<Result<T>>.Invalid(code, message);
    }

    public static Result<T> Invalid<T>(string key, string value)
    {
        return ResultFactoryBridge<Result<T>>.Invalid(key, value);
    }

    public static Result<T> Invalid<T, TEnum>(TEnum code, string key, string value) where TEnum : Enum
    {
        return ResultFactoryBridge<Result<T>>.Invalid(code, key, value);
    }

    public static Result<T> Invalid<T>(Dictionary<string, string> values)
    {
        return ResultFactoryBridge<Result<T>>.Invalid(values);
    }

    public static Result<T> Invalid<T, TEnum>(TEnum code, Dictionary<string, string> values) where TEnum : Enum
    {
        return ResultFactoryBridge<Result<T>>.Invalid(code, values);
    }
}
