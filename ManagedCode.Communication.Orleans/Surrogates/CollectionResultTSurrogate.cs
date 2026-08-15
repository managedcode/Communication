using System;
using Orleans;

namespace ManagedCode.Communication.Orleans.Surrogates;

/// <summary>
///     Orleans serialization surrogate for <c>CollectionResultT</c>.
/// </summary>
[Immutable]
[GenerateSerializer]
public struct CollectionResultTSurrogate<T>
{
    /// <summary>
    ///     Creates the surrogate from its parts.
    /// </summary>
    public CollectionResultTSurrogate(bool isSuccess, T[]? collection, int pageNumber, int pageSize, int totalItems, Problem? problem)
    {
        IsSuccess = isSuccess;
        Collection = collection ?? Array.Empty<T>();
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalItems = totalItems;
        Problem = problem;
    }

    /// <summary>
    ///     Whether the original result succeeded.
    /// </summary>
    [Id(0)] public bool IsSuccess;
    /// <summary>
    ///     The items on the original page.
    /// </summary>
    [Id(1)] public T[]? Collection;
    /// <summary>
    ///     1-based page index.
    /// </summary>
    [Id(2)] public int PageNumber;
    /// <summary>
    ///     Maximum items per page.
    /// </summary>
    [Id(3)] public int PageSize;
    /// <summary>
    ///     Total items across all pages.
    /// </summary>
    [Id(4)] public int TotalItems;
    /// <summary>
    ///     The failure carried by the original result.
    /// </summary>
    [Id(5)] public Problem? Problem;
}