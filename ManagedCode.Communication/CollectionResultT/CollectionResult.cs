using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text.Json.Serialization;
using ManagedCode.Communication.Extensions;

namespace ManagedCode.Communication;

[Serializable]
[DebuggerDisplay("IsSuccess: {IsSuccess}; {GetError().HasValue ? \" Error code: \" + GetError()!.Value.ErrorCode : string.Empty}")]
public partial struct CollectionResult<T> : IResult, IResultError
{
    internal CollectionResult(bool isSuccess, IEnumerable<T>? collection, int pageNumber, int pageSize, int totalItems, Error[]? errors,
        Dictionary<string, string>? invalidObject) : this(isSuccess, collection?.ToArray(), pageNumber, pageSize, totalItems, errors, invalidObject)
    {
    }

    internal CollectionResult(bool isSuccess, T[]? collection, int pageNumber, int pageSize, int totalItems, Error[]? errors, Dictionary<string, string>? invalidObject)
    {
        IsSuccess = isSuccess;
        Collection = collection ?? [];
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalItems = totalItems;
        TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        Errors = errors;
        InvalidObject = invalidObject;
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
        {
            if(IsFailed)
                throw new Exception(nameof(IsFailed));
            
            return;
        }

        var exceptions = Errors.Select(s => s.Exception() ?? new Exception(StringExtension.JoinFilter(';', s.ErrorCode, s.Message)));

        if (Errors.Length == 1)
            throw exceptions.First();

        throw new AggregateException(exceptions);
    }

    public void ThrowIfFailWithStackPreserved()
    {
        if (Errors?.Any() is not true)
        {
            if (IsFailed)
                throw new Exception(nameof(IsFailed));

            return;
        }

        var exceptions = Errors.Select(s => s.ExceptionInfo() ?? ExceptionDispatchInfo.Capture(new Exception(StringExtension.JoinFilter(';', s.ErrorCode, s.Message))));

        if (Errors.Length == 1)
        {
            exceptions.First().Throw();
        }

        throw new AggregateException(exceptions.Select(e => e.SourceException));
    }

    [MemberNotNullWhen(true, nameof(Collection))]
    public bool IsSuccess { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public T[] Collection { get; set; } = [];

    public bool IsEmpty => Collection is null || Collection.Length != 0;
    
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Error[]? Errors { get; set; } = [];

    [JsonIgnore]
    [MemberNotNullWhen(false, nameof(Collection))]
    public bool IsFailed => !IsSuccess;

    public Error? GetError()
    {
        if (Errors == null || Errors.Length == 0)
            return null;

        return Errors[0];
    }
    
    public TEnum? ErrorCodeAs<TEnum>() where TEnum : Enum
    {
        return GetError().HasValue ? GetError()!.Value.ErrorCodeAs<TEnum>() : default;
    }

    public bool IsErrorCode(Enum value)
    {
        return GetError()?.IsErrorCode(value) ?? false;
    }

    public bool IsNotErrorCode(Enum value)
    {
        return GetError()?.IsNotErrorCode(value) ?? false;
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<string, string>? InvalidObject { get; set; }

    [JsonIgnore]
    [MemberNotNullWhen(false, nameof(Collection))]
    public bool IsInvalid => !IsSuccess || InvalidObject?.Any() is true;

    public void AddInvalidMessage(string message)
    {
        InvalidObject ??= new Dictionary<string, string>();
        InvalidObject[nameof(message)] = message;
    }

    public void AddInvalidMessage(string key, string value)
    {
        InvalidObject ??= new Dictionary<string, string>();
        InvalidObject[key] = value;
    }
}