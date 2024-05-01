namespace ManagedCode.Communication;


public interface IResult<out T> : IResult
{
    T? Value { get; }
    
    bool IsEmpty { get; }
}