using System;
using System.Collections.Generic;

namespace ManagedCode.Communication;

public partial struct CollectionResult<T>
{
    
    public static CollectionResult<T> Invalid()
    {
        return new CollectionResult<T>(false, default, 0, 0, 0, default, new Dictionary<string, string> { { "message", nameof(Invalid) } });
    }
    
    public static CollectionResult<T> Invalid<TEnum>(TEnum code) where TEnum : Enum
    {
        return new CollectionResult<T>(false, default, 0, 0, 0, new [] {Error.Create(code)}, new Dictionary<string, string> { { nameof(code), Enum.GetName(code.GetType(), code) ?? string.Empty }});
    }
    
    public static CollectionResult<T> Invalid(string message)
    {
        return new CollectionResult<T>(false, default, 0, 0, 0, default, new Dictionary<string, string> { { nameof(message), message } });
    }
    
    public static CollectionResult<T> Invalid<TEnum>(TEnum code,string message)where TEnum : Enum
    {
        return new CollectionResult<T>(false, default, 0, 0, 0, new [] {Error.Create(code)}, new Dictionary<string, string> { { nameof(message), message } });
    }

    public static CollectionResult<T> Invalid(string key, string value)
    {
        return new CollectionResult<T>(false, default, 0, 0, 0, default, new Dictionary<string, string> { { key, value } });
    }
    
    public static CollectionResult<T> Invalid<TEnum>(TEnum code,string key, string value)where TEnum : Enum
    {
        return new CollectionResult<T>(false, default, 0, 0, 0, new [] {Error.Create(code)}, new Dictionary<string, string> { { key, value } });
    }

    public static CollectionResult<T> Invalid(Dictionary<string, string> values)
    {
        return new CollectionResult<T>(false, default, 0, 0, 0, default, values);
    }
    
    public static CollectionResult<T> Invalid<TEnum>(TEnum code,Dictionary<string, string> values)where TEnum : Enum
    {
        return new CollectionResult<T>(false, default, 0, 0, 0, new [] {Error.Create(code)}, values);
    }
}




