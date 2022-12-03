using Orleans;

namespace ManagedCode.Communication;

// This is the surrogate which will act as a stand-in for the foreign type.
// Surrogates should use plain fields instead of properties for better perfomance.
[GenerateSerializer]
public struct ResultSurrogate
{
    public ResultSurrogate(bool isSuccess, Error[]? errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    [Id(0)]
    public bool IsSuccess { get; set; }

    [Id(1)]
    public Error[]? Errors { get; set; }
}

// This is a converter which converts between the surrogate and the foreign type.
[RegisterConverter]
public sealed class ResultSurrogateConverter : IConverter<Result, ResultSurrogate>
{
    public Result ConvertFromSurrogate(in ResultSurrogate surrogate)
    {
        return new(surrogate.IsSuccess, surrogate.Errors);
    }

    public ResultSurrogate ConvertToSurrogate(in Result value)
    {
        return new(value.IsSuccess, value.Errors);
    }
}