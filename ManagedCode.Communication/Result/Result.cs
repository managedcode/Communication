using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;
using ManagedCode.Communication.Extensions;

namespace ManagedCode.Communication;

[Serializable]
[DebuggerDisplay("IsSuccess: {IsSuccess}; {GetError().HasValue ? \" Error code: \" + GetError()!.Value.ErrorCode : string.Empty}")]
public partial struct Result : IResult
{
    internal Result(bool isSuccess, Error[]? errors, Dictionary<string, string>? invalidObject)
    {
        IsSuccess = isSuccess;
        Errors = errors;
        InvalidObject = invalidObject;
    }

    public bool IsSuccess { get; set; }

    [JsonIgnore]
    public bool IsFailed => !IsSuccess;

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

        var exceptions = Errors.Select(s => s.Exception() ?? new Exception(StringExtension.JoinFilter(';', s.ErrorCode, s.Message)));
        if (Errors.Length == 1)
            throw exceptions.First();

        throw new AggregateException(exceptions);
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

    public Error? GetError()
    {
        if (Errors == null || Errors.Length == 0)
            return null;

        return Errors[0];
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Error[]? Errors { get; set; }


    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<string, string>? InvalidObject { get; set; }

    [JsonIgnore]
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