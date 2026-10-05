using System;
using System.Collections.Concurrent;
using ManagedCode.Communication.Orleans.Surrogates;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ManagedCode.Communication.Orleans.Converters;

/// <summary>Preserves Communication results in the native Orleans JSON storage format.</summary>
public sealed class CommunicationResultJsonConverter : JsonConverter
{
    private const string InvalidSuccessFlag = "A stored Result requires an isSuccess boolean.";
    private static readonly ConcurrentDictionary<Type, IResultAdapter> Adapters = new();

    /// <inheritdoc />
    public override bool CanConvert(Type objectType) => objectType == typeof(Result) ||
        objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Result<>);

    /// <inheritdoc />
    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var document = JObject.Load(reader);
        return Adapter(objectType).Read(document, serializer);
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(value);
        Adapter(value.GetType()).Write(writer, value, serializer);
    }

    private static IResultAdapter Adapter(Type type) => Adapters.GetOrAdd(type, static type =>
        type == typeof(Result) ? new ResultAdapter() :
            (IResultAdapter)Activator.CreateInstance(typeof(ResultAdapter<>).MakeGenericType(type.GetGenericArguments()))!);

    private static bool Success(JObject document)
    {
        var token = document.GetValue(CommunicationJsonNames.IsSuccess, StringComparison.OrdinalIgnoreCase);
        var failed = document.GetValue(nameof(Result.IsFailed), StringComparison.OrdinalIgnoreCase);
        if (token is null && failed?.Type == JTokenType.Boolean && failed.Value<bool>())
        {
            return false;
        }
        if (token?.Type != JTokenType.Boolean || failed is not null &&
            (failed.Type != JTokenType.Boolean || failed.Value<bool>() == token.Value<bool>()))
        {
            throw new JsonSerializationException(InvalidSuccessFlag);
        }
        return token.Value<bool>();
    }

    private static Problem? ReadProblem(JObject document, JsonSerializer serializer)
    {
        var token = document.GetValue(CommunicationJsonNames.Problem, StringComparison.OrdinalIgnoreCase);
        if (token is null || token.Type == JTokenType.Null)
        {
            return null;
        }
        if (token is JObject legacy &&
            (legacy.GetValue(nameof(Problem.StatusCode), StringComparison.OrdinalIgnoreCase) is not null ||
             legacy.GetValue(nameof(Problem.Extensions), StringComparison.OrdinalIgnoreCase) is not null))
        {
            return token.ToObject<Problem>(serializer);
        }
        return System.Text.Json.JsonSerializer.Deserialize<Problem>(token.ToString(Formatting.None));
    }

    private static void WriteProblem(JsonWriter writer, Problem? problem)
    {
        if (problem is null)
        {
            return;
        }
        writer.WritePropertyName(CommunicationJsonNames.Problem);
        JToken.Parse(System.Text.Json.JsonSerializer.Serialize(problem)).WriteTo(writer);
    }

    private interface IResultAdapter
    {
        object Read(JObject document, JsonSerializer serializer);
        void Write(JsonWriter writer, object value, JsonSerializer serializer);
    }

    private sealed class ResultAdapter : IResultAdapter
    {
        public object Read(JObject document, JsonSerializer serializer)
        {
            var surrogate = new ResultSurrogate(Success(document), ReadProblem(document, serializer));
            return new ResultSurrogateConverter().ConvertFromSurrogate(surrogate);
        }

        public void Write(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var result = (Result)value;
            writer.WriteStartObject();
            writer.WritePropertyName(CommunicationJsonNames.IsSuccess);
            writer.WriteValue(result.IsSuccess);
            WriteProblem(writer, result.Problem);
            writer.WriteEndObject();
        }
    }

    private sealed class ResultAdapter<T> : IResultAdapter
    {
        public object Read(JObject document, JsonSerializer serializer)
        {
            var token = document.GetValue(CommunicationJsonNames.Value, StringComparison.OrdinalIgnoreCase);
            var value = token is null || token.Type == JTokenType.Null ? default : token.ToObject<T>(serializer);
            var surrogate = new ResultTSurrogate<T>(Success(document), value, ReadProblem(document, serializer));
            return new ResultTSurrogateConverter<T>().ConvertFromSurrogate(surrogate);
        }

        public void Write(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var result = (Result<T>)value;
            writer.WriteStartObject();
            writer.WritePropertyName(CommunicationJsonNames.IsSuccess);
            writer.WriteValue(result.IsSuccess);
            writer.WritePropertyName(CommunicationJsonNames.Value);
            serializer.Serialize(writer, result.Value, typeof(T));
            WriteProblem(writer, result.Problem);
            writer.WriteEndObject();
        }
    }
}
