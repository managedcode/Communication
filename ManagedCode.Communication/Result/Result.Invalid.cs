using System;
using System.Collections.Generic;

namespace ManagedCode.Communication;

public partial struct Result
{
    public static Result Invalid()
    {
        return new Result(false, default, new Dictionary<string, string> { { "message", nameof(Invalid) } });
    }
    
    public static Result Invalid(string message)
    {
        return new Result(false, default, new Dictionary<string, string> { { nameof(message), message } });
    }
    
    public static Result Invalid(string key, string value)
    {
        return new Result(false, default, new Dictionary<string, string> { { key, value } });
    }
    
    public static Result Invalid(Dictionary<string, string> values)
    {
        return new Result(false, default, values);
    }
    
    
    public static Result<T> Invalid<T>()
    {
        return new Result<T>(false, default, default, new Dictionary<string, string> { { "message", nameof(Invalid)  }});
    }
    
    public static Result<T> Invalid<T>(string message)
    {
        return new Result<T>(false, default, default, new Dictionary<string, string> { { nameof(message), message } });
    }
    
    public static Result<T> Invalid<T>(string key, string value)
    {
        return new Result<T>(false, default, default, new Dictionary<string, string> { { key, value } });
    }
    
    public static Result<T> Invalid<T>(Dictionary<string, string> values)
    {
        return new Result<T>(false, default, default, values);
    }
    
}