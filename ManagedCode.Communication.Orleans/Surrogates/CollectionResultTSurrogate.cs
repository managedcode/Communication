using System;
using System.Collections.Generic;
using Orleans;

namespace ManagedCode.Communication.Surrogates;

// This is the surrogate which will act as a stand-in for the foreign type.
// Surrogates should use plain fields instead of properties for better perfomance.
[Immutable]
[GenerateSerializer]
public struct CollectionResultTSurrogate<T>
{
    public CollectionResultTSurrogate(bool isSuccess, T[]? collection, int pageNumber, int pageSize, int totalItems, Error[]? errors,
        Dictionary<string, string>? invalidObject)
    {
        IsSuccess = isSuccess;
        Collection = collection ?? Array.Empty<T>();
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalItems = totalItems;
        Errors = errors;
        InvalidObject = invalidObject;
    }

    [Id(0)] public bool IsSuccess;
    [Id(1)] public T[]? Collection;
    [Id(2)] public int PageNumber;
    [Id(3)] public int PageSize;
    [Id(4)] public int TotalItems;
    [Id(5)] public Error[]? Errors;
    [Id(6)] public Dictionary<string, string>? InvalidObject;
}

// This is a converter which converts between the surrogate and the foreign type.