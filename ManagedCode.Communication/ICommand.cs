namespace ManagedCode.Communication;

/// <summary>
/// Defines a contract for a Command in the system.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Gets the unique identifier for the command.
    /// </summary>
    /// <value>The unique identifier for the command.</value>
    string Id { get; }

    /// <summary>
    /// Gets the type of the command.
    /// </summary>
    /// <value>The type of the command.</value>
    string CommandType { get; }
}