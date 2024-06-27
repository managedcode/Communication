using System.Collections.Generic;
using System.Linq;

namespace ManagedCode.Communication;

public partial struct CollectionResult<T>
{
    public static CollectionResult<T> Succeed(T[] value, int pageNumber, int pageSize, int totalItems)
    {
        return new CollectionResult<T>(true, value, pageNumber, pageSize, totalItems, default, default);
    }

    public static CollectionResult<T> Succeed(IEnumerable<T> value, int pageNumber, int pageSize, int totalItems)
    {
        return new CollectionResult<T>(true, value, pageNumber, pageSize, totalItems, default, default);
    }

    public static CollectionResult<T> Succeed(T[] value)
    {
        return new CollectionResult<T>(true, value, 1, value.Length, value.Length, default, default);
    }

    public static CollectionResult<T> Succeed(IEnumerable<T> value)
    {
        var array = value.ToArray();
        return new CollectionResult<T>(true, array, 1, array.Length, array.Length, default, default);
    }
    
    public static CollectionResult<T> Empty()
    {
        return new CollectionResult<T>(true, [], 0, 0, 0, default, default);
    }
}