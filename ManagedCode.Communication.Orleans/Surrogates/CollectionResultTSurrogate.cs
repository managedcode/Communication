using System;
using Orleans;

namespace ManagedCode.Communication.Surrogates;

[Immutable]
[GenerateSerializer]
public struct CollectionResultTSurrogate<T>
{
    public CollectionResultTSurrogate(bool isSuccess, T[]? collection, int pageNumber, int pageSize, int totalItems,
        Problem? problem)
    {
        IsSuccess = isSuccess;
        Collection = collection ?? Array.Empty<T>();
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalItems = totalItems;
        Problem = problem;
    }

    [Id(0)] public bool IsSuccess;
    [Id(1)] public T[]? Collection;
    [Id(2)] public int PageNumber;
    [Id(3)] public int PageSize;
    [Id(4)] public int TotalItems;
    [Id(5)] public Problem? Problem;
}