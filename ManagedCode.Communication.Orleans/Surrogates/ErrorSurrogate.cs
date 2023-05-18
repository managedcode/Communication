using System;
using Orleans;

namespace ManagedCode.Communication.Surrogates;

// This is the surrogate which will act as a stand-in for the foreign type.
// Surrogates should use plain fields instead of properties for better perfomance.
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

// This is a converter which converts between the surrogate and the foreign type.