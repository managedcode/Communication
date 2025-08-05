namespace ManagedCode.Communication;

/// <summary>
///     Defines a contract for a result in the system.
/// </summary>
public interface IResult : IResultProblem, IResultInvalid
{
    /// <summary>
    ///     Gets a value indicating whether the operation was successful.
    /// </summary>
    /// <value>true if the operation was successful; otherwise, false.</value>
    bool IsSuccess { get; }

    /// <summary>
    ///     Gets a value indicating whether the operation failed.
    /// </summary>
    /// <value>true if the operation failed; otherwise, false.</value>
    bool IsFailed { get; }
}