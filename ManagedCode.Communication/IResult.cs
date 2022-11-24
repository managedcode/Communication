namespace ManagedCode.Communication;

public interface IResult
{
    bool IsSuccess { get; }
    bool IsFailed { get; }
}

public interface IResult<T> : IResult
{
    T? Value { get; }
}