using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication;

/// <summary>
/// Custom JSON converter for Problem class to handle ErrorCode serialization properly
/// </summary>
public class ProblemJsonConverter : JsonConverter<Problem>
{
    public override Problem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token");
        }

        var problem = new Problem();
        
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return problem;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();

                switch (propertyName)
                {
                    case "type":
                        problem.Type = reader.GetString() ?? "about:blank";
                        break;
                    case "title":
                        problem.Title = reader.GetString();
                        break;
                    case "status":
                        problem.StatusCode = reader.GetInt32();
                        break;
                    case "detail":
                        problem.Detail = reader.GetString();
                        break;
                    case "instance":
                        problem.Instance = reader.GetString();
                        break;
                    default:
                        // Handle extension data
                        var value = JsonSerializer.Deserialize<object>(ref reader, options);
                        if (propertyName != null)
                        {
                            problem.Extensions[propertyName] = value;
                        }
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of JSON input");
    }

    public override void Write(Utf8JsonWriter writer, Problem value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("type", value.Type);
        
        if (value.Title != null)
        {
            writer.WriteString("title", value.Title);
        }

        if (value.StatusCode != 0)
        {
            writer.WriteNumber("status", value.StatusCode);
        }

        if (value.Detail != null)
        {
            writer.WriteString("detail", value.Detail);
        }

        if (value.Instance != null)
        {
            writer.WriteString("instance", value.Instance);
        }

        // Write extension data
        foreach (var kvp in value.Extensions)
        {
            writer.WritePropertyName(kvp.Key);
            JsonSerializer.Serialize(writer, kvp.Value, options);
        }

        writer.WriteEndObject();
    }
}