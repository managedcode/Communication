using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ManagedCode.Communication.CQRS;

/// <summary>
///     JSON settings used for CQRS chunk payloads on the wire.
/// </summary>
public static class CqrsStreamSerialization
{
    /// <summary>
    ///     Web defaults (camelCase, case-insensitive reads). Both ends of a CQRS stream must agree on these;
    ///     use this instance rather than constructing your own so client and server stay in sync.
    /// </summary>
    public static JsonSerializerOptions Default { get; } = CreateDefault();

    /// <summary>
    ///     <see cref="Default" />, plus source-generated contracts for your own payload types.
    /// </summary>
    /// <param name="payloadResolver">
    ///     Your <c>JsonSerializerContext</c> (or any resolver). Consulted first; anything it does not cover — the
    ///     transport's own chunk, result and problem types included — falls back to the reflection-based resolver.
    /// </param>
    /// <returns>Frozen options, safe to keep in a static field and share between streams.</returns>
    /// <remarks>
    ///     Worth doing when a payload type is a positional record such as <c>record Progress(string State)</c>.
    ///     <see cref="JsonSerializer" /> deserializes a type with a constructor parameter through a path that
    ///     allocates argument state; a source-generated contract skips it. Measured on such a payload: 176 bytes
    ///     down to 80 read on its own, and 48 bytes saved per chunk once it is carried inside one.
    ///     <para>
    ///         Combining is the point of this method: pointing <c>TypeInfoResolver</c> straight at your context
    ///         leaves <c>CqrsStreamChunk&lt;,&gt;</c> without a contract, which fails at runtime on the first chunk.
    ///     </para>
    ///     <para>
    ///         This is the client half. On the server, add the same context to
    ///         <c>ConfigureHttpJsonOptions</c> — the SSE response is written with ASP.NET Core's own JSON options.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     [JsonSerializable(typeof(Progress))]
    ///     internal partial class StreamPayloads : JsonSerializerContext;
    ///
    ///     private static readonly CqrsStreamClientOptions Options = new()
    ///     {
    ///         JsonSerializerOptions = CqrsStreamSerialization.WithPayloadContext(StreamPayloads.Default)
    ///     };
    ///     </code>
    /// </example>
    public static JsonSerializerOptions WithPayloadContext(IJsonTypeInfoResolver payloadResolver)
    {
        ArgumentNullException.ThrowIfNull(payloadResolver);

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = JsonTypeInfoResolver.Combine(payloadResolver, new DefaultJsonTypeInfoResolver())
        };

        options.MakeReadOnly();
        return options;
    }

    private static JsonSerializerOptions CreateDefault()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Frozen so the shared instance cannot be reconfigured out from under either end of a stream.
        // populateMissingResolver: true installs the default reflection-based resolver.
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
