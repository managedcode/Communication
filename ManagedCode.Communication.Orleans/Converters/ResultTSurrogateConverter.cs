using ManagedCode.Communication.Surrogates;
using Orleans;

namespace ManagedCode.Communication.Converters;

[RegisterConverter]
public sealed class ResultTSurrogateConverter<T> : IConverter<Result<T>, ResultTSurrogate<T>>
{
    public Result<T> ConvertFromSurrogate(in ResultTSurrogate<T> surrogate)
    {
        return Result<T>.Create(surrogate.IsSuccess, surrogate.Value, surrogate.Problem);
    }

    public ResultTSurrogate<T> ConvertToSurrogate(in Result<T> value)
    {
        return new ResultTSurrogate<T>(value.IsSuccess, value.Value, value.Problem);
    }
}