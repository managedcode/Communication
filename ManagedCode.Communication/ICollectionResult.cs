namespace ManagedCode.Communication;

public interface ICollectionResult<out T> : IResult
{
    T[]? Collection { get; }
}