using System;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Orleans.Surrogates;
using Orleans;

namespace ManagedCode.Communication.Orleans.Converters;

/// <summary>
///     Orleans converter between <c>PaginationRequest</c> and its serialization surrogate.
/// </summary>
[RegisterConverter]
public sealed class PaginationRequestSurrogateConverter : IConverter<PaginationRequest, PaginationRequestSurrogate>
{
    /// <summary>
    ///     Rebuilds the value from its surrogate.
    /// </summary>
    public PaginationRequest ConvertFromSurrogate(in PaginationRequestSurrogate surrogate)
    {
        return new PaginationRequest(
            Math.Max(0, surrogate.Skip),
            Math.Max(0, surrogate.Take));
    }

    public PaginationRequestSurrogate ConvertToSurrogate(in PaginationRequest value)
    {
        return new PaginationRequestSurrogate(value.Skip, value.Take);
    }
}
