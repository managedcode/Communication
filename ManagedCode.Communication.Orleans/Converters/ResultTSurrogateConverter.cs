using ManagedCode.Communication.Orleans.Surrogates;
using Orleans;

namespace ManagedCode.Communication.Orleans.Converters;

/// <summary>
///     Orleans converter between <c>ResultT</c> and its serialization surrogate.
/// </summary>
[RegisterConverter]
public sealed class ResultTSurrogateConverter<T> : IConverter<Result<T>, ResultTSurrogate<T>>
{
    /// <summary>
    ///     Rebuilds the value from its surrogate.
    /// </summary>
    public Result<T> ConvertFromSurrogate(in ResultTSurrogate<T> surrogate)
    {
        if (surrogate.IsSuccess)
            return Result<T>.Succeed(surrogate.Value!);

        return Result<T>.CreateFailed(surrogate.Problem ?? Problem.GenericError(), surrogate.Value);
    }

    /// <summary>
    ///     Converts the value into its surrogate for serialization.
    /// </summary>
    public ResultTSurrogate<T> ConvertToSurrogate(in Result<T> value)
    {
        return new ResultTSurrogate<T>(value.IsSuccess, value.Value, value.Problem);
    }
}