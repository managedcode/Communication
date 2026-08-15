using ManagedCode.Communication.CollectionResultT;
using ManagedCode.Communication.Orleans.Surrogates;
using Orleans;

namespace ManagedCode.Communication.Orleans.Converters;

/// <summary>
///     Orleans converter between <c>CollectionResultT</c> and its serialization surrogate.
/// </summary>
[RegisterConverter]
public sealed class CollectionResultTSurrogateConverter<T> : IConverter<CollectionResult<T>, CollectionResultTSurrogate<T>>
{
    /// <summary>
    ///     Rebuilds the value from its surrogate.
    /// </summary>
    public CollectionResult<T> ConvertFromSurrogate(in CollectionResultTSurrogate<T> surrogate)
    {
        if (surrogate.IsSuccess)
            return CollectionResult<T>.CreateSuccess(surrogate.Collection, surrogate.PageNumber, surrogate.PageSize, surrogate.TotalItems);

        return CollectionResult<T>.CreateFailed(surrogate.Problem ?? Problem.GenericError(), surrogate.Collection);
    }

    /// <summary>
    ///     Converts the value into its surrogate for serialization.
    /// </summary>
    public CollectionResultTSurrogate<T> ConvertToSurrogate(in CollectionResult<T> value)
    {
        return new CollectionResultTSurrogate<T>(value.IsSuccess, value.Collection, value.PageNumber, value.PageSize, value.TotalItems,
            value.Problem);
    }
}