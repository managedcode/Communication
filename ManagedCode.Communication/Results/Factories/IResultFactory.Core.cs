using ManagedCode.Communication.Results;

namespace ManagedCode.Communication.Results;

/// <summary>
///     The factory surface every result type implements, so generic code can build results without knowing the concrete type.
/// </summary>
public partial interface IResultFactory<TSelf>
    where TSelf : struct, IResultFactory<TSelf>
{
    /// <summary>
    ///     Creates a success.
    /// </summary>
    static abstract TSelf Succeed();
    /// <summary>
    ///     Creates a failure carrying the given problem.
    /// </summary>
    static abstract TSelf Fail(Problem problem);
}
