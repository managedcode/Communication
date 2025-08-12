namespace ManagedCode.Communication.Commands;

/// <summary>
/// Represents the priority level of a command.
/// </summary>
public enum CommandPriority
{
    /// <summary>
    /// Low priority command.
    /// </summary>
    Low = 0,
    
    /// <summary>
    /// Normal priority command (default).
    /// </summary>
    Normal = 1,
    
    /// <summary>
    /// High priority command.
    /// </summary>
    High = 2,
    
    /// <summary>
    /// Critical priority command.
    /// </summary>
    Critical = 3
}