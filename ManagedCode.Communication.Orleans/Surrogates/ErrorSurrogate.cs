using System;
using Orleans;

namespace ManagedCode.Communication.Surrogates;

[Immutable]
[GenerateSerializer]
public struct ErrorSurrogate
{
    public ErrorSurrogate(Exception? exception, string message, string? errorCode = default)
    {
        Exception = exception;
        ErrorCode = errorCode;
        Message = message;
    }

    [Id(0)] public string? ErrorCode;
    [Id(1)] public string Message;
    [Id(2)] public Exception? Exception;
}