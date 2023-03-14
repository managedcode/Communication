using System;
using System.Collections.Generic;
using System.Net;

namespace ManagedCode.Communication;

public partial struct CollectionResult<T>
{
    public static CollectionResult<T> Fail()
    {
        return new CollectionResult<T>(false, default, 0,0,0, default);
    }
    
    public static CollectionResult<T> Fail(IEnumerable<T>value)
    {
        return new CollectionResult<T>(false, value, 0,0,0,default);
    }
    
    public static CollectionResult<T> Fail(T[] value)
    {
        return new CollectionResult<T>(false, value, 0,0,0,default);
    }

    public static CollectionResult<T> Fail<TEnum>(TEnum code) where TEnum : Enum
    {
        return new CollectionResult<T>(false, default, 0,0,0,new[] { Error.Create(code) });
    }

    public static CollectionResult<T> Fail<TEnum>(TEnum code, IEnumerable<T> value) where TEnum : Enum
    {
        return new CollectionResult<T>(false, value, 0,0,0, new[] { Error.Create(code) });
    }
    
    public static CollectionResult<T> Fail<TEnum>(TEnum code, T[] value) where TEnum : Enum
    {
        return new CollectionResult<T>(false, value, 0,0,0, new[] { Error.Create(code) });
    }
    
    public static CollectionResult<T> Fail(string message)
    {
        return new CollectionResult<T>(false, default,0,0,0, new[] { Error.Create(message) });
    }
    
    public static CollectionResult<T> Fail(string message, IEnumerable<T> value)
    {
        return new CollectionResult<T>(false, value, 0,0,0,new[] { Error.Create(message) });
    }
    
    public static CollectionResult<T> Fail(string message, T[] value)
    {
        return new CollectionResult<T>(false, value, 0,0,0,new[] { Error.Create(message) });
    }

    public static CollectionResult<T> Fail<TEnum>(string message, TEnum code) where TEnum : Enum
    {
        return new CollectionResult<T>(false, default, 0,0,0,new[] { Error.Create(message, code) });
    }
    
    public static CollectionResult<T> Fail<TEnum>(string message, TEnum code, IEnumerable<T> value) where TEnum : Enum
    {
        return new CollectionResult<T>(false, value, 0,0,0, new[] { Error.Create(message, code) });
    }
    
    public static CollectionResult<T> Fail<TEnum>(string message, TEnum code, T[] value) where TEnum : Enum
    {
        return new CollectionResult<T>(false, value, 0,0,0, new[] { Error.Create(message, code) });
    }

    public static CollectionResult<T> Fail<TEnum>(TEnum code, string message) where TEnum : Enum
    {
        return new CollectionResult<T>(false, default,0,0,0, new[] { Error.Create(message, code) });
    }
    
    public static CollectionResult<T> Fail<TEnum>(TEnum code, string message, IEnumerable<T> value) where TEnum : Enum
    {
        return new CollectionResult<T>(false, value,0,0,0, new[] { Error.Create(message, code) });
    }
    
    public static CollectionResult<T> Fail<TEnum>(TEnum code, string message, T[] value) where TEnum : Enum
    {
        return new CollectionResult<T>(false, value,0,0,0, new[] { Error.Create(message, code) });
    }

    public static CollectionResult<T> Fail(Error error)
    {
        return new CollectionResult<T>(false, default, 0,0,0,new[] { error });
    }
    
    public static CollectionResult<T> Fail(Error? error)
    {
        if (error.HasValue)
        {
            return new CollectionResult<T>(false, default, 0,0,0,new[] { error.Value });
        }
        
        return new CollectionResult<T>(false, default,0,0,0, default);
    }

    public static CollectionResult<T> Fail(Error[]? errors)
    {
        return new CollectionResult<T>(false, default, 0,0,0, errors);
    }

    public static CollectionResult<T> Fail(Exception? exception)
    {
        return new CollectionResult<T>(false, default, 0,0,0, new[] { Error.FromException(exception) });
    }
    
    public static CollectionResult<T> Fail(Exception? exception, IEnumerable<T> value)
    {
        return new CollectionResult<T>(false, value, 0,0,0, new[] { Error.FromException(exception) });
    }
    
    public static CollectionResult<T> Fail(Exception? exception, T[] value)
    {
        return new CollectionResult<T>(false, value, 0,0,0, new[] { Error.FromException(exception) });
    }
}