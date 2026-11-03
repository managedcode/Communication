using System;
using System.Text.Json;

namespace ManagedCode.Communication;

/// <summary>
///     The pre-encoded form of the member names the result converters read and write.
/// </summary>
/// <remarks>
///     Not a second list of names: each constant below is a <see cref="CommunicationJsonNames" /> value, so a
///     name has exactly one spelling in the codebase. This type exists because a hand-written converter writes
///     through <see cref="Utf8JsonWriter" />, which wants <see cref="JsonEncodedText" /> — encoded once into a
///     static field rather than on every write. Read-side matching lives here for the same reason: it compares
///     against those same encoded bytes.
///     <para>
///         The names are deliberately not run through
///         <see cref="JsonSerializerOptions.PropertyNamingPolicy" />. Every member of the result types carries an
///         explicit <c>[JsonPropertyName]</c>, which outranks a policy — so this matches what the reflection-based
///         path emitted before these converters existed, and what a differently configured service on the other
///         end of the wire still expects.
///     </para>
/// </remarks>
internal static class ResultMemberEncoding
{
    private const string InvalidSuccessFlag = "The result success flag must be a JSON boolean.";

    private const string IsSuccessName = CommunicationJsonNames.IsSuccess;
    private const string ValueName = CommunicationJsonNames.Value;
    private const string ProblemName = CommunicationJsonNames.Problem;

    public static readonly JsonEncodedText IsSuccess = JsonEncodedText.Encode(IsSuccessName);

    public static readonly JsonEncodedText Value = JsonEncodedText.Encode(ValueName);

    public static readonly JsonEncodedText Problem = JsonEncodedText.Encode(ProblemName);

    public static bool ReadIsSuccess(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            _ => throw new JsonException(InvalidSuccessFlag)
        };
    }

    public static ResultJsonMember MatchMember(ref Utf8JsonReader reader)
    {
        // Try all canonical names before decoding the property name for case-insensitive matching.
        if (reader.ValueTextEquals(IsSuccess.EncodedUtf8Bytes))
        {
            return ResultJsonMember.IsSuccess;
        }

        if (reader.ValueTextEquals(Value.EncodedUtf8Bytes))
        {
            return ResultJsonMember.Value;
        }

        if (reader.ValueTextEquals(Problem.EncodedUtf8Bytes))
        {
            return ResultJsonMember.Problem;
        }

        var name = reader.GetString();
        if (string.Equals(name, IsSuccessName, StringComparison.OrdinalIgnoreCase))
        {
            return ResultJsonMember.IsSuccess;
        }

        if (string.Equals(name, ValueName, StringComparison.OrdinalIgnoreCase))
        {
            return ResultJsonMember.Value;
        }

        return string.Equals(name, ProblemName, StringComparison.OrdinalIgnoreCase)
            ? ResultJsonMember.Problem
            : ResultJsonMember.Unknown;
    }
}

internal enum ResultJsonMember
{
    Unknown,
    IsSuccess,
    Value,
    Problem
}
