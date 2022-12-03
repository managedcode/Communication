using Orleans;

namespace ManagedCode.Communication;

// This is the surrogate which will act as a stand-in for the foreign type.
// Surrogates should use plain fields instead of properties for better perfomance.
[GenerateSerializer]
public struct ResultTSurrogate<T>
{
    public ResultTSurrogate(bool isSuccess, T? value, Error[]? errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
    }

    [Id(0)]
    public bool IsSuccess { get; set; }

    [Id(1)]
    public Error[]? Errors { get; set; }

    [Id(2)]
    public T? Value { get; set; }
}

// This is a converter which converts between the surrogate and the foreign type.
[RegisterConverter]
public sealed class ResultTSurrogateConverter<T> : IConverter<Result<T>, ResultTSurrogate<T>>
{
    public Result<T> ConvertFromSurrogate(in ResultTSurrogate<T> surrogate)
    {
        return new(surrogate.IsSuccess, surrogate.Value, surrogate.Errors);
    }

    public ResultTSurrogate<T> ConvertToSurrogate(in Result<T> value)
    {
        return new(value.IsSuccess, value.Value, value.Errors);
    }
}