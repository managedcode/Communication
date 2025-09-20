using System;
using ManagedCode.Communication;

namespace ManagedCode.Communication.Results.Extensions;

/// <summary>
///     Problem-related helpers for <see cref="IResultProblem"/> implementations.
/// </summary>
public static class ResultProblemExtensions
{
    public static Exception? ToException(this IResultProblem result)
    {
        return result.Problem is not null ? new ProblemException(result.Problem) : null;
    }

    public static void ThrowIfProblem(this IResultProblem result)
    {
        if (result.Problem is not null)
        {
            throw new ProblemException(result.Problem);
        }
    }
}
