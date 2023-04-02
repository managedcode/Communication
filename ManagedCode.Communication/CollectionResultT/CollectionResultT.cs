using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using ManagedCode.Communication.Extensions;

namespace ManagedCode.Communication;

[Serializable]
[DebuggerDisplay("IsSuccess: {IsSuccess}; {GetError().HasValue ? \" Error code: \" + GetError()!.Value.ErrorCode : string.Empty}")]
public partial struct CollectionResult<T> : IResult, IResultError
{
    internal CollectionResult(bool isSuccess, IEnumerable<T>? collection, 
        int pageNumber, int pageSize, int totalItems,
        Error[]? errors) : this(isSuccess, collection?.ToArray(), pageNumber, pageSize, totalItems, errors)
    {
    }
    
    internal CollectionResult(bool isSuccess, T[]? collection, 
        int pageNumber, int pageSize, int totalItems,
        Error[]? errors)
    {
        IsSuccess = isSuccess;
        Collection = collection ?? Array.Empty<T>();
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalItems = totalItems;
        TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        Errors = errors;
    }
    
    public void AddError(Error error)
    {
        if (Errors == null)
        {
            Errors = new[] { error };
        }
        else
        {
            var list = Errors.ToList();
            list.Add(error);
            Errors = list.ToArray();
        }
    }

    public void ThrowIfFail()
    {
        if (Errors?.Any() is not true) 
            return;
        
        var exceptions = Errors.Select(s => s.Exception() ?? new Exception(StringExtension.JoinFilter(';', s.ErrorCode, s.Message) ));
        
        if (Errors.Length == 1)
            throw exceptions.First();

        throw new AggregateException(exceptions);
    }

    public bool IsSuccess { get; set; }
    
    [MemberNotNullWhen(true, nameof(IsSuccess))]
    public T[]? Collection { get; set; }
    
    public int PageNumber { get; set;}
    public int PageSize { get; set;}
    public int TotalItems { get; set;}
    public int TotalPages { get; set;}

    public Error[]? Errors { get; set; }
    
    [JsonIgnore]
    public bool IsFailed => !IsSuccess;
    

    public Error? GetError()
    {
        if (Errors == null || Errors.Length == 0)
        {
            return null;
        }

        return Errors[0];
    }
}