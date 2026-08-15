using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ManagedCode.Communication;

/// <summary>
///     Supplies <see cref="ResultTJsonConverter{T}" /> for any closed <see cref="Result{T}" />.
/// </summary>
public sealed class ResultTJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Result<>);
    }

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        var valueType = typeToConvert.GetGenericArguments()[0];

        return (JsonConverter)Activator.CreateInstance(
            typeof(ResultTJsonConverter<>).MakeGenericType(valueType))!;
    }
}

/// <summary>
///     Reads and writes <see cref="Result{T}" /> directly, without the reflection-driven path
///     <see cref="JsonSerializer" /> uses for structs with <c>init</c> members and private fields.
/// </summary>
/// <typeparam name="T">The payload type.</typeparam>
public sealed class ResultTJsonConverter<T> : JsonConverter<Result<T>>
{
    /// <inheritdoc />
    public override Result<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Expected an object for Result<{typeof(T).Name}>.");
        }

        var isSuccess = false;
        T? value = default;
        Problem? problem = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            if (ResultJsonMembers.IsSuccess(ref reader))
            {
                reader.Read();
                isSuccess = reader.TokenType == JsonTokenType.True;
            }
            else if (ResultJsonMembers.IsValue(ref reader))
            {
                reader.Read();
                value = JsonSerializer.Deserialize<T>(ref reader, options);
            }
            else if (ResultJsonMembers.IsProblem(ref reader))
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

        // A failure may legitimately carry a value; Result<T>.Fail(problem, value) produces exactly that.
        return isSuccess
            ? Result<T>.CreateSuccess(value!)
            : Result<T>.CreateFailed(problem ?? Problem.GenericError(), value);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Result<T> value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStartObject();
        writer.WriteBoolean(ResultJsonMembers.IsSuccessName, value.IsSuccess);

        // Always written on success, including when it equals default: omitting it would leave a non-.NET
        // client unable to tell Succeed(0) from a result carrying nothing.
        if (value.IsSuccess || value.Value is not null)
        {
            writer.WritePropertyName(ResultJsonMembers.ValueName);
            JsonSerializer.Serialize(writer, value.Value, options);
        }

        if (value.IsFailed)
        {
            writer.WritePropertyName(ResultJsonMembers.ProblemName);
            JsonSerializer.Serialize(writer, value.Problem, options);
        }

        writer.WriteEndObject();
    }
}
