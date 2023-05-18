using System.Collections.Generic;
using Orleans;

namespace ManagedCode.Communication.Surrogates;

// This is the surrogate which will act as a stand-in for the foreign type.
// Surrogates should use plain fields instead of properties for better perfomance.
[Immutable]
[GenerateSerializer]
public struct ResultSurrogate
{
    public ResultSurrogate(bool isSuccess, Error[]? errors, Dictionary<string, string>? invalidObject)
    {
        IsSuccess = isSuccess;
        Errors = errors;
        InvalidObject = invalidObject;
    }

    [Id(0)] public bool IsSuccess;

    [Id(1)] public Error[]? Errors;

    [Id(2)] public Dictionary<string, string>? InvalidObject;
}

// This is a converter which converts between the surrogate and the foreign type.