namespace ManagedCode.Communication;

public interface IResult : IResultError, IResultInvalid
{
    bool IsSuccess { get; }
    bool IsFailed { get; }
}
