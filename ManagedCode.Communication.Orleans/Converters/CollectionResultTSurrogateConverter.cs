using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Surrogates;
using Orleans;

namespace ManagedCode.Communication.Converters;

[RegisterConverter]
public sealed class CollectionResultTSurrogateConverter<T> : IConverter<CollectionResult<T>, CollectionResultTSurrogate<T>>
{
    public CollectionResult<T> ConvertFromSurrogate(in CollectionResultTSurrogate<T> surrogate)
    {
        if (surrogate.IsSuccess)
            return CollectionResult<T>.CreateSuccess(surrogate.Collection, surrogate.PageNumber, surrogate.PageSize, surrogate.TotalItems);

        return CollectionResult<T>.CreateFailed(surrogate.Problem ?? Problem.GenericError(), surrogate.Collection);
    }

    public CollectionResultTSurrogate<T> ConvertToSurrogate(in CollectionResult<T> value)
    {
        return new CollectionResultTSurrogate<T>(value.IsSuccess, value.Collection, value.PageNumber, value.PageSize, value.TotalItems,
            value.Problem);
    }
}