using System.Collections.Generic;
using System.Linq;

namespace ManagedCode.Communication.CollectionResultT;

public partial struct CollectionResult<T>
{
    public static CollectionResult<T> Succeed(T[] value, int pageNumber, int pageSize, int totalItems)
    {
        return CreateSuccess(value, pageNumber, pageSize, totalItems);
    }

    public static CollectionResult<T> Succeed(IEnumerable<T> value, int pageNumber, int pageSize, int totalItems)
    {
        return CreateSuccess(value.ToArray(), pageNumber, pageSize, totalItems);
    }

    public static CollectionResult<T> Succeed(T[] value)
    {
        return CreateSuccess(value, 1, value.Length, value.Length);
    }

    public static CollectionResult<T> Succeed(IEnumerable<T> value)
    {
        var array = value.ToArray();
        return CreateSuccess(array, 1, array.Length, array.Length);
    }
}
