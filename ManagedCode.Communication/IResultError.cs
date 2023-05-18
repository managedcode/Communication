using System;

namespace ManagedCode.Communication;

public interface IResultError
{
    void AddError(Error error);
    Error? GetError();
    void ThrowIfFail();
    TEnum? ErrorCodeAs<TEnum>() where TEnum : Enum;
    bool IsErrorCode(Enum value);
    bool IsNotErrorCode(Enum value);
}