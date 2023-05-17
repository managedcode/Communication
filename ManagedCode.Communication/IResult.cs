using System;

namespace ManagedCode.Communication;

public interface IResult : IResultError, IResultInvalid
{
    bool IsSuccess { get; }
    bool IsFailed { get; }
}

public interface IResult<out T> : IResult
{
    T? Value { get; }
}

public interface ICollectionResult<out T> : IResult
{
    T[]? Collection { get; }
}

public interface IResultError
{
    void AddError(Error error);
    Error? GetError();
    void ThrowIfFail();
    TEnum? ErrorCodeAs<TEnum>() where TEnum : Enum;
    bool IsErrorCode(Enum value);
    bool IsNotErrorCode(Enum value);
}

public interface IResultInvalid
{
    bool IsInvalid { get; }
    void AddInvalidMessage(string message);
    void AddInvalidMessage(string key, string value);
}