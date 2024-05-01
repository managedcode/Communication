namespace ManagedCode.Communication;

public interface  ICommand<out T> : ICommand
{
    T? Value { get; }
    bool IsEmpty { get; }
}