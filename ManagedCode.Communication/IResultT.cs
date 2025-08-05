namespace ManagedCode.Communication;

/// <summary>
///     Defines a contract for a result that contains a value of a specific type.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public interface IResult<out T> : IResult
{
    /// <summary>
    ///     Gets the value from the result.
    /// </summary>
    /// <value>The value, or null if the result does not contain a value.</value>
    T? Value { get; }

    /// <summary>
    ///     Gets a value indicating whether the result is empty.
    /// </summary>
    /// <value>true if the result is empty; otherwise, false.</value>
    bool IsEmpty { get; }
}