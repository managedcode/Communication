namespace ManagedCode.Communication;

/// <summary>
///     Defines a contract for a result that contains a problem.
/// </summary>
public interface IResultProblem
{
    /// <summary>
    ///     Gets or sets the problem details.
    /// </summary>
    Problem? Problem { get; set; }

    /// <summary>
    ///     Determines whether the result has a problem.
    /// </summary>
    bool HasProblem { get; }

    /// <summary>
    ///     Throws an exception if the result indicates a failure.
    /// </summary>
    bool ThrowIfFail();
}