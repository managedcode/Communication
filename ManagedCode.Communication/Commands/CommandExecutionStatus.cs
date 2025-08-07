namespace ManagedCode.Communication.Commands;

public enum CommandExecutionStatus
{
    NotFound,
    NotStarted,
    Processing,
    InProgress,
    Completed,
    Failed
}