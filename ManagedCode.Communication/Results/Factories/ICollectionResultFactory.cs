using System;
using System.Linq;

namespace ManagedCode.Communication.Results;

public partial interface ICollectionResultFactory<TSelf, TValue> : IResultValueFactory<TSelf, TValue>, IResultFactory<TSelf>
    where TSelf : struct, ICollectionResultFactory<TSelf, TValue>
{
    static abstract TSelf Succeed(TValue[] items, int pageNumber, int pageSize, int totalItems);

    static virtual TSelf Succeed(ReadOnlySpan<TValue> items, int pageNumber, int pageSize, int totalItems)
    {
        return TSelf.Succeed(items.ToArray(), pageNumber, pageSize, totalItems);
    }
}
