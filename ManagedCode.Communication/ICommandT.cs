namespace ManagedCode.Communication;

/// <summary>
///     Defines a contract for a command that contains a value of a specific type.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public interface ICommand<out T> : ICommand
{
    /// <summary>
    ///     Gets the value from the command.
    /// </summary>
    /// <value>The value, or null if the command does not contain a value.</value>
    T? Value { get; }

    /// <summary>
    ///     Gets a value indicating whether the command is empty.
    /// </summary>
    /// <value>true if the command is empty; otherwise, false.</value>
    bool IsEmpty { get; }
}