namespace ManagedCode.Communication.Results;

public partial interface IResultFactory<TSelf>
    where TSelf : struct, IResultFactory<TSelf>
{
    /// <summary>
    ///     Creates a 400 failure for a required value that was null.
    /// </summary>
    static virtual TSelf FailNull(string? detail = null)
    {
        return ResultFactoryBridge.FailNull<TSelf>(detail);
    }

    /// <summary>
    ///     Creates a 400 failure for an invalid argument.
    /// </summary>
    static virtual TSelf FailArgument(string? detail = null)
    {
        return ResultFactoryBridge.FailArgument<TSelf>(detail);
    }

    /// <summary>
    ///     Creates a 400 failure for an argument outside its allowed range.
    /// </summary>
    static virtual TSelf FailOutOfRange(string? detail = null)
    {
        return ResultFactoryBridge.FailOutOfRange<TSelf>(detail);
    }

    /// <summary>
    ///     Creates a 409 failure for an operation that conflicts with the current state.
    /// </summary>
    static virtual TSelf FailInvalidState(string? detail = null)
    {
        return ResultFactoryBridge.FailInvalidState<TSelf>(detail);
    }
}
