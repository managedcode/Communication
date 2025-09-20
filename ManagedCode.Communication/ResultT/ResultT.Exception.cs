using System;
using System.Diagnostics.CodeAnalysis;
using ManagedCode.Communication.Results.Extensions;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    /// <summary>
    ///     Creates an exception from the result's problem.
    /// </summary>
    /// <returns>ProblemException if result has a problem, null otherwise.</returns>
    public Exception? ToException()
    {
        return ResultProblemExtensions.ToException(this);
    }

    /// <summary>
    ///     Throws a ProblemException if the result has a problem.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Value))]
    public void ThrowIfProblem()
    {
        ResultProblemExtensions.ThrowIfProblem(this);
    }
}
