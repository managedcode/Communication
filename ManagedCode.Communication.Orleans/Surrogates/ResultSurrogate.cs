using Orleans;

namespace ManagedCode.Communication.Orleans.Surrogates;

[Immutable]
[GenerateSerializer]
public struct ResultSurrogate
{
    public ResultSurrogate(bool isSuccess, Problem? problem)
    {
        IsSuccess = isSuccess;
        Problem = problem;
    }

    [Id(0)] public bool IsSuccess;

    [Id(1)] public Problem? Problem;
}