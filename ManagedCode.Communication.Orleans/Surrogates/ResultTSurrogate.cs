using Orleans;

namespace ManagedCode.Communication.Orleans.Surrogates;

/// <summary>
///     Orleans serialization surrogate for <c>ResultT</c>.
/// </summary>
[Immutable]
[GenerateSerializer]
public struct ResultTSurrogate<T>
{
    /// <summary>
    ///     Creates the surrogate from its parts.
    /// </summary>
    public ResultTSurrogate(bool isSuccess, T? value, Problem? problem)
    {
        IsSuccess = isSuccess;
        Value = value;
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

    /// <summary>
    ///     The payload carried by the original value.
    /// </summary>
    [Id(2)] public T? Value;
}