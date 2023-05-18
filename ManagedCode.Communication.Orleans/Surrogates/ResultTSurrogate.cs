using System.Collections.Generic;
using Orleans;

namespace ManagedCode.Communication.Surrogates;

// This is the surrogate which will act as a stand-in for the foreign type.
// Surrogates should use plain fields instead of properties for better perfomance.
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

// This is a converter which converts between the surrogate and the foreign type.