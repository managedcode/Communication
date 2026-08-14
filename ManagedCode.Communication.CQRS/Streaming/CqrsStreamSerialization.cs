using System.Text.Json;

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

    private static JsonSerializerOptions CreateDefault()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Frozen so the shared instance cannot be reconfigured out from under either end of a stream.
        // populateMissingResolver: true installs the default reflection-based resolver.
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
