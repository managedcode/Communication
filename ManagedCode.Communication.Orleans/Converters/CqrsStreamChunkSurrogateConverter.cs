using ManagedCode.Communication.CQRS;
using ManagedCode.Communication.Orleans.Surrogates;
using Orleans;

namespace ManagedCode.Communication.Orleans.Converters;

/// <summary>
///     Orleans converter between <c>CqrsStreamChunk</c> and its serialization surrogate.
/// </summary>
[RegisterConverter]
public sealed class CqrsStreamChunkSurrogateConverter<TProgress, TResult>
    : IConverter<CqrsStreamChunk<TProgress, TResult>, CqrsStreamChunkSurrogate<TProgress, TResult>>
{
    /// <summary>
    ///     Rebuilds the value from its surrogate.
    /// </summary>
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

    /// <summary>
    ///     Converts the value into its surrogate for serialization.
    /// </summary>
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
