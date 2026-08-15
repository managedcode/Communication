using System;
using System.Collections.Generic;
using ManagedCode.Communication.Results;

namespace ManagedCode.Communication;

public partial struct Result
{
    /// <summary>
    ///     Creates a validation failure with no field details.
    /// </summary>
    public static Result Invalid() => ResultFactoryBridge<Result>.Invalid();

    /// <summary>
    ///     Creates a validation failure identified by an enum error code.
    /// </summary>
    public static Result Invalid<TEnum>(TEnum code) where TEnum : Enum => ResultFactoryBridge<Result>.Invalid(code);

    /// <summary>
    ///     Creates a validation failure with a general message.
    /// </summary>
    public static Result Invalid(string message) => ResultFactoryBridge<Result>.Invalid(message);

    /// <summary>
    ///     Creates a validation failure with an enum error code and a general message.
    /// </summary>
    public static Result Invalid<TEnum>(TEnum code, string message) where TEnum : Enum
    {
        return ResultFactoryBridge<Result>.Invalid(code, message);
    }

    /// <summary>
    ///     Creates a validation failure for a single field.
    /// </summary>
    public static Result Invalid(string key, string value)
    {
        return ResultFactoryBridge<Result>.Invalid(key, value);
    }

    /// <summary>
    ///     Creates a validation failure for a single field, identified by an enum error code.
    /// </summary>
    public static Result Invalid<TEnum>(TEnum code, string key, string value) where TEnum : Enum
    {
        return ResultFactoryBridge<Result>.Invalid(code, key, value);
    }

    /// <summary>
    ///     Creates a validation failure from a field/message map.
    /// </summary>
    public static Result Invalid(Dictionary<string, string> values)
    {
        return ResultFactoryBridge<Result>.Invalid(values);
    }

    /// <summary>
    ///     Creates a validation failure from a field/message map, identified by an enum error code.
    /// </summary>
    public static Result Invalid<TEnum>(TEnum code, Dictionary<string, string> values) where TEnum : Enum
    {
        return ResultFactoryBridge<Result>.Invalid(code, values);
    }


    /// <summary>
    ///     Creates a typed validation failure with no field details.
    /// </summary>
    public static Result<T> Invalid<T>() => ResultFactoryBridge<Result<T>>.Invalid();

    /// <summary>
    ///     Creates a typed validation failure identified by an enum error code.
    /// </summary>
    public static Result<T> Invalid<T, TEnum>(TEnum code) where TEnum : Enum
    {
        return ResultFactoryBridge<Result<T>>.Invalid(code);
    }

    /// <summary>
    ///     Creates a typed validation failure with a general message.
    /// </summary>
    public static Result<T> Invalid<T>(string message)
    {
        return ResultFactoryBridge<Result<T>>.Invalid(message);
    }

    /// <summary>
    ///     Creates a typed validation failure with an enum error code and a general message.
    /// </summary>
    public static Result<T> Invalid<T, TEnum>(TEnum code, string message) where TEnum : Enum
    {
        return ResultFactoryBridge<Result<T>>.Invalid(code, message);
    }

    /// <summary>
    ///     Creates a typed validation failure for a single field.
    /// </summary>
    public static Result<T> Invalid<T>(string key, string value)
    {
        return ResultFactoryBridge<Result<T>>.Invalid(key, value);
    }

    /// <summary>
    ///     Creates a typed validation failure for a single field, identified by an enum error code.
    /// </summary>
    public static Result<T> Invalid<T, TEnum>(TEnum code, string key, string value) where TEnum : Enum
    {
        return ResultFactoryBridge<Result<T>>.Invalid(code, key, value);
    }

    /// <summary>
    ///     Creates a typed validation failure from a field/message map.
    /// </summary>
    public static Result<T> Invalid<T>(Dictionary<string, string> values)
    {
        return ResultFactoryBridge<Result<T>>.Invalid(values);
    }

    /// <summary>
    ///     Creates a typed validation failure from a field/message map, identified by an enum error code.
    /// </summary>
    public static Result<T> Invalid<T, TEnum>(TEnum code, Dictionary<string, string> values) where TEnum : Enum
    {
        return ResultFactoryBridge<Result<T>>.Invalid(code, values);
    }
}
