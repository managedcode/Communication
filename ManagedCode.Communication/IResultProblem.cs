using System.Diagnostics.CodeAnalysis;

namespace ManagedCode.Communication;

/// <summary>
///     Defines a contract for a result that contains a problem.
/// </summary>
public interface IResultProblem
{
    /// <summary>
    ///     Gets the problem details.
    /// </summary>
    Problem? Problem { get; }

    /// <summary>
    ///     Determines whether the result has a problem.
    /// </summary>
    bool HasProblem { get; }

    /// <summary>
    ///     Throws an exception if the result indicates a failure.
    /// </summary>
    bool ThrowIfFail();

    /// <summary>
    ///     Tries to get the problem from the result.
    /// </summary>
    /// <param name="problem">When this method returns, contains the problem if the result has a problem; otherwise, null.</param>
    /// <returns>true if the result has a problem; otherwise, false.</returns>
    bool TryGetProblem([MaybeNullWhen(false)] out Problem problem);
}       