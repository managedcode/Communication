using System;

namespace ManagedCode.Communication;

public interface IResult
{
    bool IsSuccess { get; }
    bool IsFail { get; }
}

public interface IResult<T> : IResult
{
    T Value { get; }
}