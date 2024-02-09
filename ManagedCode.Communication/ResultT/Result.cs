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
public partial struct Result<T> : IResult<T>
{
    internal Result(bool isSuccess, T value, Error[] errors, Dictionary<string, string>? invalidObject)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
        InvalidObject = invalidObject;
    }
    
    internal Result(Exception exception) : this(false, default, new[] { Error.FromException(exception) }, default)
    {
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
        if (Errors.Any() is not true)
            return;

        var exceptions = Errors.Select(s => s.Exception() ?? new Exception(StringExtension.JoinFilter(';', s.ErrorCode, s.Message)));

        if (Errors.Length == 1)
            throw exceptions.First();

        throw new AggregateException(exceptions);
    }

    public bool IsSuccess { get; set; }

    [JsonIgnore]
    public bool IsFailed => !IsSuccess;

    [MemberNotNullWhen(true, nameof(IsSuccess))]
    [MemberNotNullWhen(false, nameof(IsFailed))]
    [MemberNotNullWhen(false, nameof(IsInvalid))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public T Value { get; set; }

    public Error? GetError()
    {
        if (Errors == null || Errors.Length == 0)
            return null;

        return Errors[0];
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Error[]? Errors { get; set; } = Array.Empty<Error>();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<string, string>? InvalidObject { get; set; }

    [JsonIgnore]
    public bool IsInvalid => !IsSuccess || InvalidObject?.Any() is true;
    
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