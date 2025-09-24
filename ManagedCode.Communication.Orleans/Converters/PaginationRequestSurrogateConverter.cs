using System;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.Surrogates;
using Orleans;

namespace ManagedCode.Communication.Converters;

[RegisterConverter]
public sealed class PaginationRequestSurrogateConverter : IConverter<PaginationRequest, PaginationRequestSurrogate>
{
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
