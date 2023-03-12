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

public interface IResultError
{
    void AddError(Error error);
    Error? GetError();
    void ThrowIfFail();
    
}
