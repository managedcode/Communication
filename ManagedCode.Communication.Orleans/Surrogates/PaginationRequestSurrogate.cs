using Orleans;

namespace ManagedCode.Communication.Orleans.Surrogates;

/// <summary>
///     Orleans serialization surrogate for <c>PaginationRequest</c>.
/// </summary>
[Immutable]
[GenerateSerializer]
public struct PaginationRequestSurrogate
{
    [Id(0)] public int Skip;
    [Id(1)] public int Take;

    public PaginationRequestSurrogate(int skip, int take)
    {
        Skip = skip;
        Take = take;
    }
}
