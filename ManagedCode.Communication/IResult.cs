using System;

namespace ManagedCode.Communication;

public interface IResult
{
    bool IsSuccess { get; }
    bool IsFail { get; }
    string ResultType { get; }

    TEnum ResultCode<TEnum>() where TEnum : Enum;
    bool IsResultCode(Enum value);

    bool IsNotResultCode(Enum value);
}

public interface IResult<T> : IResult
{
    T? Value { get; set; }
}