using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using ManagedCode.Communication.Constants;

namespace ManagedCode.Communication;

/// <summary>
///     JSON converter for <see cref="Problem" /> that writes RFC 7807 members first and everything else as extensions.
/// </summary>
public class ProblemJsonConverter : JsonConverter<Problem>
{
    private const string TypeMember = "type";
    private const string TitleMember = "title";
    private const string StatusMember = "status";
    private const string DetailMember = "detail";
    private const string InstanceMember = "instance";

    /// <summary>
    ///     Reads a problem, accepting both lowercase RFC 7807 member names and PascalCase.
    /// </summary>
    public override Problem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

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

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            var propertyName = reader.GetString();
            reader.Read();

            // RFC 7807 member names are lowercase, but producers that use PascalCase (the System.Text.Json
            // default, Newtonsoft, several non-.NET stacks) are common enough that a case-sensitive match would
            // silently dump every standard member into Extensions and leave the Problem blank.
            if (Matches(propertyName, TypeMember))
            {
                problem.Type = ReadString(ref reader) ?? ProblemConstants.Types.AboutBlank;
            }
            else if (Matches(propertyName, TitleMember))
            {
                problem.Title = ReadString(ref reader);
            }
            else if (Matches(propertyName, StatusMember))
            {
                problem.StatusCode = ReadStatusCode(ref reader);
            }
            else if (Matches(propertyName, DetailMember))
            {
                problem.Detail = ReadString(ref reader);
            }
            else if (Matches(propertyName, InstanceMember))
            {
                problem.Instance = ReadString(ref reader);
            }
            else
            {
                var value = JsonSerializer.Deserialize<object>(ref reader, options);
                if (propertyName is not null)
                {
                    problem.Extensions[propertyName] = value;
                }
            }
        }

        throw new JsonException("Unexpected end of JSON input");
    }

    /// <summary>
    ///     Writes the RFC 7807 members followed by any extensions, skipping extensions that would duplicate a standard member.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, Problem value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        writer.WriteStartObject();

        writer.WriteString(TypeMember, value.Type);

        if (value.Title is not null)
        {
            writer.WriteString(TitleMember, value.Title);
        }

        if (value.StatusCode != 0)
        {
            writer.WriteNumber(StatusMember, value.StatusCode);
        }

        if (value.Detail is not null)
        {
            writer.WriteString(DetailMember, value.Detail);
        }

        if (value.Instance is not null)
        {
            writer.WriteString(InstanceMember, value.Instance);
        }

        foreach (var kvp in value.Extensions)
        {
            // An extension named like a standard member would emit a duplicate JSON key; readers disagree on
            // which one wins, so the standard member written above is the one that stands.
            if (IsStandardMember(kvp.Key))
            {
                continue;
            }

            writer.WritePropertyName(kvp.Key);
            JsonSerializer.Serialize(writer, kvp.Value, options);
        }

        writer.WriteEndObject();
    }

    private static bool Matches(string? propertyName, string member)
    {
        return string.Equals(propertyName, member, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsStandardMember(string key)
    {
        return Matches(key, TypeMember)
               || Matches(key, TitleMember)
               || Matches(key, StatusMember)
               || Matches(key, DetailMember)
               || Matches(key, InstanceMember);
    }

    private static string? ReadString(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.String:
                return reader.GetString();
            // Tolerate a scalar where a string is expected rather than failing the whole payload.
            case JsonTokenType.True:
                return "true";
            case JsonTokenType.False:
                return "false";
            case JsonTokenType.Number:
                return reader.GetDouble().ToString(CultureInfo.InvariantCulture);
            default:
                throw new JsonException($"Unexpected token '{reader.TokenType}' for an RFC 7807 string member.");
        }
    }

    private static int ReadStatusCode(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                // "status": null occurs in the wild; treat it as "not supplied" rather than failing the payload.
                return 0;
            case JsonTokenType.Number:
                return reader.TryGetInt32(out var status) ? status : 0;
            case JsonTokenType.String:
                // Some gateways stringify the status code.
                return int.TryParse(reader.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                    ? parsed
                    : 0;
            default:
                throw new JsonException($"Unexpected token '{reader.TokenType}' for the RFC 7807 'status' member.");
        }
    }
}
