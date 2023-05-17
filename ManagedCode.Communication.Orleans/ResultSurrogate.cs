using System.Collections.Generic;
using Orleans;

namespace ManagedCode.Communication;

// This is the surrogate which will act as a stand-in for the foreign type.
// Surrogates should use plain fields instead of properties for better perfomance.
[GenerateSerializer]
public struct ResultSurrogate
{
    public ResultSurrogate(bool isSuccess, Error[]? errors, Dictionary<string, string>? invalidObject)
    {
        IsSuccess = isSuccess;
        Errors = errors;
        InvalidObject = invalidObject;
    }

    [Id(0)]
    public bool IsSuccess { get; set; }

    [Id(1)]
    public Error[]? Errors { get; set; }

    [Id(2)]
    public Dictionary<string, string>? InvalidObject { get; set; }
}

// This is a converter which converts between the surrogate and the foreign type.
[RegisterConverter]
public sealed class ResultSurrogateConverter : IConverter<Result, ResultSurrogate>
{
    public Result ConvertFromSurrogate(in ResultSurrogate surrogate)
    {
        return new Result(surrogate.IsSuccess, surrogate.Errors, surrogate.InvalidObject);
    }

    public ResultSurrogate ConvertToSurrogate(in Result value)
    {
        return new ResultSurrogate(value.IsSuccess, value.Errors, value.InvalidObject);
    }
}