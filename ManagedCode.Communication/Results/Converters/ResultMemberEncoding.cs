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
    private const string IsSuccessName = CommunicationJsonNames.IsSuccess;
    private const string ValueName = CommunicationJsonNames.Value;
    private const string ProblemName = CommunicationJsonNames.Problem;

    public static readonly JsonEncodedText IsSuccess = JsonEncodedText.Encode(IsSuccessName);

    public static readonly JsonEncodedText Value = JsonEncodedText.Encode(ValueName);

    public static readonly JsonEncodedText Problem = JsonEncodedText.Encode(ProblemName);

    public static bool MatchesIsSuccess(ref Utf8JsonReader reader)
    {
        return Matches(ref reader, IsSuccess, IsSuccessName);
    }

    public static bool MatchesValue(ref Utf8JsonReader reader)
    {
        return Matches(ref reader, Value, ValueName);
    }

    public static bool MatchesProblem(ref Utf8JsonReader reader)
    {
        return Matches(ref reader, Problem, ProblemName);
    }

    /// <summary>
    ///     Compares against the encoded UTF-8 name first, which allocates nothing and covers everything this
    ///     library writes, then falls back to a case-insensitive comparison so a producer using PascalCase still
    ///     reads.
    /// </summary>
    /// <remarks>
    ///     The bytes come from the same <see cref="JsonEncodedText" /> that is written, which itself comes from
    ///     <see cref="CommunicationJsonNames" /> — so there is no second spelling of a name anywhere that could
    ///     drift from the first.
    /// </remarks>
    private static bool Matches(ref Utf8JsonReader reader, JsonEncodedText encoded, string name)
    {
        return reader.ValueTextEquals(encoded.EncodedUtf8Bytes)
               || string.Equals(reader.GetString(), name, StringComparison.OrdinalIgnoreCase);
    }
}
