using ManagedCode.Communication.Surrogates;
using Orleans;

namespace ManagedCode.Communication.Converters;

[RegisterConverter]
public sealed class ResultTSurrogateConverter<T> : IConverter<Result<T>, ResultTSurrogate<T>>
{
    public Result<T> ConvertFromSurrogate(in ResultTSurrogate<T> surrogate)
    {
        if (surrogate.IsSuccess)
            return Result<T>.Succeed(surrogate.Value!);

        return Result<T>.CreateFailed(surrogate.Problem ?? Problem.GenericError(), surrogate.Value);
    }

    public ResultTSurrogate<T> ConvertToSurrogate(in Result<T> value)
    {
        return new ResultTSurrogate<T>(value.IsSuccess, value.Value, value.Problem);
    }
}