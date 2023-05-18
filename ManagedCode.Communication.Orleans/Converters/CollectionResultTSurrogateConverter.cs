using ManagedCode.Communication.Surrogates;
using Orleans;

namespace ManagedCode.Communication.Converters;

[RegisterConverter]
public sealed class CollectionResultTSurrogateConverter<T> : IConverter<CollectionResult<T>, CollectionResultTSurrogate<T>>
{
    public CollectionResult<T> ConvertFromSurrogate(in CollectionResultTSurrogate<T> surrogate)
    {
        return new CollectionResult<T>(surrogate.IsSuccess, surrogate.Collection, surrogate.PageNumber, surrogate.PageSize, surrogate.TotalItems, surrogate.Errors,
            surrogate.InvalidObject);
    }

    public CollectionResultTSurrogate<T> ConvertToSurrogate(in CollectionResult<T> value)
    {
        return new CollectionResultTSurrogate<T>(value.IsSuccess, value.Collection, value.PageNumber, value.PageSize, value.TotalItems, value.Errors,
            value.InvalidObject);
    }
}