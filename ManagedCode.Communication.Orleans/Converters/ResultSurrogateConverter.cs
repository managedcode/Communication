using ManagedCode.Communication.Surrogates;
using Orleans;

namespace ManagedCode.Communication.Converters;

[RegisterConverter]
public sealed class ResultSurrogateConverter : IConverter<Result, ResultSurrogate>
{
    public Result ConvertFromSurrogate(in ResultSurrogate surrogate)
    {
        if (surrogate.IsSuccess)
            return Result.Succeed();

        return Result.CreateFailed(surrogate.Problem ?? Problem.GenericError());
    }

    public ResultSurrogate ConvertToSurrogate(in Result value)
    {
        return new ResultSurrogate(value.IsSuccess, value.Problem);
    }
}