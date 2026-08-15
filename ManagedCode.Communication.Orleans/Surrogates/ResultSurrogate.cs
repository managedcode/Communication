using Orleans;

namespace ManagedCode.Communication.Orleans.Surrogates;

/// <summary>
///     Orleans serialization surrogate for <c>Result</c>.
/// </summary>
[Immutable]
[GenerateSerializer]
public struct ResultSurrogate
{
    /// <summary>
    ///     Creates the surrogate from its parts.
    /// </summary>
    public ResultSurrogate(bool isSuccess, Problem? problem)
    {
        IsSuccess = isSuccess;
        Problem = problem;
    }

    /// <summary>
    ///     Whether the original result succeeded.
    /// </summary>
    [Id(0)] public bool IsSuccess;

    /// <summary>
    ///     The failure carried by the original result.
    /// </summary>
    [Id(1)] public Problem? Problem;
}