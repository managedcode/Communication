using ManagedCode.Communication.Orleans.Surrogates;
using Orleans;

namespace ManagedCode.Communication.Orleans.Converters;

/// <summary>
///     Orleans converter between <c>Result</c> and its serialization surrogate.
/// </summary>
[RegisterConverter]
public sealed class ResultSurrogateConverter : IConverter<Result, ResultSurrogate>
{
    /// <summary>
    ///     Rebuilds the value from its surrogate.
    /// </summary>
    public Result ConvertFromSurrogate(in ResultSurrogate surrogate)
    {
        if (surrogate.IsSuccess)
            return Result.Succeed();

        return Result.CreateFailed(surrogate.Problem ?? Problem.GenericError());
    }

    /// <summary>
    ///     Converts the value into its surrogate for serialization.
    /// </summary>
    public ResultSurrogate ConvertToSurrogate(in Result value)
    {
        return new ResultSurrogate(value.IsSuccess, value.Problem);
    }
}