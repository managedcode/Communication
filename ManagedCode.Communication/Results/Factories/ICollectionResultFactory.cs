using System;
using System.Linq;

namespace ManagedCode.Communication.Results;

/// <summary>
///     The factory surface for results that carry a page of items.
/// </summary>
public partial interface ICollectionResultFactory<TSelf, TValue> : IResultValueFactory<TSelf, TValue>, IResultFactory<TSelf>
    where TSelf : struct, ICollectionResultFactory<TSelf, TValue>
{
    /// <summary>
    ///     Creates a successful page with explicit paging metadata.
    /// </summary>
    static abstract TSelf Succeed(TValue[] items, int pageNumber, int pageSize, int totalItems);

    /// <summary>
    ///     Creates a successful page from a span, with explicit paging metadata.
    /// </summary>
    static virtual TSelf Succeed(ReadOnlySpan<TValue> items, int pageNumber, int pageSize, int totalItems)
    {
        return TSelf.Succeed(items.ToArray(), pageNumber, pageSize, totalItems);
    }
}
