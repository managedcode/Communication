using ManagedCode.Communication.Surrogates;
using Orleans;

namespace ManagedCode.Communication.Converters;

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