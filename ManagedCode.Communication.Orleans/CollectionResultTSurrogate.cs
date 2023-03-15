using System;
using Orleans;

namespace ManagedCode.Communication;

// This is the surrogate which will act as a stand-in for the foreign type.
// Surrogates should use plain fields instead of properties for better perfomance.
[GenerateSerializer]
public struct CollectionResultTSurrogate<T>
{
    public CollectionResultTSurrogate(bool isSuccess, T[]? collection, 
        int pageNumber, int pageSize, int totalItems,
        Error[]? errors)
    {
        IsSuccess = isSuccess;
        Collection = collection ?? Array.Empty<T>();
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalItems = totalItems;
        Errors = errors;
    }
    
    [Id(0)]
    public bool IsSuccess { get; set; }
    
    [Id(1)]
    public T[]? Collection { get; set; }
    
    [Id(2)]
    public int PageNumber { get; set;}
    
    [Id(3)]
    public int PageSize { get; set;}
    
    [Id(4)]
    public int TotalItems { get; set;}

    [Id(5)]
    public Error[]? Errors { get; set; }
}

// This is a converter which converts between the surrogate and the foreign type.
[RegisterConverter]
public sealed class CollectionResultTSurrogateConverter<T> : IConverter<CollectionResult<T>, CollectionResultTSurrogate<T>>
{
    public CollectionResult<T> ConvertFromSurrogate(in CollectionResultTSurrogate<T> surrogate)
    {
        return new(surrogate.IsSuccess, surrogate.Collection, surrogate.PageNumber, surrogate.PageSize, surrogate.TotalItems, surrogate.Errors);
    }

    public CollectionResultTSurrogate<T> ConvertToSurrogate(in CollectionResult<T> value)
    {
        return new(value.IsSuccess, value.Collection, value.PageNumber, value.PageSize, value.TotalItems, value.Errors);
    }
}