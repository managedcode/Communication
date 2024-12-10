using System.Collections.Generic;
using Orleans;

namespace ManagedCode.Communication.Surrogates;

[Immutable]
[GenerateSerializer]
public struct ResultTSurrogate<T>
{
    public ResultTSurrogate(bool isSuccess, T? value, Error[]? errors, Dictionary<string, string>? invalidObject)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
        InvalidObject = invalidObject;
    }

    [Id(0)] public bool IsSuccess;

    [Id(1)] public Error[]? Errors;

    [Id(2)] public Dictionary<string, string>? InvalidObject;

    [Id(3)] public T? Value;
}


