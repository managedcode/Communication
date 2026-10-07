using System;
using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ManagedCode.Communication.Orleans.Converters;

/// <summary>Preserves typed JSON DOM values in native Newtonsoft Orleans storage.</summary>
public sealed class CommunicationJsonElementContractResolver : IContractResolver, IValueProvider
{
    private const string JsonPropertyName = "json";
    private const string InvalidJsonElementMessage = "A stored JsonElement requires a valid JSON payload.";
    private readonly IContractResolver _inner;
    private readonly JsonObjectContract _elementContract;

    /// <summary>Creates a resolver which preserves the configured contracts for all other types.</summary>
    public CommunicationJsonElementContractResolver(IContractResolver inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
        _elementContract = new JsonObjectContract(typeof(JsonElement))
        {
            OverrideCreator = CreateElement
        };
        _elementContract.Properties.Add(new Newtonsoft.Json.Serialization.JsonProperty
        {
            PropertyName = JsonPropertyName,
            PropertyType = typeof(string),
            Readable = true,
            Writable = false,
            ValueProvider = this,
            Required = Required.Always
        });
        _elementContract.CreatorParameters.Add(new Newtonsoft.Json.Serialization.JsonProperty
        {
            PropertyName = JsonPropertyName,
            PropertyType = typeof(string),
            Writable = true,
            Required = Required.Always
        });
    }

    /// <inheritdoc />
    public JsonContract ResolveContract(Type type) => type == typeof(JsonElement)
        ? _elementContract : _inner.ResolveContract(type);

    /// <inheritdoc />
    object? IValueProvider.GetValue(object target) => System.Text.Json.JsonSerializer.Serialize((JsonElement)target);

    /// <inheritdoc />
    void IValueProvider.SetValue(object target, object? value) => throw new NotSupportedException(InvalidJsonElementMessage);

    private static object CreateElement(object?[] arguments)
    {
        if (arguments is not [string json])
        {
            throw new JsonSerializationException(InvalidJsonElementMessage);
        }
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<JsonElement>(json);
        }
        catch (System.Text.Json.JsonException exception)
        {
            throw new JsonSerializationException(InvalidJsonElementMessage, exception);
        }
    }
}
