using System;
using System.Collections.Generic;
using System.Linq;

namespace ManagedCode.Communication.CollectionResultT;

public partial struct CollectionResult<T>
{
    public static CollectionResult<T> Succeed()
    {
        return CreateSuccess(Array.Empty<T>(), 1, 0, 0);
    }

    public static CollectionResult<T> Succeed(T[] value, int pageNumber, int pageSize, int totalItems)
    {
        return CreateSuccess(value, pageNumber, pageSize, totalItems);
    }

    public static CollectionResult<T> Succeed(IEnumerable<T> value, int pageNumber, int pageSize, int totalItems)
    {
        var array = value as T[] ?? value.ToArray();
        return CreateSuccess(array, pageNumber, pageSize, totalItems);
    }

    public static CollectionResult<T> Succeed(T[] value)
    {
        var length = value.Length;
        return CreateSuccess(value, 1, length, length);
    }

    public static CollectionResult<T> Succeed(IEnumerable<T> value)
    {
        var array = value as T[] ?? value.ToArray();
        var length = array.Length;
        return CreateSuccess(array, 1, length, length);
    }

    public static CollectionResult<T> Succeed(T value)
    {
        return CreateSuccess(new[] { value }, 1, 1, 1);
    }
}
