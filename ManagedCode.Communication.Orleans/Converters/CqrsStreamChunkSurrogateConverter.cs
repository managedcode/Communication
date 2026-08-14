using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.Orleans.Surrogates;
using Orleans;

namespace ManagedCode.Communication.Orleans.Converters;

[RegisterConverter]
public sealed class CqrsStreamChunkSurrogateConverter<TProgress, TResult>
    : IConverter<CqrsStreamChunk<TProgress, TResult>, CqrsStreamChunkSurrogate<TProgress, TResult>>
{
    public CqrsStreamChunk<TProgress, TResult> ConvertFromSurrogate(
        in CqrsStreamChunkSurrogate<TProgress, TResult> surrogate)
    {
        return new CqrsStreamChunk<TProgress, TResult>(
            surrogate.Kind,
            surrogate.ProgressResult,
            surrogate.Final,
            surrogate.Message,
            surrogate.EventType,
            surrogate.EventId,
            surrogate.Sequence,
            surrogate.TimestampUtc);
    }

    public CqrsStreamChunkSurrogate<TProgress, TResult> ConvertToSurrogate(
        in CqrsStreamChunk<TProgress, TResult> value)
    {
        return new CqrsStreamChunkSurrogate<TProgress, TResult>(
            value.Kind,
            value.ProgressResult,
            value.Final,
            value.Message,
            value.EventType,
            value.EventId,
            value.Sequence,
            value.TimestampUtc);
    }
}
