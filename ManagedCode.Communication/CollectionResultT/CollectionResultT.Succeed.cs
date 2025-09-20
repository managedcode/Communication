using System.Collections.Generic;
using ManagedCode.Communication.CollectionResults.Factories;

namespace ManagedCode.Communication.CollectionResultT;

public partial struct CollectionResult<T>
{
    public static CollectionResult<T> Succeed(T[] value, int pageNumber, int pageSize, int totalItems)
    {
        return CollectionResultFactory.Success(value, pageNumber, pageSize, totalItems);
    }

    public static CollectionResult<T> Succeed(IEnumerable<T> value, int pageNumber, int pageSize, int totalItems)
    {
        return CollectionResultFactory.Success(value, pageNumber, pageSize, totalItems);
    }

    public static CollectionResult<T> Succeed(T[] value)
    {
        return CollectionResultFactory.Success(value);
    }

    public static CollectionResult<T> Succeed(IEnumerable<T> value)
    {
        return CollectionResultFactory.Success(value);
    }
}
