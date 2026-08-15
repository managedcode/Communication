using Orleans;

namespace ManagedCode.Communication.Orleans.Surrogates;

/// <summary>
///     Orleans serialization surrogate for <c>PaginationRequest</c>.
/// </summary>
[Immutable]
[GenerateSerializer]
public struct PaginationRequestSurrogate
{
    /// <summary>
    ///     Items to skip.
    /// </summary>
    [Id(0)] public int Skip;
    /// <summary>
    ///     Items to take.
    /// </summary>
    [Id(1)] public int Take;

    /// <summary>
    ///     Creates the surrogate from its parts.
    /// </summary>
    public PaginationRequestSurrogate(int skip, int take)
    {
        Skip = skip;
        Take = take;
    }
}
