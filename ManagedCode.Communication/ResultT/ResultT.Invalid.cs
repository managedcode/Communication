using System;
using System.Collections.Generic;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    public static Result<T> Invalid()
    {
        return new Result<T>(false, default, default, new Dictionary<string, string> { { "message", nameof(Invalid) } });
    }
    
    public static Result<T> Invalid<TEnum>(TEnum code) where TEnum : Enum
    {
        return new Result<T>(false, default, new [] {Error.Create(code)}, new Dictionary<string, string> { { nameof(code), Enum.GetName(code.GetType(), code) ?? string.Empty }});
    }

    public static Result<T> Invalid(string message)
    {
        return new Result<T>(false, default, default, new Dictionary<string, string> { { nameof(message), message } });
    }
    
    public static Result<T> Invalid<TEnum>(TEnum code,string message) where TEnum : Enum
    {
        return new Result<T>(false, default, new [] {Error.Create(code)}, new Dictionary<string, string> { { nameof(message), message } });
    }

    public static Result<T> Invalid(string key, string value)
    {
        return new Result<T>(false, default, default, new Dictionary<string, string> { { key, value } });
    }
    
    public static Result<T> Invalid<TEnum>(TEnum code,string key, string value) where TEnum : Enum
    {
        return new Result<T>(false, default, new [] {Error.Create(code)}, new Dictionary<string, string> { { key, value } });
    }

    public static Result<T> Invalid(Dictionary<string, string> values)
    {
        return new Result<T>(false, default, default, values);
    }
    
    public static Result<T> Invalid<TEnum>(TEnum code,Dictionary<string, string> values) where TEnum : Enum
    {
        return new Result<T>(false, default, new [] {Error.Create(code)}, values);
    }
}