namespace ManagedCode.Communication;

public interface IResult
{
    bool IsSuccess { get; }
    bool IsFailed { get; }
    void AddError(Error error);
}

public interface IResult<T> : IResult
{
    T? Value { get; }
}

public interface IErrorAdder
{
    void AddError(Error error);
}