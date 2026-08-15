using System;
using System.Text.Json;

namespace ManagedCode.Communication;

/// <summary>
///     Member-name matching shared by the result converters.
/// </summary>
/// <remarks>
///     Compares against UTF-8 literals first, which needs no string allocation and covers everything this
///     library writes. Only when that misses does it fall back to a case-insensitive comparison, so payloads
///     from producers that use PascalCase still round-trip.
/// </remarks>
internal static class ResultJsonMembers
{
    public const string IsSuccessName = "isSuccess";
    public const string ValueName = "value";
    public const string ProblemName = "problem";
    public const string CollectionName = "collection";
    public const string PageNumberName = "pageNumber";
    public const string PageSizeName = "pageSize";
    public const string TotalItemsName = "totalItems";
    public const string TotalPagesName = "totalPages";

    public static bool IsSuccess(ref Utf8JsonReader reader) => Matches(ref reader, "isSuccess"u8, IsSuccessName);

    public static bool IsValue(ref Utf8JsonReader reader) => Matches(ref reader, "value"u8, ValueName);

    public static bool IsProblem(ref Utf8JsonReader reader) => Matches(ref reader, "problem"u8, ProblemName);

    public static bool IsCollection(ref Utf8JsonReader reader) => Matches(ref reader, "collection"u8, CollectionName);

    public static bool IsPageNumber(ref Utf8JsonReader reader) => Matches(ref reader, "pageNumber"u8, PageNumberName);

    public static bool IsPageSize(ref Utf8JsonReader reader) => Matches(ref reader, "pageSize"u8, PageSizeName);

    public static bool IsTotalItems(ref Utf8JsonReader reader) => Matches(ref reader, "totalItems"u8, TotalItemsName);

    public static bool IsTotalPages(ref Utf8JsonReader reader) => Matches(ref reader, "totalPages"u8, TotalPagesName);

    private static bool Matches(ref Utf8JsonReader reader, ReadOnlySpan<byte> utf8Name, string name)
    {
        return reader.ValueTextEquals(utf8Name)
               || string.Equals(reader.GetString(), name, StringComparison.OrdinalIgnoreCase);
    }
}
