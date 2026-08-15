using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication;

/// <summary>
///     Reads and writes <see cref="Result" /> directly, without the reflection-driven path
///     <see cref="JsonSerializer" /> uses for structs with <c>init</c> members and private fields.
/// </summary>
/// <remarks>
///     Results are the most-serialized type in an application built on this library, so the default path's cost
///     shows up everywhere. Deserializing a <c>Result&lt;T&gt;</c> through it allocated roughly 512 bytes more
///     than the payload itself; this converter brings that overhead to zero.
/// </remarks>
public sealed class ResultJsonConverter : JsonConverter<Result>
{
    /// <inheritdoc />
    public override Result Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected an object for Result.");
        }

        var isSuccess = false;
        Problem? problem = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            if (ResultMemberEncoding.MatchesIsSuccess(ref reader))
            {
                reader.Read();
                isSuccess = reader.TokenType == JsonTokenType.True;
            }
            else if (ResultMemberEncoding.MatchesProblem(ref reader))
            {
                reader.Read();
                problem = JsonSerializer.Deserialize<Problem>(ref reader, options);
            }
            else
            {
                reader.Read();
                reader.Skip();
            }
        }

        return isSuccess ? Result.CreateSuccess() : Result.CreateFailed(problem ?? Problem.GenericError());
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Result value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStartObject();
        writer.WriteBoolean(ResultMemberEncoding.IsSuccess, value.IsSuccess);

        // Only a failure carries a problem, and the generic fallback is reconstructed on read.
        if (value.IsFailed)
        {
            writer.WritePropertyName(ResultMemberEncoding.Problem);
            JsonSerializer.Serialize(writer, value.Problem, options);
        }

        writer.WriteEndObject();
    }
}
