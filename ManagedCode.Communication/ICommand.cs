namespace ManagedCode.Communication;

/// <summary>
///     Defines a contract for a Commands in the system.
/// </summary>
public interface ICommand
{
    /// <summary>
    ///     Gets the unique identifier for the command.
    /// </summary>
    /// <value>The unique identifier for the command.</value>
    string CommandId { get; }

    /// <summary>
    ///     Gets the type of the command.
    /// </summary>
    /// <value>The type of the command.</value>
    string CommandType { get; }
}