using Orleans;

namespace ManagedCode.Communication.Orleans.Surrogates;

/// <summary>
///     Orleans serialization surrogate for <c>ResultT</c>.
/// </summary>
[Immutable]
[GenerateSerializer]
public struct ResultTSurrogate<T>
{
    public ResultTSurrogate(bool isSuccess, T? value, Problem? problem)
    {
        IsSuccess = isSuccess;
        Value = value;
        Problem = problem;
    }

    [Id(0)] public bool IsSuccess;

    [Id(1)] public Problem? Problem;

    [Id(2)] public T? Value;
}