using System;
using System.Collections.Generic;
using ManagedCode.Communication.Results;

namespace ManagedCode.Communication.CollectionResultT;

public partial struct CollectionResult<T>
{
    /// <summary>
    ///     Creates a validation failure with no field details.
    /// </summary>
    public static CollectionResult<T> Invalid() => ResultFactoryBridge<CollectionResult<T>>.Invalid();

    /// <summary>
    ///     Creates a validation failure identified by an enum error code.
    /// </summary>
    public static CollectionResult<T> Invalid<TEnum>(TEnum code) where TEnum : Enum
    {
        return ResultFactoryBridge<CollectionResult<T>>.Invalid(code);
    }

    /// <summary>
    ///     Creates a validation failure with a general message.
    /// </summary>
    public static CollectionResult<T> Invalid(string message)
    {
        return ResultFactoryBridge<CollectionResult<T>>.Invalid(message);
    }

    /// <summary>
    ///     Creates a validation failure with an enum error code and a general message.
    /// </summary>
    public static CollectionResult<T> Invalid<TEnum>(TEnum code, string message) where TEnum : Enum
    {
        return ResultFactoryBridge<CollectionResult<T>>.Invalid(code, message);
    }

    /// <summary>
    ///     Creates a validation failure for a single field.
    /// </summary>
    public static CollectionResult<T> Invalid(string key, string value)
    {
        return ResultFactoryBridge<CollectionResult<T>>.Invalid(key, value);
    }

    /// <summary>
    ///     Creates a validation failure for a single field, identified by an enum error code.
    /// </summary>
    public static CollectionResult<T> Invalid<TEnum>(TEnum code, string key, string value) where TEnum : Enum
    {
        return ResultFactoryBridge<CollectionResult<T>>.Invalid(code, key, value);
    }

    /// <summary>
    ///     Creates a validation failure from a field/message map.
    /// </summary>
    public static CollectionResult<T> Invalid(Dictionary<string, string> values)
    {
        return ResultFactoryBridge<CollectionResult<T>>.Invalid(values);
    }

    /// <summary>
    ///     Creates a validation failure from a field/message map, identified by an enum error code.
    /// </summary>
    public static CollectionResult<T> Invalid<TEnum>(TEnum code, Dictionary<string, string> values) where TEnum : Enum
    {
        return ResultFactoryBridge<CollectionResult<T>>.Invalid(code, values);
    }
}
