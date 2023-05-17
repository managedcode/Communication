using System;
using System.Collections.Generic;

namespace ManagedCode.Communication;

public partial struct CollectionResult<T>
{
    public static CollectionResult<T> Invalid(string message)
    {
        return new CollectionResult<T>(false, default, 0, 0, 0, default, new Dictionary<string, string> { { nameof(message), message } });
    }
    
    public static CollectionResult<T> Invalid(string key, string value)
    {
        return new CollectionResult<T>(false, default, 0, 0, 0, default, new Dictionary<string, string> { { key, value } });
    }
    
    public static CollectionResult<T> Invalid(Dictionary<string, string> values)
    {
        return new CollectionResult<T>(false, default, 0, 0, 0, default, values);
    }
}