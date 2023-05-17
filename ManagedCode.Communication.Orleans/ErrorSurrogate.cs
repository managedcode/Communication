using System;
using Orleans;

namespace ManagedCode.Communication;

// This is the surrogate which will act as a stand-in for the foreign type.
// Surrogates should use plain fields instead of properties for better perfomance.
[GenerateSerializer]
public struct ErrorSurrogate
{
    public ErrorSurrogate(Exception? exception, string message, string? errorCode = default)
    {
        Exception = exception;
        ErrorCode = errorCode;
        Message = message;
    }

    [Id(0)]
    public string? ErrorCode { get; set; }

    [Id(1)]
    public string Message { get; set; }

    [Id(2)]
    public Exception? Exception { get; set; }
}

// This is a converter which converts between the surrogate and the foreign type.
[RegisterConverter]
public sealed class ErrorSurrogateConverter : IConverter<Error, ErrorSurrogate>
{
    public Error ConvertFromSurrogate(in ErrorSurrogate surrogate)
    {
        return new Error(surrogate.Exception, surrogate.Message, surrogate.ErrorCode);
    }

    public ErrorSurrogate ConvertToSurrogate(in Error value)
    {
        return new ErrorSurrogate(value.Exception(), value.Message, value.ErrorCode);
    }
}